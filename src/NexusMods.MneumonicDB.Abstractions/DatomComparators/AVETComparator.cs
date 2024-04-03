﻿using NexusMods.MneumonicDB.Abstractions.ElementComparers;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

namespace NexusMods.MneumonicDB.Abstractions.DatomComparators;

/// <summary>
/// The AVET Comparator.
/// </summary>
/// <typeparam name="TRegistry"></typeparam>
public class AVETComparator<TRegistry>(TRegistry registry) : ADatomComparator<
    AComparer<TRegistry>,
    ValueComparer<TRegistry>,
    EComparer<TRegistry>,
    TxComparer<TRegistry>,
    AssertComparer<TRegistry>,
    TRegistry>(registry)
    where TRegistry : IAttributeRegistry;