using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA pullback strategy with dynamic length and speed filters.
/// Enters when price returns to a dynamic EMA with candle confirmation and speed thresholds.
/// </summary>
public class EmaPullbackSpeedStrategy : Strategy
	{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxLength;
	private readonly StrategyParam<decimal> _accelMultiplier;
	private readonly StrategyParam<decimal> _returnThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _fixedTpPct;
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<decimal> _longSpeedMin;
	private readonly StrategyParam<decimal> _shortSpeedMax;

	private ExponentialMovingAverage _emaShort;
	private ExponentialMovingAverage _emaLong;
	private AverageTrueRange _atr;
	private Highest _maxAbsDiff;
	private Highest _maxDeltaDiff;

	private decimal? _dynEma;
	private decimal _prevCountsDiff;
	private ICandleMessage _prevCandle1;
	private ICandleMessage _prevCandle2;
	private decimal _longSl;
	private decimal _longTp;
	private decimal _shortSl;
	private decimal _shortTp;

/// <summary>
/// Candle type used by the strategy.
/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Maximum dynamic EMA length.
/// </summary>
	public int MaxLength { get => _maxLength.Value; set => _maxLength.Value = value; }

/// <summary>
/// Accelerator multiplier for dynamic EMA.
/// </summary>
	public decimal AccelMultiplier { get => _accelMultiplier.Value; set => _accelMultiplier.Value = value; }

/// <summary>
/// Pullback distance threshold in percent.
/// </summary>
	public decimal ReturnThreshold { get => _returnThreshold.Value; set => _returnThreshold.Value = value; }

/// <summary>
/// ATR period for stop and target calculation.
/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

/// <summary>
/// ATR multiplier for stop-loss.
/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

/// <summary>
/// Fixed take profit percent.
/// </summary>
	public decimal FixedTpPct { get => _fixedTpPct.Value; set => _fixedTpPct.Value = value; }

/// <summary>
/// Short EMA period.
/// </summary>
	public int ShortEmaLength { get => _shortEmaLength.Value; set => _shortEmaLength.Value = value; }

/// <summary>
/// Long EMA period.
/// </summary>
	public int LongEmaLength { get => _longEmaLength.Value; set => _longEmaLength.Value = value; }

/// <summary>
/// Minimum speed for long entry.
/// </summary>
	public decimal LongSpeedMin { get => _longSpeedMin.Value; set => _longSpeedMin.Value = value; }

/// <summary>
/// Maximum speed for short entry.
/// </summary>
	public decimal ShortSpeedMax { get => _shortSpeedMax.Value; set => _shortSpeedMax.Value = value; }

/// <summary>
/// Initializes a new instance of the <see cref="EmaPullbackSpeedStrategy"/>.
/// </summary>
	public EmaPullbackSpeedStrategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles", "General");

	_maxLength = Param(nameof(MaxLength), 50)
	.SetRange(10, 200)
	.SetDisplay("Maximum Length", "Maximum dynamic EMA length", "Dynamic EMA")
	.SetCanOptimize(true);

	_accelMultiplier = Param(nameof(AccelMultiplier), 3m)
	.SetRange(1m, 10m)
	.SetDisplay("Accel Multiplier", "Accelerator multiplier", "Dynamic EMA")
	.SetCanOptimize(true);

	_returnThreshold = Param(nameof(ReturnThreshold), 5m)
	.SetRange(0.5m, 20m)
	.SetDisplay("Pullback Threshold %", "Pullback distance", "Dynamic EMA")
	.SetCanOptimize(true);

	_atrLength = Param(nameof(AtrLength), 14)
	.SetRange(5, 50)
	.SetDisplay("ATR Period", "ATR period", "Risk Management");

	_atrMultiplier = Param(nameof(AtrMultiplier), 4m)
	.SetRange(1m, 10m)
	.SetDisplay("ATR Multiplier", "ATR stop-loss multiplier", "Risk Management")
	.SetCanOptimize(true);

	_fixedTpPct = Param(nameof(FixedTpPct), 1.5m)
	.SetRange(0.5m, 10m)
	.SetDisplay("Fixed TP %", "Take profit percent", "Risk Management")
	.SetCanOptimize(true);

	_shortEmaLength = Param(nameof(ShortEmaLength), 21)
	.SetRange(5, 100)
	.SetDisplay("Short EMA Length", "Fast EMA length", "EMA Filter")
	.SetCanOptimize(true);

	_longEmaLength = Param(nameof(LongEmaLength), 50)
	.SetRange(10, 200)
	.SetDisplay("Long EMA Length", "Slow EMA length", "EMA Filter")
	.SetCanOptimize(true);

	_longSpeedMin = Param(nameof(LongSpeedMin), 1000m)
	.SetDisplay("Long Speed Min", "Minimum speed for long", "Speed")
	.SetCanOptimize(true);

	_shortSpeedMax = Param(nameof(ShortSpeedMax), -1000m)
	.SetDisplay("Short Speed Max", "Maximum speed for short", "Speed")
	.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_dynEma = null;
	_prevCountsDiff = 0m;
	_prevCandle1 = null;
	_prevCandle2 = null;
	_emaShort = null;
	_emaLong = null;
	_atr = null;
	_maxAbsDiff = null;
	_maxDeltaDiff = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_emaShort = new ExponentialMovingAverage { Length = ShortEmaLength };
	_emaLong = new ExponentialMovingAverage { Length = LongEmaLength };
	_atr = new AverageTrueRange { Length = AtrLength };
	_maxAbsDiff = new Highest { Length = 200 };
	_maxDeltaDiff = new Highest { Length = 200 };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_emaShort, _emaLong, _atr, ProcessCandle)
	.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShort, decimal emaLong, decimal atr)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var close = candle.ClosePrice;
	var countsDiff = close;
	var maxAbsCountsDiff = _maxAbsDiff.Process(candle.OpenTime, Math.Abs(countsDiff)).GetValue<decimal>();
	var deltaCountsDiff = Math.Abs(countsDiff - _prevCountsDiff);
	var maxDeltaCountsDiff = _maxDeltaDiff.Process(candle.OpenTime, deltaCountsDiff).GetValue<decimal>();
	if (maxDeltaCountsDiff == 0)
	maxDeltaCountsDiff = 1m;

	var countsDiffNorm = (countsDiff + maxAbsCountsDiff) / (2m * maxAbsCountsDiff);
	var dynLength = 5m + countsDiffNorm * (MaxLength - 5m);
	var accelFactor = deltaCountsDiff / maxDeltaCountsDiff;
	var alphaBase = 2m / (dynLength + 1m);
	var alpha = Math.Min(1m, alphaBase * (1m + accelFactor * AccelMultiplier));
	_dynEma = _dynEma is null ? close : alpha * close + (1m - alpha) * _dynEma.Value;

	var isUptrend = close > _dynEma;
	var isDowntrend = close < _dynEma;
	var distance = Math.Abs(close - _dynEma.Value) / _dynEma.Value * 100m;
	var returnedToTrend = distance < ReturnThreshold;

	var bullishReversal = _prevCandle2 != null && _prevCandle1 != null &&
	_prevCandle2.ClosePrice > _prevCandle2.OpenPrice &&
	_prevCandle1.ClosePrice > _prevCandle1.OpenPrice &&
	close > _prevCandle1.HighPrice;

	var bearishReversal = _prevCandle2 != null && _prevCandle1 != null &&
	_prevCandle2.ClosePrice < _prevCandle2.OpenPrice &&
	_prevCandle1.ClosePrice < _prevCandle1.OpenPrice &&
	close < _prevCandle1.LowPrice;

	var speed = close - candle.OpenPrice;
	var trendSpeedUp = speed > 0m;
	var trendSpeedDn = speed < 0m;
	var trendSpeedFilterLong = speed >= LongSpeedMin;
	var trendSpeedFilterShort = speed <= ShortSpeedMax;

	var longCondition = isUptrend && bullishReversal && returnedToTrend && trendSpeedUp && emaShort > emaLong && trendSpeedFilterLong;
	var shortCondition = isDowntrend && bearishReversal && returnedToTrend && trendSpeedDn && emaShort < emaLong && trendSpeedFilterShort;

	if (longCondition && Position <= 0)
	{
	BuyMarket();
	_longSl = close - atr * AtrMultiplier;
	_longTp = close + close * FixedTpPct / 100m;
	}
	else if (shortCondition && Position >= 0)
	{
	SellMarket();
	_shortSl = close + atr * AtrMultiplier;
	_shortTp = close - close * FixedTpPct / 100m;
	}

	if (Position > 0)
	{
	if (close <= _longSl || close >= _longTp)
	SellMarket();
	}
	else if (Position < 0)
	{
	if (close >= _shortSl || close <= _shortTp)
	BuyMarket();
	}

	_prevCountsDiff = countsDiff;
	_prevCandle2 = _prevCandle1;
	_prevCandle1 = candle;
	}
	}
