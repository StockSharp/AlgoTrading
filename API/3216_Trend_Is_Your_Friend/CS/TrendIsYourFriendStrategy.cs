using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy based on the "Trend Is Your Friend" expert advisor.
/// Combines multi-timeframe MACD filtering with short-term momentum pattern checks
/// and adaptive risk management using Bollinger Bands exits, break-even protection
/// and trailing stops.
/// </summary>
public class TrendIsYourFriendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailingActivationPips;
	private readonly StrategyParam<decimal> _trailingDistancePips;

	private ExponentialMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private BollingerBands _bollinger = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private bool _macdReady;
	private decimal _macdMain;
	private decimal _macdSignal;

	private CandleSnapshot? _previousCandle;
	private CandleSnapshot? _twoBarsAgo;

	private decimal? _entryPrice;
	private decimal? _trailingStop;
	private bool _breakEvenApplied;

	private struct CandleSnapshot
	{
		public decimal Open;
		public decimal Close;
		public decimal High;
		public decimal Low;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrendIsYourFriendStrategy"/>.
	/// </summary>
	public TrendIsYourFriendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Entry Candle", "Timeframe used for entry logic", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Candle", "Higher timeframe used for MACD filter", "General");

		_fastMaLength = Param(nameof(FastMaLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Length of the fast EMA", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Length of the slow LWMA", "Indicators");

		_bollingerLength = Param(nameof(BollingerLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Length", "Length of Bollinger Bands", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break-Even", "Enable break-even stop adjustment", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
		.SetDisplay("Break-Even Trigger", "Profit in pips before stop is moved", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
		.SetDisplay("Break-Even Offset", "Offset applied when moving stop", "Risk");

		_useTrailing = Param(nameof(UseTrailing), true)
		.SetDisplay("Use Trailing", "Enable trailing stop logic", "Risk");

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 40m)
		.SetDisplay("Trailing Activation", "Profit in pips required to arm trailing", "Risk");

		_trailingDistancePips = Param(nameof(TrailingDistancePips), 40m)
		.SetDisplay("Trailing Distance", "Distance maintained by trailing stop", "Risk");
	}

	/// <summary>
	/// Type of candles used for entry logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type used for the MACD filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow LWMA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables break-even stop movement.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit required to activate break-even logic.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Offset applied when the stop is moved to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enables trailing stop logic.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Profit required before the trailing stop is armed.
	/// </summary>
	public decimal TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Distance maintained by the trailing stop once armed.
	/// </summary>
	public decimal TrailingDistancePips
	{
		get => _trailingDistancePips.Value;
		set => _trailingDistancePips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macdReady = false;
		_macdMain = 0m;
		_macdSignal = 0m;
		_previousCandle = null;
		_twoBarsAgo = null;
		_entryPrice = null;
		_trailingStop = null;
		_breakEvenApplied = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fastMa = new ExponentialMovingAverage { Length = FastMaLength };
	_slowMa = new LinearWeightedMovingAverage { Length = SlowMaLength };
_bollinger = new BollingerBands
{
	Length = BollingerLength,
	Width = BollingerWidth
};

_macd = new MovingAverageConvergenceDivergenceSignal
{
	Macd =
	{
		ShortMa = { Length = 12 },
		LongMa = { Length = 26 }
	},
	SignalMa = { Length = 9 }
};

var mainSubscription = SubscribeCandles(CandleType);
mainSubscription
.Bind(_fastMa, _slowMa, _bollinger, ProcessMainCandle)
.Start();

var macdSubscription = SubscribeCandles(MacdCandleType, allowBuildFromSmallerTimeFrame: true);
macdSubscription
.BindEx(_macd, ProcessMacdCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
	DrawCandles(area, mainSubscription);
	DrawIndicator(area, _fastMa);
	DrawIndicator(area, _slowMa);
	DrawIndicator(area, _bollinger);
	DrawOwnTrades(area);

	var macdArea = CreateChartArea();
	if (macdArea != null)
	{
		DrawIndicator(macdArea, _macd);
	}
}
}

private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!macdValue.IsFinal)
	return;

	if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macd)
	return;

	if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
	return;

	_macdMain = macdLine;
	_macdSignal = signalLine;
	_macdReady = true;
}

