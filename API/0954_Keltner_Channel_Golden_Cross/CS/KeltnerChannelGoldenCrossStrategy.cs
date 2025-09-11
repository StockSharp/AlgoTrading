using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel strategy with Golden Cross filter.
/// </summary>
public class KeltnerChannelGoldenCrossStrategy : Strategy
{
private readonly StrategyParam<int> _maLength;
private readonly StrategyParam<decimal> _entryAtrMultiplier;
private readonly StrategyParam<decimal> _profitAtrMultiplier;
private readonly StrategyParam<decimal> _exitAtrMultiplier;
private readonly StrategyParam<MovingAverageTypeEnum> _maType;
private readonly StrategyParam<int> _shortMaLength;
private readonly StrategyParam<int> _longMaLength;
private readonly StrategyParam<DataType> _candleType;

/// <summary>
/// Basis MA length.
/// </summary>
public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

/// <summary>
/// ATR multiplier for entry channel.
/// </summary>
public decimal EntryAtrMultiplier { get => _entryAtrMultiplier.Value; set => _entryAtrMultiplier.Value = value; }

/// <summary>
/// ATR multiplier for take profit.
/// </summary>
public decimal ProfitAtrMultiplier { get => _profitAtrMultiplier.Value; set => _profitAtrMultiplier.Value = value; }

/// <summary>
/// ATR multiplier for stop.
/// </summary>
public decimal ExitAtrMultiplier { get => _exitAtrMultiplier.Value; set => _exitAtrMultiplier.Value = value; }

/// <summary>
/// Basis MA type.
/// </summary>
public MovingAverageTypeEnum MaType { get => _maType.Value; set => _maType.Value = value; }

/// <summary>
/// Short MA length for golden cross.
/// </summary>
public int ShortMaLength { get => _shortMaLength.Value; set => _shortMaLength.Value = value; }

/// <summary>
/// Long MA length for golden cross.
/// </summary>
public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }

/// <summary>
/// Candle type for strategy.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initializes a new instance of <see cref="KeltnerChannelGoldenCrossStrategy"/>.
/// </summary>
public KeltnerChannelGoldenCrossStrategy()
{
_maLength = Param(nameof(MaLength), 21)
.SetDisplay("MA Length", "Length for basis moving average", "General")
.SetGreaterThanZero()
.SetCanOptimize(true);

_entryAtrMultiplier = Param(nameof(EntryAtrMultiplier), 1m)
.SetDisplay("Entry ATR Mult", "ATR multiplier for entry channel", "Risk")
.SetCanOptimize(true);

_profitAtrMultiplier = Param(nameof(ProfitAtrMultiplier), 4m)
.SetDisplay("Profit Mult", "ATR multiplier for take profit", "Risk")
.SetCanOptimize(true);

_exitAtrMultiplier = Param(nameof(ExitAtrMultiplier), -1m)
.SetDisplay("Exit Mult", "ATR multiplier for stop", "Risk")
.SetCanOptimize(true);

_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
.SetDisplay("MA Type", "Type of basis moving average", "General");

_shortMaLength = Param(nameof(ShortMaLength), 50)
.SetDisplay("Short MA", "Short moving average length", "Trend")
.SetGreaterThanZero()
.SetCanOptimize(true);

_longMaLength = Param(nameof(LongMaLength), 200)
.SetDisplay("Long MA", "Long moving average length", "Trend")
.SetGreaterThanZero()
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var basis = CreateMa(MaType, MaLength);
var entryAtr = new AverageTrueRange { Length = 10 };
var atr = new AverageTrueRange { Length = MaLength };
var shortMa = new ExponentialMovingAverage { Length = ShortMaLength };
var longMa = new ExponentialMovingAverage { Length = LongMaLength };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(basis, entryAtr, atr, shortMa, longMa, ProcessCandle)
.Start();

StartProtection();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, basis);
DrawIndicator(area, shortMa);
DrawIndicator(area, longMa);
DrawOwnTrades(area);
}
}

private MovingAverage CreateMa(MovingAverageTypeEnum type, int length)
{
return type switch
{
MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
_ => new SimpleMovingAverage { Length = length },
};
}

private void ProcessCandle(ICandleMessage candle, decimal basis, decimal entryAtr, decimal atr, decimal shortMa, decimal longMa)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var price = candle.ClosePrice;
var upperEntry = basis + EntryAtrMultiplier * entryAtr;
var lowerEntry = basis - EntryAtrMultiplier * entryAtr;
var takeProfit = basis + ProfitAtrMultiplier * atr;
var takeProfitShort = basis - ProfitAtrMultiplier * atr;
var stopLong = basis + ExitAtrMultiplier * atr;
var stopShort = basis - ExitAtrMultiplier * atr;
var longTrend = shortMa > longMa;
var shortTrend = shortMa < longMa;

if (Position > 0)
{
if (price >= takeProfit || price <= stopLong)
SellMarket(Position);

return;
}

if (Position < 0)
{
if (price <= takeProfitShort || price >= stopShort)
BuyMarket(Math.Abs(Position));

return;
}

if (longTrend && price > upperEntry)
BuyMarket(Volume);
else if (shortTrend && price < lowerEntry)
SellMarket(Volume);
}

/// <summary>
/// Moving average type.
/// </summary>
public enum MovingAverageTypeEnum
{
/// <summary>
/// Simple moving average.
/// </summary>
Simple,

/// <summary>
/// Exponential moving average.
/// </summary>
Exponential,

/// <summary>
/// Weighted moving average.
/// </summary>
Weighted
}
}

