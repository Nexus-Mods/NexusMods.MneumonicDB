﻿using System;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

namespace NexusMods.EventSourcing.DatomStore;

public static class BuiltInAttributes
{
    public static void AddBuiltInAttributes(this IServiceCollection services) =>
        services.AddAttribute<UniqueId>()
            .AddAttribute<ValueSerializerId>();

    /// <summary>
    /// The unique identifier of the entity, used to link attributes across application restarts and model changes.
    /// </summary>
    public class UniqueId() : ScalarAttribute<UniqueId, UInt128>(UniqueIdStaticId);

    /// <summary>
    /// Static unique id of the UniqueId attribute
    /// </summary>
    public static UInt128 UniqueIdStaticId = "083F3E32-C1CE-4231-BDE4-FAD045D8126C".ToUInt128Guid();

    /// <summary>
    /// The database entity id of the UniqueId attribute
    /// </summary>
    public static ulong UniqueIdEntityId = 1;

    /// <summary>
    /// The unique id if the IValueSerializer used to serialize the value of the attribute.
    /// </summary>
    public class ValueSerializerId() : ScalarAttribute<ValueSerializerId, UInt128>(ValueSerializerIdStaticId);

    /// <summary>
    /// Static unique id of the UniqueId attribute
    /// </summary>
    public static UInt128 ValueSerializerIdStaticId = "602279C9-B37B-4487-B36B-99DCA4A2475A".ToUInt128Guid();

    /// <summary>
    /// The database entity id of the UniqueId attribute
    /// </summary>
    public static ulong ValueSerializerIdEntityId = 2;


    /// <summary>
    /// The initial set of built-in attributes that always exist in the database.
    /// </summary>
    public static DbAttribute[] Initial = [
        new DbAttribute(UniqueIdStaticId, UniqueIdEntityId, UInt128Serializer.Id),
        new DbAttribute(ValueSerializerIdStaticId, ValueSerializerIdEntityId, UInt128Serializer.Id),
    ];

    public static IDatom[] InitialDatoms = [
        UniqueId.Assert(UniqueIdEntityId, UniqueIdStaticId),
        ValueSerializerId.Assert(UniqueIdEntityId, UInt128Serializer.Id),

        UniqueId.Assert(ValueSerializerIdEntityId, ValueSerializerIdStaticId),
        ValueSerializerId.Assert(ValueSerializerIdEntityId, UInt128Serializer.Id),
    ];

}