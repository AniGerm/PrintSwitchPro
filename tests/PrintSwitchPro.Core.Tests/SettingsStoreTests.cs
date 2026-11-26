using PrintSwitchPro.Core.Models;
using PrintSwitchPro.Core.Storage;

namespace PrintSwitchPro.Core.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Roundtrips_Json_Settings()
    {
        var dir = Path.Combine(Path.GetTempPath(), "psp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var store = new SettingsStore(dir);
            var settings = new AppSettings
            {
                Language = "en",
                DefaultPrinter = "HP",
                AutoCheckUpdates = false,
                Mappings =
                [
                    new ProcessMapping { ProcessName = "excel", MappedPrinter = "Laser" },
                ],
            };
            store.Save(settings);

            var loaded = store.Load();
            Assert.Equal("en", loaded.Language);
            Assert.Equal("HP", loaded.DefaultPrinter);
            Assert.False(loaded.AutoCheckUpdates);
            Assert.Single(loaded.Mappings);
            Assert.Equal("excel", loaded.Mappings[0].ProcessName);
            Assert.Equal("Laser", loaded.Mappings[0].MappedPrinter);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Normalizes_Invalid_Language_To_German()
    {
        var dir = Path.Combine(Path.GetTempPath(), "psp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var store = new SettingsStore(dir);
            store.Save(new AppSettings { Language = "fr" });
            Assert.Equal("de", store.Load().Language);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
