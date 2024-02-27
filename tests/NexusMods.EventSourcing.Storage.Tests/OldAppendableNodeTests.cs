﻿using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Algorithms;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage.Tests;

public class OldAppendableNodeTests(IServiceProvider provider, IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    : AStorageTest(provider, valueSerializers, attributes)
{
    [Fact]
    public void CanAppendDataToBlock()
    {
        var block = new AppendableChunk();
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in TestData(10))
        {
            block.Append(datom);
        }

        block.Length.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
    }




    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    public void CanSortBlock(SortOrders order)
    {
        var block = new AppendableChunk();
        var allDatoms = TestData(10).ToArray();
        Random.Shared.Shuffle(allDatoms);

        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        var compare = new EATV(_registry);
        block.Sort(compare);

        var sorted = allDatoms.Order(CreateComparer(compare))
            .ToArray();

        block.Length.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = sorted[i];
            AssertEqual(datomA, datomB, i);
        }
    }


    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    public void CanMergeBlock(SortOrders orders)
    {
        var block = new AppendableChunk();
        var allDatoms = TestData(10).ToArray();

        Random.Shared.Shuffle(allDatoms);

        var block2 = new AppendableChunk();

        var half = allDatoms.Length / 2;
        for (var i = 0; i < half; i++)
        {
            block.Append(in allDatoms[i]);
        }

        for (var i = half; i < allDatoms.Length; i++)
        {
            block2.Append(in allDatoms[i]);
        }

        var compare = IDatomComparator.Create(orders, _registry);

        block.Sort(compare);
        block2.Sort(compare);

        var joined = SortedMerge.Merge(block, block2, compare);

        var sorted = allDatoms.Order(CreateComparer(compare))
            .ToArray();

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = joined[i];
            var datomB = sorted[i];
            AssertEqual(datomA, datomB, i);
        }
    }

    [Fact]
    public void CanSeekToDatom()
    {
        var compare = new EATV(_registry);
        var block = new AppendableChunk();
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in allDatoms)
        {
            block.Append(in datom);
        }

        var sorted = allDatoms.Order(CreateComparer(compare)).ToArray();
        block.Sort(compare);

        for (var i = 0; i < sorted.Length; i++)
        {
            var datom = sorted[i];
            var idx = BinarySearch.SeekEqualOrLess(block, compare, 0, block.Length, in datom);

            var found = block[idx];
            AssertEqual(found, datom, i);
        }
    }


    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(1024 * 8)]
    public void CanReadAndWriteBlocks(uint count)
    {
        var allDatoms = TestData(count).ToArray();
        var block = new AppendableChunk();
        foreach (var datom in allDatoms)
        {
            block.Append(in datom);
        }

        var packed = block.Pack();

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = packed[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }

        var writer = new PooledMemoryBufferWriter();
        packed.WriteTo(writer);


        Logger.LogInformation("Packed {0} datoms into {1} bytes", packed.Length, writer.WrittenMemory.Length);
        Logger.LogInformation("Average size: {0} bytes", writer.WrittenMemory.Length / packed.Length);
        var uncompressedSize = packed.Length * (8 + 8 + 1 + 8);
        Logger.LogInformation("Compression ratio: {0}%", (writer.WrittenMemory.Length * 100) / uncompressedSize);

        var read = ChunkReader.ReadDataChunk(writer.WrittenMemory);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = read[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
    }



    public IComparer<Datom> CreateComparer(IDatomComparator datomComparator)
    {
        return Comparer<Datom>.Create((a, b) => datomComparator.Compare(in a, in b));
    }

    public IEnumerable<Datom> TestData(uint max)
    {
        for (ulong eid = 0; eid < max; eid += 1)
        {
            for (ulong tx = 0; tx < 10; tx += 1)
            {
                for (ulong val = 1; val < 10; val += 1)
                {
                    yield return new Datom()
                    {
                        E = EntityId.From(eid),
                        A = AttributeId.From(10),
                        T = TxId.From(tx),
                        F = DatomFlags.Added,
                        V = BitConverter.GetBytes(val)
                    };
                    //yield return Assert<TestAttributes.FileHash>(eid, tx, val);
                    //yield return Assert<TestAttributes.FileName>(eid, tx, " file " + val);
                }
            }
        }
    }

}