private void ProcessMainCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal middle, decimal upper, decimal lower)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_bollinger.IsFormed || !_macdReady)
	{
		StoreCandle(candle);
		return;
	}

	if (_previousCandle is null || _twoBarsAgo is null)
	{
		StoreCandle(candle);
		return;
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	{
		StoreCandle(candle);
		return;
	}

	// Manage an existing long position before looking for new entries.
	if (Position > 0m && _entryPrice.HasValue)
	{
		if (ManageLongPosition(candle, upper, lower))
		{
			StoreCandle(candle);
			return;
		}
	}
	// Manage an existing short position before looking for new entries.
	else if (Position < 0m && _entryPrice.HasValue)
	{
		if (ManageShortPosition(candle, upper, lower))
		{
			StoreCandle(candle);
			return;
		}
	}

	if (Position == 0m)
	{
		TryOpenPosition(candle, fastMa, slowMa);
	}

	StoreCandle(candle);
}

private void TryOpenPosition(ICandleMessage candle, decimal fastMa, decimal slowMa)
{
	var previous = _previousCandle.Value;
	var second = _twoBarsAgo.Value;

	// Calculate candle body directions to reproduce the original pattern detection.
	var directionPrev = previous.Open - previous.Close;
	var directionSecond = second.Open - second.Close;

	var bullishPattern = directionSecond > 0m && directionPrev < 0m && Math.Abs(directionPrev) > Math.Abs(directionSecond);
	var bearishPattern = directionSecond < 0m && directionPrev > 0m && Math.Abs(directionPrev) < Math.Abs(directionSecond);

	var macdBullish = _macdMain > _macdSignal;
	var macdBearish = _macdMain < _macdSignal;

	if (fastMa > slowMa && bullishPattern && macdBullish)
	{
		BuyMarket(Volume);
	}
	else if (fastMa < slowMa && bearishPattern && macdBearish)
	{
		SellMarket(Volume);
	}
}

private bool ManageLongPosition(ICandleMessage candle, decimal upper, decimal lower)
{
	var entry = _entryPrice!.Value;
	var priceStep = GetPriceStep();

	var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * priceStep : (decimal?)null;
	var stopLossDistance = StopLossPips > 0m ? StopLossPips * priceStep : (decimal?)null;

	// Check fixed take-profit before other exit rules.
	if (takeProfitDistance.HasValue && candle.HighPrice >= entry + takeProfitDistance.Value)
	{
		SellMarket(Position);
		return true;
	}

	// Hard stop-loss protects against adverse moves.
	if (stopLossDistance.HasValue && candle.LowPrice <= entry - stopLossDistance.Value)
	{
		SellMarket(Position);
		return true;
	}

	// Exit when price closes outside the Bollinger envelope.
	if (candle.ClosePrice >= upper)
	{
		SellMarket(Position);
		return true;
	}

	// Update dynamic protection after fixed rules.
	ApplyBreakEvenForLong(candle, entry, priceStep);
	ApplyTrailingForLong(candle, entry, priceStep);

	if (_trailingStop.HasValue && candle.LowPrice <= _trailingStop.Value)
	{
		SellMarket(Position);
		return true;
	}

	return false;
}

private bool ManageShortPosition(ICandleMessage candle, decimal upper, decimal lower)
{
	var entry = _entryPrice!.Value;
	var priceStep = GetPriceStep();

	var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * priceStep : (decimal?)null;
	var stopLossDistance = StopLossPips > 0m ? StopLossPips * priceStep : (decimal?)null;

	// Fixed take-profit check.
	if (takeProfitDistance.HasValue && candle.LowPrice <= entry - takeProfitDistance.Value)
	{
		BuyMarket(Math.Abs(Position));
		return true;
	}

	// Hard stop-loss check.
	if (stopLossDistance.HasValue && candle.HighPrice >= entry + stopLossDistance.Value)
	{
		BuyMarket(Math.Abs(Position));
		return true;
	}

	// Exit when the close breaks below the lower band.
	if (candle.ClosePrice <= lower)
	{
		BuyMarket(Math.Abs(Position));
		return true;
	}

	ApplyBreakEvenForShort(candle, entry, priceStep);
	ApplyTrailingForShort(candle, entry, priceStep);

	if (_trailingStop.HasValue && candle.HighPrice >= _trailingStop.Value)
	{
		BuyMarket(Math.Abs(Position));
		return true;
	}

	return false;
}

