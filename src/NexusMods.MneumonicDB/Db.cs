﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.Comparators;
using NexusMods.MneumonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly AttributeRegistry _registry;

    private readonly IndexSegmentCache<EntityId> _entityCache;
    private readonly IndexSegmentCache<(EntityId, Type)> _reverseCache;

    public ISnapshot Snapshot { get; }
    public IAttributeRegistry Registry => _registry;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _entityCache = new IndexSegmentCache<EntityId>(EntityIterator, registry);
        _reverseCache = new IndexSegmentCache<(EntityId, Type)>(ReverseIterator, registry);
        Snapshot = snapshot;
        BasisTxId = txId;
    }

    private static IEnumerable<Datom> EntityIterator(IDb db, EntityId id)
    {
        return db.Snapshot.Datoms(IndexType.EAVTCurrent, id, EntityId.From(id.Value + 1));
    }

    private static IEnumerable<Datom> ReverseIterator(IDb db, (EntityId, Type) key)
    {
        var (id, type) = key;
        var attrId = db.Registry.GetAttributeId(type);

        Span<byte> startKey = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
        Span<byte> endKey = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
        MemoryMarshal.Write(startKey,  new KeyPrefix().Set(EntityId.MaxValueNoPartition, attrId, TxId.MinValue, false));
        MemoryMarshal.Write(endKey,  new KeyPrefix().Set(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false));

        MemoryMarshal.Write(startKey.SliceFast(KeyPrefix.Size), id);
        MemoryMarshal.Write(endKey.SliceFast(KeyPrefix.Size), id.Value + 1);


        return db.Snapshot.Datoms(IndexType.VAETCurrent, startKey, endKey);
    }

    public TxId BasisTxId { get; }

    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids)
        where TModel : struct, IEntity
    {
        foreach (var id in ids)
        {
            yield return Get<TModel>(id);
        }
    }

    public TValue Get<TAttribute, TValue>(ref ModelHeader header, EntityId id)
        where TAttribute : IAttribute<TValue>
    {
        var attrId = _registry.GetAttributeId<TAttribute>();
        var value = _entityCache.Get(this, header.Id)
            .Where(d => d.A == attrId)
            .Select(d => d.Resolve<TValue>())
            .First();

        return value;
    }

    public IEnumerable<TValue> GetAll<TAttribute, TValue>(ref ModelHeader model, EntityId modelId)
        where TAttribute : IAttribute<TValue>
    {
        var attrId = _registry.GetAttributeId<TAttribute>();
        var results = _entityCache.Get(this, model.Id)
            .Where(d => d.A == attrId)
            .Select(d => d.Resolve<TValue>());

        return results;
    }

    public TModel Get<TModel>(EntityId id)
        where TModel : struct, IEntity
    {
        ModelHeader header = new()
        {
            Id = id,
            Db = this
        };

        return MemoryMarshal.CreateReadOnlySpan(ref header, 1)
            .CastFast<ModelHeader, TModel>()[0];
    }

    public TModel[] GetReverse<TAttribute, TModel>(EntityId id)
        where TAttribute : IAttribute<EntityId>
        where TModel : struct, IEntity
    {
        var attrId = _registry.GetAttributeId<TAttribute>();
        return _reverseCache.Get(this, (id, typeof(TAttribute)))
            .Where(d => d.A == attrId)
            .Select(d => d.E)
            .Select(Get<TModel>)
            .ToArray();
    }

    public IEnumerable<IReadDatom> Datoms(EntityId entityId)
    {
        return _entityCache.Get(this, entityId)
            .Select(d => d.Resolved);
    }

    public IEnumerable<IReadDatom> Datoms(TxId txId)
    {
        return Snapshot.Datoms(IndexType.TxLog, txId, TxId.From(txId.Value + 1))
            .Select(d => d.Resolved);
    }

    public void Dispose() { }
}
