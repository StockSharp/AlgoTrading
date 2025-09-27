namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Port of the MetaTrader expert revised_self_adaptive_ea.
/// Detects bullish and bearish engulfing patterns confirmed by RSI and a trend moving average.
/// Applies ATR based risk management with optional trailing stop supervision.
/// </summary>
public class RevisedSelfAdaptiveEaStrategy : Strategy
{
	private readonly StrategyParam<int> _averageBodyPeriod;
	private readonly StrategyParam<int> _movingAveragePeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _maxRiskPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<decimal> _trailingStopAtrMultiplier;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private SimpleMovingAverage _movingAverage = null!;
	private AverageTrueRange _atr = null!;
	private SimpleMovingAverage _bodyAverage = null!;

	private ICandleMessage _previousCandle;

	private decimal _lastAtrValue;
	private decimal _averageBodyValue;
	private decimal _pipSize;
	private bool _pipSizeInitialized;

	private decimal? _longEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _longTrailingStopPrice;

	private decimal? _shortEntryPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _shortTrailingStopPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="RevisedSelfAdaptiveEaStrategy"/>.
	/// </summary>
	public RevisedSelfAdaptiveEaStrategy()
	{
		_averageBodyPeriod = Param(nameof(AverageBodyPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);

		_movingAveragePeriod = Param(nameof(MovingAveragePeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("MA period", "Simple moving average period used as a directional filter.", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(2, 30, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("RSI period", "Length of the RSI oscillator applied to candle closes.", "Oscillator")
		.SetCanOptimize(true)
		.SetOptimize(3, 30, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR period", "Average True Range period that controls stop distances.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(7, 50, 1);

		_volume = Param(nameof(TradeVolume), 0.05m)
		.SetGreaterThanZero()
		.SetDisplay("Trade volume", "Base position size expressed in lots.", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20m)
		.SetNotNegative()
		.SetDisplay("Max spread", "Maximum allowed spread expressed in points.", "Risk");

		_maxRiskPercent = Param(nameof(MaxRiskPercent), 1m)
		.SetNotNegative()
		.SetDisplay("Max risk percent", "Maximum percentage of the portfolio equity accepted per trade.", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use trailing stop", "Enable ATR driven trailing stop supervision.", "Risk");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m)
		.SetNotNegative()
		.SetDisplay("Stop loss ATR multiplier", "Number of ATRs used to place the protective stop.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 4m)
		.SetNotNegative()
		.SetDisplay("Take profit ATR multiplier", "Number of ATRs used to place the profit target.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 8m, 0.5m);

		_trailingStopAtrMultiplier = Param(nameof(TrailingStopAtrMultiplier), 1.5m)
		.SetNotNegative()
		.SetDisplay("Trailing stop ATR multiplier", "ATR distance maintained by the trailing stop logic.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
		.SetNotNegative()
		.SetDisplay("Oversold level", "RSI threshold that confirms bullish reversals.", "Oscillator")
		.SetCanOptimize(true)
		.SetOptimize(10m, 50m, 5m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
		.SetNotNegative()
		.SetDisplay("Overbought level", "RSI threshold that confirms bearish reversals.", "Oscillator")
		.SetCanOptimize(true)
		.SetOptimize(50m, 90m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle type", "Time frame used for pattern detection.", "General");
	}

/// <summary>
/// Rolling period used to evaluate the average candle body size.
/// </summary>
public int AverageBodyPeriod
{
	get => _averageBodyPeriod.Value;
	set => _averageBodyPeriod.Value = value;
}

/// <summary>
/// Period applied to the simple moving average filter.
/// </summary>
public int MovingAveragePeriod
{
	get => _movingAveragePeriod.Value;
	set => _movingAveragePeriod.Value = value;
}

/// <summary>
/// RSI length used to confirm overbought and oversold conditions.
/// </summary>
public int RsiPeriod
{
	get => _rsiPeriod.Value;
	set => _rsiPeriod.Value = value;
}

/// <summary>
/// Average True Range period that controls risk distances.
/// </summary>
public int AtrPeriod
{
	get => _atrPeriod.Value;
	set => _atrPeriod.Value = value;
}

/// <summary>
/// Default trade volume.
/// </summary>
public decimal TradeVolume
{
	get => _volume.Value;
	set => _volume.Value = value;
}

/// <summary>
/// Maximum allowed spread measured in points.
/// </summary>
public decimal MaxSpreadPoints
{
	get => _maxSpreadPoints.Value;
	set => _maxSpreadPoints.Value = value;
}

/// <summary>
/// Maximum portion of portfolio equity that can be exposed per trade.
/// </summary>
public decimal MaxRiskPercent
{
	get => _maxRiskPercent.Value;
	set => _maxRiskPercent.Value = value;
}

/// <summary>
/// Enables the ATR based trailing stop controller.
/// </summary>
public bool UseTrailingStop
{
	get => _useTrailingStop.Value;
	set => _useTrailingStop.Value = value;
}

/// <summary>
/// ATR multiplier used to position the stop loss.
/// </summary>
public decimal StopLossAtrMultiplier
{
	get => _stopLossAtrMultiplier.Value;
	set => _stopLossAtrMultiplier.Value = value;
}

/// <summary>
/// ATR multiplier used to position the take profit target.
/// </summary>
public decimal TakeProfitAtrMultiplier
{
	get => _takeProfitAtrMultiplier.Value;
	set => _takeProfitAtrMultiplier.Value = value;
}

/// <summary>
/// ATR multiplier that defines the trailing stop distance.
/// </summary>
public decimal TrailingStopAtrMultiplier
{
	get => _trailingStopAtrMultiplier.Value;
	set => _trailingStopAtrMultiplier.Value = value;
}

/// <summary>
/// RSI threshold that validates bullish signals.
/// </summary>
public decimal OversoldLevel
{
	get => _oversoldLevel.Value;
	set => _oversoldLevel.Value = value;
}

/// <summary>
/// RSI threshold that validates bearish signals.
/// </summary>
public decimal OverboughtLevel
{
	get => _overboughtLevel.Value;
	set => _overboughtLevel.Value = value;
}

/// <summary>
/// Candle type consumed by the strategy.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
	base.OnReseted();

	_previousCandle = null;
	_lastAtrValue = 0m;
	_averageBodyValue = 0m;
	_pipSize = 0m;
	_pipSizeInitialized = false;
	ResetLongRiskLevels();
	ResetShortRiskLevels();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	Volume = TradeVolume;

	_rsi = new RelativeStrengthIndex
	{
		Length = RsiPeriod
	};

_movingAverage = new SimpleMovingAverage
{
	Length = MovingAveragePeriod
};

_atr = new AverageTrueRange
{
	Length = AtrPeriod
};

_bodyAverage = new SimpleMovingAverage
{
	Length = AverageBodyPeriod
};

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_rsi, _movingAverage, _atr, ProcessCandle)
.Start();

StartProtection();
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
	base.OnNewMyTrade(trade);

	var order = trade.Order;
	if (order == null)
	return;

	if (order.Direction == Sides.Buy)
	{
		if (Position > 0m)
		{
			// Long exposure established, compute fresh protective levels.
			_longEntryPrice = trade.Price;
			InitializeLongRiskLevels(trade.Price);
		}
	else if (Position >= 0m)
	{
		// Short exposure was reduced or closed.
		ResetShortRiskLevels();
	}
}
else if (order.Direction == Sides.Sell)
{
	if (Position < 0m)
	{
		// Short exposure established, compute protective levels.
		_shortEntryPrice = trade.Price;
		InitializeShortRiskLevels(trade.Price);
	}
else if (Position <= 0m)
{
	// Long exposure was reduced or closed.
	ResetLongRiskLevels();
}
}
}

/// <inheritdoc />
protected override void OnPositionChanged(decimal delta)
{
	base.OnPositionChanged(delta);

	if (Position == 0m)
	{
		// Flat state clears pending protective levels.
		ResetLongRiskLevels();
		ResetShortRiskLevels();
	}
}

private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal maValue, decimal atrValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!_pipSizeInitialized)
	InitializePipSize();

	_lastAtrValue = atrValue;
	UpdateAverageBody(candle);
	ManageOpenPositions(candle);

	if (!_rsi.IsFormed || !_movingAverage.IsFormed || !_atr.IsFormed)
	{
		_previousCandle = candle;
		return;
	}

if (_previousCandle == null)
{
	_previousCandle = candle;
	return;
}

if (!IsSpreadWithinLimit())
{
	_previousCandle = candle;
	return;
}

var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
var minimumBody = _averageBodyValue > 0m ? _averageBodyValue : 0m;

var bullishEngulfing = candle.ClosePrice > candle.OpenPrice &&
_previousCandle.ClosePrice < _previousCandle.OpenPrice &&
candle.OpenPrice <= _previousCandle.ClosePrice &&
bodySize >= minimumBody;

var bearishEngulfing = candle.ClosePrice < candle.OpenPrice &&
_previousCandle.ClosePrice > _previousCandle.OpenPrice &&
candle.OpenPrice >= _previousCandle.ClosePrice &&
bodySize >= minimumBody;

if (bullishEngulfing && rsiValue <= OversoldLevel && candle.ClosePrice >= maValue)
{
	TryOpenLong(candle.ClosePrice);
}
else if (bearishEngulfing && rsiValue >= OverboughtLevel && candle.ClosePrice <= maValue)
{
	TryOpenShort(candle.ClosePrice);
}

_previousCandle = candle;
}

private void TryOpenLong(decimal price)
{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (Position > 0m)
	return;

	if (ShouldBlockByRisk(price))
	return;

	if (Position < 0m)
	{
		// Close the opposing short exposure before flipping direction.
		ClosePosition();
		return;
	}

var volume = GetTradeVolume();
if (volume <= 0m)
return;

// Market order is used to replicate the MetaTrader execution style.
BuyMarket(volume);
}

private void TryOpenShort(decimal price)
{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (Position < 0m)
	return;

	if (ShouldBlockByRisk(price))
	return;

	if (Position > 0m)
	{
		// Close the opposing long exposure before flipping direction.
		ClosePosition();
		return;
	}

var volume = GetTradeVolume();
if (volume <= 0m)
return;

SellMarket(volume);
}

private void ManageOpenPositions(ICandleMessage candle)
{
	if (Position > 0m)
	{
		// Manage protective logic for long exposure.
		var exitVolume = Position;

		if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
		{
			SellMarket(exitVolume);
			return;
		}

	if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
	{
		SellMarket(exitVolume);
		return;
	}

if (UseTrailingStop && _longTrailingStopPrice.HasValue && _lastAtrValue > 0m)
{
	var candidate = candle.ClosePrice - _lastAtrValue * TrailingStopAtrMultiplier;
	if (candidate > _longTrailingStopPrice.Value)
	_longTrailingStopPrice = candidate;

	if (candle.LowPrice <= _longTrailingStopPrice.Value)
	{
		SellMarket(exitVolume);
		return;
	}
}
}
else if (Position < 0m)
{
	// Manage protective logic for short exposure.
	var exitVolume = Math.Abs(Position);

	if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
	{
		BuyMarket(exitVolume);
		return;
	}

if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
{
	BuyMarket(exitVolume);
	return;
}

if (UseTrailingStop && _shortTrailingStopPrice.HasValue && _lastAtrValue > 0m)
{
	var candidate = candle.ClosePrice + _lastAtrValue * TrailingStopAtrMultiplier;
	if (candidate < _shortTrailingStopPrice.Value)
	_shortTrailingStopPrice = candidate;

	if (candle.HighPrice >= _shortTrailingStopPrice.Value)
	{
		BuyMarket(exitVolume);
		return;
	}
}
}
}

private decimal GetTradeVolume()
{
	var volume = TradeVolume;
	var security = Security;
	if (security == null)
	return volume;

	var step = security.VolumeStep;
	if (step > 0m)
	{
		var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
		volume = steps * step;
	}

var minVolume = security.MinVolume;
if (minVolume.HasValue && volume < minVolume.Value)
volume = minVolume.Value;

var maxVolume = security.MaxVolume;
if (maxVolume.HasValue && volume > maxVolume.Value)
volume = maxVolume.Value;

return volume;
}

private bool ShouldBlockByRisk(decimal price)
{
	if (MaxRiskPercent <= 0m)
	return false;

	if (_lastAtrValue <= 0m || StopLossAtrMultiplier <= 0m || price <= 0m)
	return false;

	var potentialLoss = _lastAtrValue * StopLossAtrMultiplier;
	var riskPercent = potentialLoss / price * 100m;
	return riskPercent > MaxRiskPercent;
}

private void UpdateAverageBody(ICandleMessage candle)
{
	var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
	var value = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, body, candle.OpenTime));
	if (value.IsFinal && value.TryGetValue(out decimal average))
	_averageBodyValue = average;
}

private void InitializePipSize()
{
	_pipSize = GetPipSize();
	_pipSizeInitialized = _pipSize > 0m;
}

private decimal GetPipSize()
{
	var security = Security;
	if (security == null)
	return 0.0001m;

	var step = security.PriceStep;
	if (step > 0m)
	return step;

	var decimals = security.Decimals;
	if (decimals.HasValue)
	{
		var pow = Math.Pow(10, -decimals.Value);
		return Convert.ToDecimal(pow);
	}

return 0.0001m;
}

private bool IsSpreadWithinLimit()
{
	if (MaxSpreadPoints <= 0m)
	return true;

	var security = Security;
	if (security == null)
	return true;

	var bestBid = security.BestBid?.Price;
	var bestAsk = security.BestAsk?.Price;
	if (!bestBid.HasValue || !bestAsk.HasValue || !_pipSizeInitialized || _pipSize <= 0m)
	return true;

	var spreadPoints = (bestAsk.Value - bestBid.Value) / _pipSize;
	return spreadPoints <= MaxSpreadPoints;
}

private void InitializeLongRiskLevels(decimal entryPrice)
{
	if (_lastAtrValue <= 0m)
	{
		ResetLongRiskLevels();
		return;
	}

_longStopPrice = StopLossAtrMultiplier > 0m ? entryPrice - _lastAtrValue * StopLossAtrMultiplier : null;
_longTakeProfitPrice = TakeProfitAtrMultiplier > 0m ? entryPrice + _lastAtrValue * TakeProfitAtrMultiplier : null;
_longTrailingStopPrice = UseTrailingStop && TrailingStopAtrMultiplier > 0m ? entryPrice - _lastAtrValue * TrailingStopAtrMultiplier : null;
}

private void InitializeShortRiskLevels(decimal entryPrice)
{
	if (_lastAtrValue <= 0m)
	{
		ResetShortRiskLevels();
		return;
	}

_shortStopPrice = StopLossAtrMultiplier > 0m ? entryPrice + _lastAtrValue * StopLossAtrMultiplier : null;
_shortTakeProfitPrice = TakeProfitAtrMultiplier > 0m ? entryPrice - _lastAtrValue * TakeProfitAtrMultiplier : null;
_shortTrailingStopPrice = UseTrailingStop && TrailingStopAtrMultiplier > 0m ? entryPrice + _lastAtrValue * TrailingStopAtrMultiplier : null;
}

private void ResetLongRiskLevels()
{
	_longEntryPrice = null;
	_longStopPrice = null;
	_longTakeProfitPrice = null;
	_longTrailingStopPrice = null;
}

private void ResetShortRiskLevels()
{
	_shortEntryPrice = null;
	_shortStopPrice = null;
	_shortTakeProfitPrice = null;
	_shortTrailingStopPrice = null;
}
}