private void ApplyBreakEvenForLong(ICandleMessage candle, decimal entry, decimal priceStep)
{
	if (!UseBreakEven || BreakEvenTriggerPips <= 0m)
	return;

	if (_breakEvenApplied)
	return;

	var trigger = entry + BreakEvenTriggerPips * priceStep;
	if (candle.HighPrice < trigger)
	return;

	var offset = BreakEvenOffsetPips * priceStep;
	var newStop = entry + offset;

	_trailingStop = _trailingStop.HasValue ? Math.Max(_trailingStop.Value, newStop) : newStop;
	_breakEvenApplied = true;
}

private void ApplyBreakEvenForShort(ICandleMessage candle, decimal entry, decimal priceStep)
{
	if (!UseBreakEven || BreakEvenTriggerPips <= 0m)
	return;

	if (_breakEvenApplied)
	return;

	var trigger = entry - BreakEvenTriggerPips * priceStep;
	if (candle.LowPrice > trigger)
	return;

	var offset = BreakEvenOffsetPips * priceStep;
	var newStop = entry - offset;

	_trailingStop = _trailingStop.HasValue ? Math.Min(_trailingStop.Value, newStop) : newStop;
	_breakEvenApplied = true;
}

private void ApplyTrailingForLong(ICandleMessage candle, decimal entry, decimal priceStep)
{
	if (!UseTrailing || TrailingDistancePips <= 0m || TrailingActivationPips <= 0m)
	return;

	var activation = TrailingActivationPips * priceStep;
	if (candle.HighPrice - entry < activation)
	return;

	var distance = TrailingDistancePips * priceStep;
	var candidate = candle.HighPrice - distance;
	_trailingStop = _trailingStop.HasValue ? Math.Max(_trailingStop.Value, candidate) : candidate;
}

private void ApplyTrailingForShort(ICandleMessage candle, decimal entry, decimal priceStep)
{
	if (!UseTrailing || TrailingDistancePips <= 0m || TrailingActivationPips <= 0m)
	return;

	var activation = TrailingActivationPips * priceStep;
	if (entry - candle.LowPrice < activation)
	return;

	var distance = TrailingDistancePips * priceStep;
	var candidate = candle.LowPrice + distance;
	_trailingStop = _trailingStop.HasValue ? Math.Min(_trailingStop.Value, candidate) : candidate;
}

private void StoreCandle(ICandleMessage candle)
{
	_twoBarsAgo = _previousCandle;
	_previousCandle = new CandleSnapshot
	{
		Open = candle.OpenPrice,
		Close = candle.ClosePrice,
		High = candle.HighPrice,
		Low = candle.LowPrice
	};
}

private decimal GetPriceStep()
{
	var step = Security?.PriceStep ?? 0.0001m;
	return step > 0m ? step : 0.0001m;
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
	base.OnNewMyTrade(trade);

	if (trade.Order.Security != Security)
	return;

	if (Position == 0m)
	{
		ResetPositionState();
		return;
	}

	if (Position > 0m && trade.Trade.Side == Sides.Buy)
	{
		_entryPrice = trade.Trade.Price;
		_trailingStop = null;
		_breakEvenApplied = false;
	}
	else if (Position < 0m && trade.Trade.Side == Sides.Sell)
	{
		_entryPrice = trade.Trade.Price;
		_trailingStop = null;
		_breakEvenApplied = false;
	}
}

/// <inheritdoc />
protected override void OnPositionChanged(decimal delta)
{
	base.OnPositionChanged(delta);

	if (Position == 0m)
	{
		ResetPositionState();
	}
}

private void ResetPositionState()
{
	_entryPrice = null;
	_trailingStop = null;
	_breakEvenApplied = false;
}
}

