﻿using System;
using System.Buffers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A typed datom for writing new datoms to the database. This is implemented by attributes
///     to provide a typed, unboxed, way of passing datoms through various functions. This datom is for the write
///     portion of a transaction so no transaction id is included. In additon, the attribute is also not included
///     as it is implied by the type of the IWriteDatom
/// </summary>
public interface IWriteDatom
{
    /// <summary>
    ///     The entity id for this datom
    /// </summary>
    public EntityId E { get; }

    /// <summary>
    ///     Extracts the entity and attribute from the datom, and writes the value to the buffer.
    /// </summary>
    public void Write<TWriter>(RegistryId registryId, Func<EntityId, EntityId> remapFn, TxId txId, TWriter writer)
        where TWriter : IBufferWriter<byte>;
}
