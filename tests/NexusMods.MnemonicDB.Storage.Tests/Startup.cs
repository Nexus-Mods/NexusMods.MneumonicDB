﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.TestModel.ValueSerializers;
using Xunit.DependencyInjection.Logging;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMnemonicDBStorage()
            .AddSingleton<Backend>()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddValueSerializer<RelativePathSerializer>()
            .AddValueSerializer<HashSerializer>()
            .AddValueSerializer<SizeSerializer>()
            .AddValueSerializer<UriSerializer>()
            .AddAttributeCollection(typeof(File))
            .AddAttributeCollection(typeof(Mod))
            .AddAttributeCollection(typeof(Collection))
            .AddAttributeCollection(typeof(Loadout));
    }
}
