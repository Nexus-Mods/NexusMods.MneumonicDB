﻿using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage;

public class RootNode(AttributeRegistry registry)
{
    public Index<EATV> EatvIndex = null!;
    public Index<TxLog> TxLog = null!;



    public void Ingest<TRawDatom>(RootNode prev, IEnumerator<TRawDatom> datoms)
    where TRawDatom : IRawDatom
    {
        /*
        var tmpTable = new OldAppendableNode(configuration);
        while(datoms.MoveNext())
        {
            tmpTable.Insert(datoms.Current, new TxLog(registry));
        }*/

        throw new NotImplementedException();

        /*
        TxLog.Ingest<AppendableBlock.FlyweightDatomEnumerator, AppendableBlock.FlyweightRawDatom>(tmpTable.GetEnumerator());

        tmpTable.Sort(new Eatv(registry));
        EatvIndex.Ingest<AppendableBlock.FlyweightDatomEnumerator, AppendableBlock.FlyweightRawDatom>(tmpTable.GetEnumerator());

        tmpTable.Sort(new Avte(registry));
        AvteIndex.Ingest<AppendableBlock.FlyweightDatomEnumerator, AppendableBlock.FlyweightRawDatom>(tmpTable.GetEnumerator());
        */


    }
}