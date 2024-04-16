﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly AttributeRegistry _registry;

    private readonly IndexSegmentCache<EntityId> _entityCache;
    private readonly IndexSegmentCache<(EntityId, AttributeId)> _reverseCache;

    public ISnapshot Snapshot { get; }
    public IAttributeRegistry Registry => _registry;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _entityCache = new IndexSegmentCache<EntityId>(EntityDatoms, registry);
        _reverseCache = new IndexSegmentCache<(EntityId, AttributeId)>(ReverseDatoms, registry);
        Snapshot = snapshot;
        BasisTxId = txId;
    }

    private static IEnumerable<Datom> EntityDatoms(IDb db, EntityId id)
    {
        return db.Snapshot.Datoms(IndexType.EAVTCurrent, id, EntityId.From(id.Value + 1));
    }

    private static IEnumerable<Datom> ReverseDatoms(IDb db, (EntityId, AttributeId) key)
    {
        var (id, attrId) = key;

        Span<byte> startKey = stackalloc byte[KeyPrefix.Size + sizeof(ulong) + 1];
        Span<byte> endKey = stackalloc byte[KeyPrefix.Size + sizeof(ulong) + 1];
        MemoryMarshal.Write(startKey,  new KeyPrefix().Set(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false));
        MemoryMarshal.Write(endKey,  new KeyPrefix().Set(EntityId.MaxValueNoPartition, attrId, TxId.MaxValue, false));

        startKey[KeyPrefix.Size] = (byte)ValueTags.Reference;
        endKey[KeyPrefix.Size] = (byte)ValueTags.Reference;

        MemoryMarshal.Write(startKey.SliceFast(KeyPrefix.Size + 1), id);
        MemoryMarshal.Write(endKey.SliceFast(KeyPrefix.Size + 1), id.Value);


        return db.Snapshot.Datoms(IndexType.VAETCurrent, startKey, endKey);
    }

    public TxId BasisTxId { get; }

    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids)
        where TModel : IEntity
    {
        foreach (var id in ids)
        {
            yield return Get<TModel>(id);
        }
    }

    public TValue Get<TValue, TLowLevel>(EntityId id, Attribute<TValue, TLowLevel> attribute)
    {
        var attrId = attribute.GetDbId(_registry.Id);
        var entry = _entityCache.Get(this, id);
        for (var i = 0; i < entry.Count; i++)
        {
            var datom = entry[i];
            if (datom.A == attrId)
            {
                return datom.Resolve(attribute);
            }
        }

        throw new KeyNotFoundException();
    }

    public IEnumerable<EntityId> Find(IAttribute attribute)
    {
        var attrId = attribute.GetDbId(_registry.Id);
        var a = new KeyPrefix().Set(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false);
        var b = new KeyPrefix().Set(EntityId.MaxValueNoPartition, attrId, TxId.MaxValue, false);
        return Snapshot
            .Datoms(IndexType.AEVTCurrent, a, b)
            .Select(d => d.E);
    }

    public IndexSegment GetSegment(EntityId id)
    {
        return _entityCache.Get(this, id);
    }

    public IEnumerable<TValue> GetAll<TValue, TLowLevel>(EntityId id, Attribute<TValue, TLowLevel> attribute)
    {
        var attrId = attribute.GetDbId(_registry.Id);
        var results = _entityCache.Get(this, id)
            .Where(d => d.A == attrId)
            .Select(d => d.Resolve(attribute));

        return results;
    }

    public IEnumerable<EntityId> FindIndexed<TValue, TLowLevel>(TValue value, Attribute<TValue, TLowLevel> attribute)
    {
        var attrId = attribute.GetDbId(_registry.Id);
        if (!attribute.IsIndexed)
            throw new InvalidOperationException($"Attribute {attribute.Id} is not indexed");

        using var start = new PooledMemoryBufferWriter(64);
        attribute.Write(EntityId.MinValueNoPartition, _registry.Id, value, TxId.MinValue, false, start);

        using var end = new PooledMemoryBufferWriter(64);
        attribute.Write(EntityId.MaxValueNoPartition, _registry.Id, value, TxId.MinValue, false, end);

        var results = Snapshot
            .Datoms(IndexType.AVETCurrent, start.GetWrittenSpan(), end.GetWrittenSpan())
            .Select(d => d.E);

        return results;
    }

    public TModel Get<TModel>(EntityId id)
        where TModel : IEntity
    {
        return EntityConstructors<TModel>.Constructor(id, this);
    }

    public Entities<EntityIds, TModel> GetReverse<TModel>(EntityId id, Attribute<EntityId, ulong> attribute)
        where TModel : IEntity
    {
        var segment = _reverseCache.Get(this, (id, attribute.GetDbId(_registry.Id)));
        var ids = new EntityIds(segment, 0, segment.Count);
        return new Entities<EntityIds, TModel>(ids, this);
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
