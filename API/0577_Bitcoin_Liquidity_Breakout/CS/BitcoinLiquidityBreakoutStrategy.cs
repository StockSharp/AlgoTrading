using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin liquidity breakout strategy.
/// Enters long positions during high liquidity and volatility when fast trend is bullish.
/// </summary>
public class BitcoinLiquidityBreakoutStrategy : Strategy
{
private readonly StrategyParam<decimal> _liquidityThreshold;
private readonly StrategyParam<decimal> _priceChangeThreshold;
private readonly StrategyParam<int> _volatilityPeriod;
private readonly StrategyParam<int> _liquidityPeriod;
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<int> _rsiPeriod;
private readonly StrategyParam<decimal> _stopLossPercent;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<bool> _useStopLoss;
private readonly StrategyParam<bool> _useTakeProfit;
private readonly StrategyParam<DataType> _candleType;

private AverageTrueRange _atr;
private SimpleMovingAverage _atrMa;
private SimpleMovingAverage _volumeSma;

/// <summary>
/// Liquidity multiplier for volume SMA.
/// </summary>
public decimal LiquidityThreshold { get => _liquidityThreshold.Value; set => _liquidityThreshold.Value = value; }

/// <summary>
/// Minimum price change percentage.
/// </summary>
public decimal PriceChangeThreshold { get => _priceChangeThreshold.Value; set => _priceChangeThreshold.Value = value; }

/// <summary>
/// ATR calculation period.
/// </summary>
public int VolatilityPeriod { get => _volatilityPeriod.Value; set => _volatilityPeriod.Value = value; }

/// <summary>
/// Volume SMA period.
/// </summary>
public int LiquidityPeriod { get => _liquidityPeriod.Value; set => _liquidityPeriod.Value = value; }

/// <summary>
/// Fast moving average period.
/// </summary>
public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }

/// <summary>
/// Slow moving average period.
/// </summary>
public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }

/// <summary>
/// RSI period.
/// </summary>
public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

/// <summary>
/// Stop loss percentage.
/// </summary>
public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

/// <summary>
/// Take profit percentage.
/// </summary>
public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

/// <summary>
/// Enable stop loss.
/// </summary>
public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

/// <summary>
/// Enable take profit.
/// </summary>
public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

/// <summary>
/// Candle type for strategy.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public BitcoinLiquidityBreakoutStrategy()
{
_liquidityThreshold = Param(nameof(LiquidityThreshold), 1.3m)
.SetGreaterThanZero()
.SetDisplay("Liquidity Threshold", "Multiplier for volume SMA", "Indicators");

_priceChangeThreshold = Param(nameof(PriceChangeThreshold), 1.5m)
.SetGreaterThanZero()
.SetDisplay("Price Change %", "Minimum price change percentage", "Indicators");

_volatilityPeriod = Param(nameof(VolatilityPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("ATR Period", "ATR calculation length", "Indicators");

_liquidityPeriod = Param(nameof(LiquidityPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("Volume SMA Period", "Volume average length", "Indicators");

_fastMaPeriod = Param(nameof(FastMaPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Fast MA", "Fast moving average period", "Indicators");

_slowMaPeriod = Param(nameof(SlowMaPeriod), 21)
.SetGreaterThanZero()
.SetDisplay("Slow MA", "Slow moving average period", "Indicators");

_rsiPeriod = Param(nameof(RsiPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Period", "RSI calculation length", "Indicators");

_stopLossPercent = Param(nameof(StopLossPercent), 0.5m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

_takeProfitPercent = Param(nameof(TakeProfitPercent), 7m)
.SetGreaterThanZero()
.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management");

_useStopLoss = Param(nameof(UseStopLoss), true)
.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk Management");

_useTakeProfit = Param(nameof(UseTakeProfit), true)
.SetDisplay("Use Take Profit", "Enable take profit", "Risk Management");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Time frame for strategy", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_atr = new AverageTrueRange { Length = VolatilityPeriod };
_atrMa = new SimpleMovingAverage { Length = 10 };
_volumeSma = new SimpleMovingAverage { Length = LiquidityPeriod };

var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
var slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_atr, fastMa, slowMa, rsi, ProcessCandle)
.Start();

StartProtection(
UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new(),
UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : new());

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, fastMa);
DrawIndicator(area, slowMa);
DrawIndicator(area, rsi);
DrawIndicator(area, _atr);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal fastMaValue, decimal slowMaValue, decimal rsiValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var atrMaValue = _atrMa.Process(new DecimalIndicatorValue(_atrMa, atrValue, candle.Time)).ToDecimal();
var volumeSmaValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.Time)).ToDecimal();

var priceChange = 100m * (candle.HighPrice - candle.LowPrice) / candle.LowPrice;
var highLiquidity = candle.TotalVolume > volumeSmaValue * LiquidityThreshold;
var highPriceChange = priceChange > PriceChangeThreshold;
var highVolatility = atrValue > atrMaValue;

var buyCondition = highLiquidity && highPriceChange && fastMaValue > slowMaValue && rsiValue < 65m && highVolatility;
var sellCondition = fastMaValue < slowMaValue || rsiValue > 70m;

if (buyCondition && Position <= 0)
{
BuyMarket();
}
else if (sellCondition && Position > 0)
{
ClosePosition();
}
}
}
