using System.Xml;
using FxExchange.Core;

namespace FxExchange.Tests;

public class NationalbankenRateSourceTests
{
    [Fact]
    public void Parse_ReadsRatesFromXml()
    {
        var provider = NationalbankenRateSource.Parse(SampleData.NationalbankenXml);

        Assert.Equal(746.13m, provider.GetDkkPer100Units("EUR"));
        Assert.Equal(642.50m, provider.GetDkkPer100Units("USD"));
        Assert.Equal(4.1830m, provider.GetDkkPer100Units("JPY"));
        Assert.Equal(100m, provider.GetDkkPer100Units("DKK"));
    }

    [Fact]
    public void Parse_SkipsRowsWithInvalidRates()
    {
        Assert.Null(NationalbankenRateSource.Parse(SampleData.NationalbankenXml).GetDkkPer100Units("XXX"));
    }

    [Fact]
    public void Parse_AcceptsDecimalComma()
    {
        const string danishXml = """<exchangerates><dailyrates><currency code="EUR" rate="746,13" /></dailyrates></exchangerates>""";

        Assert.Equal(746.13m, NationalbankenRateSource.Parse(danishXml).GetDkkPer100Units("EUR"));
    }

    [Fact]
    public void Parse_NoRates_Throws()
    {
        Assert.Throws<ExchangeException>(() => NationalbankenRateSource.Parse("<exchangerates />"));
    }

    [Fact]
    public void Parse_NotXml_Throws()
    {
        Assert.Throws<XmlException>(() => NationalbankenRateSource.Parse("<html>oops"));
    }

    [Fact]
    public async Task FetchAsync_ParsesHttpResponse()
    {
        using var http = new HttpClient(new StubHandler(() => StubHandler.Ok(SampleData.NationalbankenXml)));

        var provider = await NationalbankenRateSource.FetchAsync(http);

        Assert.Equal(746.13m, provider.GetDkkPer100Units("EUR"));
    }
}
