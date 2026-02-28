namespace StockSharp.Tests;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public partial class CSharpTests
{

    [TestMethod]
    public Task ZeroLagMaTrendFollowing()
        => RunStrategy<ZeroLagMaTrendFollowingStrategy>();

    [TestMethod]
    public Task ZeroLagMacdCrossover()
        => RunStrategy<ZeroLagMacdCrossoverStrategy>();

    [TestMethod]
    public Task ZeroLagMacdKijunSenEom()
        => RunStrategy<ZeroLagMacdKijunSenEomStrategy>();

    [TestMethod]
    public Task ZeroLagMacd()
        => RunStrategy<ZeroLagMacdStrategy>();

    [TestMethod]
    public Task ZeroLagTemaCrossesPakun()
        => RunStrategy<ZeroLagTemaCrossesPakunStrategy>();

    [TestMethod]
    public Task ZeroLagVolatilityBreakoutEmaTrend()
        => RunStrategy<ZeroLagVolatilityBreakoutEmaTrendStrategy>();

    [TestMethod]
    public Task ZigAndZagScalpel()
        => RunStrategy<ZigAndZagScalpelStrategy>();

    [TestMethod]
    public Task ZigAndZagTrader()
        => RunStrategy<ZigAndZagTraderStrategy>();

    [TestMethod]
    public Task ZigDanZagUltimateInvestmentLongTerm()
        => RunStrategy<ZigDanZagUltimateInvestmentLongTermStrategy>();

    [TestMethod]
    public Task ZigZagAroon()
        => RunStrategy<ZigZagAroonStrategy>();

    [TestMethod]
    public Task ZigZagClimber()
        => RunStrategy<ZigZagClimberStrategy>();

    [TestMethod]
    public Task ZigZagEA()
        => RunStrategy<ZigZagEAStrategy>();

    [TestMethod]
    public Task ZigZagEvgeTrofi1()
        => RunStrategy<ZigZagEvgeTrofi1Strategy>();

    [TestMethod]
    public Task ZigZagEvgeTrofi()
        => RunStrategy<ZigZagEvgeTrofiStrategy>();

    [TestMethod]
    public Task ZigzagCandles()
        => RunStrategy<ZigzagCandlesStrategy>();

    [TestMethod]
    public Task ZmfxStolid5aEa()
        => RunStrategy<ZmfxStolid5aEaStrategy>();

    [TestMethod]
    public Task ZonalTradingOscillator()
        => RunStrategy<ZonalTradingOscillatorStrategy>();

    [TestMethod]
    public Task ZonalTrading()
        => RunStrategy<ZonalTradingStrategy>();

    [TestMethod]
    public Task ZoneRecoveryArea()
        => RunStrategy<ZoneRecoveryAreaStrategy>();

    [TestMethod]
    public Task ZoneRecoveryButton()
        => RunStrategy<ZoneRecoveryButtonStrategy>();

    [TestMethod]
    public Task ZoneRecoveryFormula()
        => RunStrategy<ZoneRecoveryFormulaStrategy>();

    [TestMethod]
    public Task ZoneRecoveryHedge()
        => RunStrategy<ZoneRecoveryHedgeStrategy>();

    [TestMethod]
    public Task Zpf()
        => RunStrategy<ZpfStrategy>();

    [TestMethod]
    public Task Zs1ForexInstruments()
        => RunStrategy<Zs1ForexInstrumentsStrategy>();

    [TestMethod]
    public Task iCHO_Trend_CCIDualOnMA_Filter()
        => RunStrategy<iCHO_Trend_CCIDualOnMA_FilterStrategy>();

}
