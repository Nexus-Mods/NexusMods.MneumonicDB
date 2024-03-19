﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using NexusMods.EventSourcing.Storage.Nodes.Index;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public static class ExtensionMethods
{
    /// <summary>
    /// Returns the indices that would sort the <see cref="IReadable"/> according to the given <see cref="IDatomComparator"/>.
    /// </summary>
    public static int[] GetSortIndices(this IDatomResult readable, IDatomComparator comparator)
    {
        var pidxs = GC.AllocateUninitializedArray<int>((int)readable.Length);

        // TODO: may not matter, but we could probably use a vectorized version of this
        for (var i = 0; i < pidxs.Length; i++)
        {
            pidxs[i] = i;
        }

        var comp = comparator.MakeComparer(readable);
        Array.Sort(pidxs, 0, (int)readable.Length, comp);

        return pidxs;
    }

    /// <summary>
    /// Creates a new IReadable by creating a read-only view of a portion of the given IReadable.
    /// </summary>
    public static IDatomResult SubView(this IDatomResult src, int offset, int length)
    {
        throw new NotImplementedException();
        /*
        EnsureFrozen(src);
        Debug.Assert(offset >= 0 && length >= 0 && offset + length <= src.Length, "Index out of range during SubView creation");
        return new ReadableView(src, offset, length);
        */
    }




    /*

    private static int FindEATVReader(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        var start = 0;
        var end = readable.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.E.CompareTo(readable.GetEntityId(mid));
            if (cmp == 0)
            {
                var attrId = readable.GetAttributeId(mid);
                var attrCmp = target.A.CompareTo(attrId);
                if (attrCmp == 0)
                {
                    var tmpCmp = target.T.CompareTo(readable.GetTransactionId(mid));
                    if (tmpCmp == 0)
                    {
                        cmp = registry.CompareValues(attrId, target.V.Span, readable.GetValue(mid));
                    }
                    else
                    {
                        cmp = -tmpCmp;
                    }
                }
                else
                {
                    cmp = attrCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    private static int FindAETVReader(this IReadable src, in Datom target, IAttributeRegistry registry)
    {
        var start = 0;
        var end = src.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(src.GetAttributeId(mid));
            if (cmp == 0)
            {
                var entCmp = target.E.CompareTo(src.GetEntityId(mid));
                if (entCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(src.GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = registry.CompareValues(target.A, target.V.Span, src.GetValue(mid));
                    }
                    else
                    {
                        cmp = tCmp;
                    }

                }
                else
                {
                    cmp = entCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    private static int FindAVTEReader(this IReadable src, in Datom target, IAttributeRegistry registry)
    {
        var start = 0;
        var end = src.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(src.GetAttributeId(mid));
            if (cmp == 0)
            {
                var valueCmp = registry.CompareValues(target.A, target.V.Span, src.GetValue(mid));
                if (valueCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(src.GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = target.E.CompareTo(src.GetEntityId(mid));
                    }
                    else
                    {
                        cmp = tCmp;
                    }
                }
                else
                {
                    cmp = valueCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindEATV(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindEATVReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindAETV(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindAETVReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindAVTE(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindAVTEReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int Find(this IReadable readable, in Datom target, SortOrders order, IAttributeRegistry registry)
    {
        return order switch
        {
            SortOrders.EATV => readable.FindEATV(target, registry),
            SortOrders.AETV => readable.FindAETV(target, registry),
            SortOrders.AVTE => readable.FindAVTE(target, registry),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, "Unknown sort order")
        };
    }


    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int Find(this IReadable readable, in Datom target, IDatomComparator comparator)
    {
        return readable.Find(target, comparator.SortOrder, comparator.AttributeRegistry);
    }

    /// <summary>
    /// Slower version of <see cref="Pack"/> that requires copying every column into a span, then packing it.
    /// This is required for nodes that are not <see cref="IAppendable"/> and not <see cref="IPacked"/>, such as
    /// views and sorted nodes.
    /// </summary>
    private static IReadable PackSlow(this IReadable readable, INodeStore store)
    {
        return new DataPackedNode
        {
            Length = readable.Length,
            EntityIds = (ULongColumn)readable.EntityIdsColumn.Pack(),
            AttributeIds = (ULongColumn)readable.AttributeIdsColumn.Pack(),
            Values = (BlobColumn)readable.ValuesColumn.Pack(),
            TransactionIds = (ULongColumn)readable.TransactionIdsColumn.Pack()
        };
    }

    public static IReadable ReadDataNode(ReadOnlyMemory<byte> writerWrittenMemory)
    {
        var dataPackedNode = DataPackedNode.Serializer.Parse(writerWrittenMemory);
        return dataPackedNode;
    }

    public static IReadable Merge(this INode src, IReadable other, IDatomComparator comparator)
    {
        switch (src)
        {
            case EventSourcing.Abstractions.Nodes.Index.IReadable index:
                return MergeIndex(index, other, comparator);
            case IReadable readable:
                return MergeData(src, other, comparator);
            default:
                throw new NotImplementedException();
        }
    }

    private static IReadable MergeIndex(INode index, IReadable other, IDatomComparator comparator, INodeStore store)
    {
        if (index is EventSourcing.Abstractions.Nodes.Index.IAppendable appendable)
            return appendable.Ingest(other, store);
        throw new NotImplementedException();
    }

    private static IReadable MergeData(IReadable src, IReadable other, IDatomComparator comparator)
    {
        // TODO: use sorted merge, maybe?
        var appendable = Appendable.Create(src);
        appendable.Add(other);
        return appendable.AsSorted(comparator);
    }


    */
}
