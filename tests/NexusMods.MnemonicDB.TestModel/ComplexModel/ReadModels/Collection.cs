﻿using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;

public class Collection(ITransaction tx) : AEntity(tx)
{
    public string Name
    {
        get => CollectionAttributes.Name.Get(this);
        init => CollectionAttributes.Name.Add(this, value);
    }

    public Values<EntityId> ModIds => CollectionAttributes.Mods.GetAll(this);

    public Collection Attach(Mod mod)
    {
        CollectionAttributes.Mods.Add(this, mod.Id);
        return this;
    }

    public Entities<Values<EntityId>, Mod> Mods => CollectionAttributes.Mods.GetAll(this).As<Mod>(Db);

    public EntityId LoadoutId
    {
        get => CollectionAttributes.LoadoutId.Get(this);
        init => CollectionAttributes.LoadoutId.Add(this, value);
    }

    public Loadout Loadout
    {
        get => Db.Get<Loadout>(LoadoutId);
        init => CollectionAttributes.LoadoutId.Add(this, value.Id);
    }


}