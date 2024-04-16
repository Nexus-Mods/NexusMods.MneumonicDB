﻿using System.Buffers;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Storage.Tests.TestAttributes;

public class Blobs
{
    private const string Namespace = "NexusMods.MnemonicDB.Storage.Tests.TestAttributes";

    public static readonly TestBlobAttribute InKeyBlob = new(Namespace, "InKeyBlob");
    public static readonly TestHashedBlobAttribute InValueBlob = new(Namespace, "InValueBlob");
}


public class TestBlobAttribute(string ns, string name) : BlobAttribute<byte[]>(ns, name)
{
    protected override byte[] FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag) => value.ToArray();

    protected override void WriteValue<TWriter>(byte[] value, TWriter writer)
    {
        writer.Write(value);
    }
}

public class TestHashedBlobAttribute(string ns, string name) : HashedBlobAttribute<byte[]>(ns, name)
{
    protected override byte[] FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag) => value.ToArray();

    protected override void WriteValue<TWriter>(byte[] value, TWriter writer)
    {
        writer.Write(value);
    }
}
