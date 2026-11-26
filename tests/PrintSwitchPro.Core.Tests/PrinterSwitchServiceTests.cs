using PrintSwitchPro.Core.Models;
using PrintSwitchPro.Core.Services;

namespace PrintSwitchPro.Core.Tests;

public class PrinterSwitchServiceTests
{
    [Fact]
    public void Applies_Mapped_Printer_Once_Per_Process_Change()
    {
        var service = new PrinterSwitchService();
        var settings = new AppSettings
        {
            DefaultPrinter = "Default",
            Mappings =
            [
                new ProcessMapping { ProcessName = "WINWORD", MappedPrinter = "OfficePrinter" },
            ],
        };

        Assert.Equal("OfficePrinter", service.ResolvePrinterToApply("WINWORD", settings));
        Assert.Null(service.ResolvePrinterToApply("WINWORD", settings));
        Assert.Equal("Default", service.ResolvePrinterToApply("notepad", settings));
        Assert.Equal("OfficePrinter", service.ResolvePrinterToApply("WINWORD", settings));
    }

    [Fact]
    public void Returns_Null_When_No_Default_And_No_Mapping()
    {
        var service = new PrinterSwitchService();
        var settings = new AppSettings();
        Assert.Null(service.ResolvePrinterToApply("chrome", settings));
    }
}
