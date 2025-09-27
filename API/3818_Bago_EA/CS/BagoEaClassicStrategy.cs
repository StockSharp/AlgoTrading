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
/// Port of the classic MetaTrader "Bago EA" (MQL/7656) trend-following expert.
/// Combines EMA and RSI confirmation with Vegas tunnel filters and layered trailing management to match the original behaviour.
/// </summary>
public class BagoEaClassicStrategy : Strategy
{

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _stopLossToFiboPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStep1Pips;
	private readonly StrategyParam<decimal> _trailingStep2Pips;
	private readonly StrategyParam<decimal> _trailingStep3Pips;
	private readonly StrategyParam<decimal> _partialClose1Volume;
	private readonly StrategyParam<decimal> _partialClose2Volume;
	private readonly StrategyParam<int> _crossEffectiveBars;
	private readonly StrategyParam<decimal> _tunnelBandWidthPips;
	private readonly StrategyParam<decimal> _tunnelSafeZonePips;
	private readonly StrategyParam<bool> _enableLondonSession;
	private readonly StrategyParam<bool> _enableNewYorkSession;
	private readonly StrategyParam<bool> _enableTokyoSession;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageType> _maMethod;
	private readonly StrategyParam<AppliedPriceType> _maAppliedPrice;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<AppliedPriceType> _rsiAppliedPrice;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _historyLimit;
	private readonly StrategyParam<decimal> _fiftyLevel;

	private LengthIndicator<decimal> _fastMa = null!;
	private LengthIndicator<decimal> _slowMa = null!;
	private LengthIndicator<decimal> _vegasFastMa = null!;
	private LengthIndicator<decimal> _vegasSlowMa = null!;
	private RelativeStrengthIndex _rsi = null!;

	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();
	private readonly List<decimal> _vegasFastHistory = new();
	private readonly List<decimal> _vegasSlowHistory = new();
	private readonly List<decimal> _rsiHistory = new();
	private readonly List<CandleSnapshot> _candleHistory = new();

	private bool _emaCrossedUp;
	private bool _emaCrossedDown;
	private bool _rsiCrossedUp;
	private bool _rsiCrossedDown;
	private bool _tunnelCrossedUp;
	private bool _tunnelCrossedDown;
	private int _emaCrossUpTimer;
	private int _emaCrossDownTimer;
	private int _rsiCrossUpTimer;
	private int _rsiCrossDownTimer;

	private decimal _pipSize;
	private decimal _stopLossDistance;
	private decimal _stopLossToFiboDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStep1Distance;
	private decimal _trailingStep2Distance;
	private decimal _trailingStep3Distance;
	private decimal _tunnelBandWidthDistance;
	private decimal _tunnelSafeZoneDistance;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private bool _longPartial1Done;
	private bool _longPartial2Done;
	private bool _shortPartial1Done;
	private bool _shortPartial2Done;

	private Order _longStopOrder;
	private Order _shortStopOrder;
	private decimal _longStopPrice;
	private decimal _shortStopPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="BagoEaClassicStrategy"/> class.
	/// </summary>
	public BagoEaClassicStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Executed lot size", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
		.SetDisplay("Stop Loss (pips)", "Initial protective stop distance", "Risk");

		_stopLossToFiboPips = Param(nameof(StopLossToFiboPips), 20m)
		.SetDisplay("Stop Loss to Fibo (pips)", "Extra offset when parking stops near the tunnel", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
		.SetDisplay("Trailing Stop (pips)", "Distance of the dynamic trailing stop", "Risk");

		_trailingStep1Pips = Param(nameof(TrailingStep1Pips), 55m)
		.SetDisplay("Trailing Step 1 (pips)", "First profit layer for partial exit", "Risk");

		_trailingStep2Pips = Param(nameof(TrailingStep2Pips), 89m)
		.SetDisplay("Trailing Step 2 (pips)", "Second profit layer for partial exit", "Risk");

		_trailingStep3Pips = Param(nameof(TrailingStep3Pips), 144m)
		.SetDisplay("Trailing Step 3 (pips)", "Final profit layer before switching to hard trailing", "Risk");

		_partialClose1Volume = Param(nameof(PartialClose1Volume), 1m)
		.SetDisplay("Partial Close #1", "Volume to close at the first trailing layer", "Risk");

		_partialClose2Volume = Param(nameof(PartialClose2Volume), 1m)
		.SetDisplay("Partial Close #2", "Volume to close at the second trailing layer", "Risk");

		_crossEffectiveBars = Param(nameof(CrossEffectiveBars), 2)
		.SetGreaterThanZero()
		.SetDisplay("Cross Validity", "Bars while EMA/RSI cross remains active", "Filters");

		_tunnelBandWidthPips = Param(nameof(TunnelBandWidthPips), 5m)
		.SetDisplay("Tunnel Band (pips)", "Neutral zone around the Vegas tunnel", "Filters");

		_tunnelSafeZonePips = Param(nameof(TunnelSafeZonePips), 120m)
		.SetDisplay("Tunnel Safe Zone (pips)", "Maximum distance allowed above the tunnel", "Filters");

		_enableLondonSession = Param(nameof(EnableLondonSession), true)
		.SetDisplay("Trade London", "Allow trading during London hours", "Sessions");

		_enableNewYorkSession = Param(nameof(EnableNewYorkSession), true)
		.SetDisplay("Trade New York", "Allow trading during New York hours", "Sessions");

		_enableTokyoSession = Param(nameof(EnableTokyoSession), false)
		.SetDisplay("Trade Tokyo", "Allow trading during Tokyo hours", "Sessions");

		_fastPeriod = Param(nameof(FastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA period", "Indicator");

		_maShift = Param(nameof(MaShift), 0)
		.SetDisplay("MA Shift", "Horizontal displacement in bars", "Indicator");

		_maMethod = Param(nameof(MaMethod), MovingAverageType.Exponential)
		.SetDisplay("MA Method", "Moving average calculation mode", "Indicator");

		_maAppliedPrice = Param(nameof(MaAppliedPrice), AppliedPriceType.Close)
		.SetDisplay("MA Price", "Applied price for moving averages", "Indicator");

		_rsiPeriod = Param(nameof(RsiPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI averaging length", "Indicator");

		_rsiAppliedPrice = Param(nameof(RsiAppliedPrice), AppliedPriceType.Close)
		.SetDisplay("RSI Price", "Applied price for RSI", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "General");
		_historyLimit = Param(nameof(HistoryLimit), 300)
			.SetDisplay("History Limit", "Maximum number of cached indicator values", "General")
			.SetGreaterThanZero();
		_fiftyLevel = Param(nameof(FiftyLevel), 50m)
			.SetDisplay("RSI Middle", "Neutral RSI level separating long and short signals", "Indicators")
			.SetNotNegative();
	}

/// <summary>
/// Executed order volume.
/// </summary>
public decimal TradeVolume
{
	get => _tradeVolume.Value;
	set => _tradeVolume.Value = value;
}

/// <summary>
/// Initial stop-loss in pips.
/// </summary>
public decimal StopLossPips
{
	get => _stopLossPips.Value;
	set => _stopLossPips.Value = value;
}

/// <summary>
/// Extra buffer applied when parking stops around the Vegas tunnel.
/// </summary>
public decimal StopLossToFiboPips
{
	get => _stopLossToFiboPips.Value;
	set => _stopLossToFiboPips.Value = value;
}

/// <summary>
/// Trailing stop distance in pips.
/// </summary>
public decimal TrailingStopPips
{
	get => _trailingStopPips.Value;
	set => _trailingStopPips.Value = value;
}

/// <summary>
/// First trailing layer distance in pips.
/// </summary>
public decimal TrailingStep1Pips
{
	get => _trailingStep1Pips.Value;
	set => _trailingStep1Pips.Value = value;
}

/// <summary>
/// Second trailing layer distance in pips.
/// </summary>
public decimal TrailingStep2Pips
{
	get => _trailingStep2Pips.Value;
	set => _trailingStep2Pips.Value = value;
}

/// <summary>
/// Third trailing layer distance in pips.
/// </summary>
public decimal TrailingStep3Pips
{
	get => _trailingStep3Pips.Value;
	set => _trailingStep3Pips.Value = value;
}

/// <summary>
/// Volume closed at the first trailing layer.
/// </summary>
public decimal PartialClose1Volume
{
	get => _partialClose1Volume.Value;
	set => _partialClose1Volume.Value = value;
}

/// <summary>
/// Volume closed at the second trailing layer.
/// </summary>
public decimal PartialClose2Volume
{
	get => _partialClose2Volume.Value;
	set => _partialClose2Volume.Value = value;
}

/// <summary>
/// Number of bars for which EMA/RSI crosses stay valid.
/// </summary>
public int CrossEffectiveBars
{
	get => _crossEffectiveBars.Value;
	set => _crossEffectiveBars.Value = value;
}

/// <summary>
/// Neutral band around the Vegas tunnel.
/// </summary>
public decimal TunnelBandWidthPips
{
	get => _tunnelBandWidthPips.Value;
	set => _tunnelBandWidthPips.Value = value;
}

/// <summary>
/// Maximum allowed distance above the tunnel for long entries.
/// </summary>
public decimal TunnelSafeZonePips
{
	get => _tunnelSafeZonePips.Value;
	set => _tunnelSafeZonePips.Value = value;
}

/// <summary>
/// Enables trading during the London session.
/// </summary>
public bool EnableLondonSession
{
	get => _enableLondonSession.Value;
	set => _enableLondonSession.Value = value;
}

/// <summary>
/// Enables trading during the New York session.
/// </summary>
public bool EnableNewYorkSession
{
	get => _enableNewYorkSession.Value;
	set => _enableNewYorkSession.Value = value;
}

/// <summary>
/// Enables trading during the Tokyo session.
/// </summary>
public bool EnableTokyoSession
{
	get => _enableTokyoSession.Value;
	set => _enableTokyoSession.Value = value;
}

/// <summary>
/// Fast EMA period.
/// </summary>
public int FastPeriod
{
	get => _fastPeriod.Value;
	set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow EMA period.
/// </summary>
public int SlowPeriod
{
	get => _slowPeriod.Value;
	set => _slowPeriod.Value = value;
}

/// <summary>
/// Horizontal shift applied to the moving averages.
/// </summary>
public int MaShift
{
	get => _maShift.Value;
	set => _maShift.Value = value;
}

/// <summary>
/// Moving average calculation method.
/// </summary>
public MovingAverageType MaMethod
{
	get => _maMethod.Value;
	set => _maMethod.Value = value;
}

/// <summary>
/// Applied price for the moving averages.
/// </summary>
public AppliedPriceType MaAppliedPrice
{
	get => _maAppliedPrice.Value;
	set => _maAppliedPrice.Value = value;
}

/// <summary>
/// RSI averaging period.
/// </summary>
public int RsiPeriod
{
	get => _rsiPeriod.Value;
	set => _rsiPeriod.Value = value;
}

/// <summary>
/// Applied price for the RSI indicator.
/// </summary>
public AppliedPriceType RsiAppliedPrice
{
	get => _rsiAppliedPrice.Value;
	set => _rsiAppliedPrice.Value = value;
}

/// <summary>
/// Candle data type used for calculations.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}
	/// <summary>
	/// Maximum number of cached history items used by the strategy.
	/// </summary>
	public int HistoryLimit
	{
		get => _historyLimit.Value;
		set => _historyLimit.Value = value;
	}
	/// <summary>
	/// RSI neutral level separating bullish and bearish regimes.
	/// </summary>
	public decimal FiftyLevel
	{
		get => _fiftyLevel.Value;
		set => _fiftyLevel.Value = value;
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

	_fastHistory.Clear();
	_slowHistory.Clear();
	_vegasFastHistory.Clear();
	_vegasSlowHistory.Clear();
	_rsiHistory.Clear();
	_candleHistory.Clear();

	_emaCrossedUp = false;
	_emaCrossedDown = false;
	_rsiCrossedUp = false;
	_rsiCrossedDown = false;
	_tunnelCrossedUp = false;
	_tunnelCrossedDown = false;
	_emaCrossUpTimer = 0;
	_emaCrossDownTimer = 0;
	_rsiCrossUpTimer = 0;
	_rsiCrossDownTimer = 0;

	_longEntryPrice = 0m;
	_shortEntryPrice = 0m;
	_longPartial1Done = false;
	_longPartial2Done = false;
	_shortPartial1Done = false;
	_shortPartial2Done = false;

	CancelLongStop();
	CancelShortStop();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	InitializeIndicators();
	InitializePipSettings();

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();

	StartProtection();
}

private void InitializeIndicators()
{
	_fastMa = CreateMovingAverage(MaMethod, FastPeriod);
	_slowMa = CreateMovingAverage(MaMethod, SlowPeriod);
	_vegasFastMa = CreateMovingAverage(MaMethod, 144);
	_vegasSlowMa = CreateMovingAverage(MaMethod, 169);
	_rsi = new RelativeStrengthIndex
	{
		Length = RsiPeriod
	};
}

private void InitializePipSettings()
{
	if (Security == null)
	{
		_pipSize = 0m;
		return;
	}

_pipSize = Security.PriceStep ?? 0m;
if (_pipSize <= 0m)
_pipSize = 1m;

var decimals = Security.Decimals;
if (decimals == 3 || decimals == 5)
_pipSize *= 10m;

_stopLossDistance = StopLossPips * _pipSize;
_stopLossToFiboDistance = StopLossToFiboPips * _pipSize;
_trailingStopDistance = TrailingStopPips * _pipSize;
_trailingStep1Distance = TrailingStep1Pips * _pipSize;
_trailingStep2Distance = TrailingStep2Pips * _pipSize;
_trailingStep3Distance = TrailingStep3Pips * _pipSize;
_tunnelBandWidthDistance = TunnelBandWidthPips * _pipSize;
_tunnelSafeZoneDistance = TunnelSafeZonePips * _pipSize;
}

private void ProcessCandle(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
	return;

	InitializePipSettings();

	var maInput = GetAppliedPrice(candle, MaAppliedPrice);
	var rsiInput = GetAppliedPrice(candle, RsiAppliedPrice);
	var time = candle.OpenTime;

	var fastValue = _fastMa.Process(maInput, time, true).ToDecimal();
	var slowValue = _slowMa.Process(maInput, time, true).ToDecimal();
	var vegasFastValue = _vegasFastMa.Process(maInput, time, true).ToDecimal();
	var vegasSlowValue = _vegasSlowMa.Process(maInput, time, true).ToDecimal();
	var rsiValue = _rsi.Process(rsiInput, time, true).ToDecimal();

	AddToHistory(_fastHistory, fastValue);
	AddToHistory(_slowHistory, slowValue);
	AddToHistory(_vegasFastHistory, vegasFastValue);
	AddToHistory(_vegasSlowHistory, vegasSlowValue);
	AddToHistory(_rsiHistory, rsiValue);
	AddCandleToHistory(new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));

	if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_vegasFastMa.IsFormed || !_vegasSlowMa.IsFormed || !_rsi.IsFormed)
	return;

	UpdateCrossStates();

	var closeTime = candle.CloseTime;
	if (!IsTradingSession(closeTime) || !IsFormedAndOnlineAndAllowTrading())
	{
		ManagePositions(candle);
		return;
	}

if (TryEnterLong(candle))
return;

if (TryEnterShort(candle))
return;

ManagePositions(candle);
}

private void ManagePositions(ICandleMessage candle)
{
	if (Position > 0m)
	{
		ManageLongPosition(candle);
	}
else if (Position < 0m)
{
	ManageShortPosition(candle);
}
else
{
	ResetLongState();
	ResetShortState();
}
}

private void ManageLongPosition(ICandleMessage candle)
{
	if (_emaCrossedDown || _rsiCrossedDown)
	{
		ClosePosition();
		ResetLongState();
		return;
	}

var vegasSlow = GetMaValue(_vegasSlowHistory, 0);
if (!vegasSlow.HasValue)
return;

var close = candle.ClosePrice;
var basePrice = vegasSlow.Value;

if (_tunnelCrossedUp)
{
	if (_trailingStep3Distance > 0m && close >= basePrice + _trailingStep3Distance)
	{
		UpdateLongStop(close - _trailingStopDistance);
		return;
	}

if (_trailingStep2Distance > 0m && close >= basePrice + _trailingStep2Distance)
{
	TryCloseLongPartial(PartialClose2Volume, basePrice + _trailingStep2Distance, ref _longPartial2Done);
	UpdateLongStop(close - _trailingStopDistance);
	return;
}

if (_trailingStep1Distance > 0m && close >= basePrice + _trailingStep1Distance)
{
	TryCloseLongPartial(PartialClose1Volume, basePrice + _trailingStep1Distance, ref _longPartial1Done);
	UpdateLongStop(close - _trailingStopDistance);
	return;
}

if (_trailingStopDistance > 0m)
UpdateLongStop(close - _trailingStopDistance);
}
else
{
	if (_trailingStep1Distance > 0m && close >= basePrice - _trailingStep1Distance)
	{
		var newStop = basePrice - (_trailingStep1Distance + _stopLossToFiboDistance);
		UpdateLongStop(newStop);
		return;
	}

if (_trailingStep2Distance > 0m && close >= basePrice - _trailingStep2Distance)
{
	var newStop = basePrice - (_trailingStep2Distance + _stopLossToFiboDistance);
	UpdateLongStop(newStop);
	return;
}

if (_trailingStep3Distance > 0m && close >= basePrice - _trailingStep3Distance)
{
	var newStop = basePrice - (_trailingStep3Distance + _stopLossToFiboDistance);
	UpdateLongStop(newStop);
}
}
}

private void ManageShortPosition(ICandleMessage candle)
{
	if (_emaCrossedUp || _rsiCrossedUp)
	{
		ClosePosition();
		ResetShortState();
		return;
	}

var vegasSlow = GetMaValue(_vegasSlowHistory, 0);
if (!vegasSlow.HasValue)
return;

var close = candle.ClosePrice;
var basePrice = vegasSlow.Value;

if (_tunnelCrossedDown)
{
	if (_trailingStep3Distance > 0m && close <= basePrice - _trailingStep3Distance)
	{
		UpdateShortStop(close + _trailingStopDistance);
		return;
	}

if (_trailingStep2Distance > 0m && close <= basePrice - _trailingStep2Distance)
{
	TryCloseShortPartial(PartialClose2Volume, basePrice - _trailingStep2Distance, ref _shortPartial2Done);
	UpdateShortStop(close + _trailingStopDistance);
	return;
}

if (_trailingStep1Distance > 0m && close <= basePrice - _trailingStep1Distance)
{
	TryCloseShortPartial(PartialClose1Volume, basePrice - _trailingStep1Distance, ref _shortPartial1Done);
	UpdateShortStop(close + _trailingStopDistance);
	return;
}

if (_trailingStopDistance > 0m)
UpdateShortStop(close + _trailingStopDistance);
}
else
{
	if (_trailingStep1Distance > 0m && close <= basePrice + _trailingStep1Distance)
	{
		var newStop = basePrice + (_trailingStep1Distance + _stopLossToFiboDistance);
		UpdateShortStop(newStop);
		return;
	}

if (_trailingStep2Distance > 0m && close <= basePrice + _trailingStep2Distance)
{
	var newStop = basePrice + (_trailingStep2Distance + _stopLossToFiboDistance);
	UpdateShortStop(newStop);
	return;
}

if (_trailingStep3Distance > 0m && close <= basePrice + _trailingStep3Distance)
{
	var newStop = basePrice + (_trailingStep3Distance + _stopLossToFiboDistance);
	UpdateShortStop(newStop);
}
}
}

private void TryCloseLongPartial(decimal volume, decimal referenceLevel, ref bool flag)
{
	if (flag || volume <= 0m)
	return;

	if (Math.Abs(Position) <= volume)
	return;

	if (_longEntryPrice >= referenceLevel)
	return;

	SellMarket(volume);
	flag = true;
}

private void TryCloseShortPartial(decimal volume, decimal referenceLevel, ref bool flag)
{
	if (flag || volume <= 0m)
	return;

	if (Math.Abs(Position) <= volume)
	return;

	if (_shortEntryPrice <= referenceLevel)
	return;

	BuyMarket(volume);
	flag = true;
}

private void UpdateLongStop(decimal newStopPrice)
{
	if (newStopPrice <= 0m || Math.Abs(Position) <= 0m)
	return;

	if (_longStopOrder != null)
	{
		var minMove = GetMinimalStopMove();
		if (newStopPrice <= _longStopPrice + minMove)
		return;

		CancelLongStop();
	}

_longStopOrder = SellStop(Math.Abs(Position), newStopPrice);
_longStopPrice = newStopPrice;
}

private void UpdateShortStop(decimal newStopPrice)
{
	if (newStopPrice <= 0m || Math.Abs(Position) <= 0m)
	return;

	if (_shortStopOrder != null)
	{
		var minMove = GetMinimalStopMove();
		if (newStopPrice >= _shortStopPrice - minMove)
		return;

		CancelShortStop();
	}

_shortStopOrder = BuyStop(Math.Abs(Position), newStopPrice);
_shortStopPrice = newStopPrice;
}

private void CancelLongStop()
{
	if (_longStopOrder != null)
	{
		CancelOrder(_longStopOrder);
		_longStopOrder = null;
		_longStopPrice = 0m;
	}
}

private void CancelShortStop()
{
	if (_shortStopOrder != null)
	{
		CancelOrder(_shortStopOrder);
		_shortStopOrder = null;
		_shortStopPrice = 0m;
	}
}

private void ResetLongState()
{
	_longPartial1Done = false;
	_longPartial2Done = false;
	_longEntryPrice = 0m;
	CancelLongStop();
}

private void ResetShortState()
{
	_shortPartial1Done = false;
	_shortPartial2Done = false;
	_shortEntryPrice = 0m;
	CancelShortStop();
}

private bool TryEnterLong(ICandleMessage candle)
{
	if (!_emaCrossedUp || !_rsiCrossedUp)
	return false;

	var currentCandle = GetCandle(0);
	var previousCandle = GetCandle(1);
	var vegasSlow = GetMaValue(_vegasSlowHistory, 0);

	if (currentCandle == null || previousCandle == null || !vegasSlow.HasValue)
	return false;

	var basePrice = vegasSlow.Value;
	var bullishBreak = currentCandle.Close >= basePrice + _tunnelBandWidthDistance &&
	currentCandle.Close <= basePrice + _tunnelSafeZoneDistance &&
	currentCandle.Open < currentCandle.Close;

	var pullbackBreak = currentCandle.Close <= basePrice - _tunnelBandWidthDistance;

	if (!bullishBreak && !pullbackBreak)
	return false;

	if (Position < 0m)
	{
		ClosePosition();
		ResetShortState();
		return true;
	}

if (Position > 0m || TradeVolume <= 0m)
return false;

BuyMarket(TradeVolume);
_longEntryPrice = candle.ClosePrice;
ResetShortState();

if (_stopLossDistance > 0m)
UpdateLongStop(candle.ClosePrice - _stopLossDistance);

return true;
}

private bool TryEnterShort(ICandleMessage candle)
{
	if (!_emaCrossedDown || !_rsiCrossedDown)
	return false;

	var currentCandle = GetCandle(0);
	var previousCandle = GetCandle(1);
	var vegasSlow = GetMaValue(_vegasSlowHistory, 0);

	if (currentCandle == null || previousCandle == null || !vegasSlow.HasValue)
	return false;

	var basePrice = vegasSlow.Value;
	var bearishBreak = currentCandle.Close <= basePrice - _tunnelBandWidthDistance &&
	currentCandle.Close >= basePrice - _tunnelSafeZoneDistance &&
	currentCandle.Open > currentCandle.Close;

	var pushDownBreak = currentCandle.Close >= basePrice + _tunnelBandWidthDistance;

	if (!bearishBreak && !pushDownBreak)
	return false;

	if (Position > 0m)
	{
		ClosePosition();
		ResetLongState();
		return true;
	}

if (Position < 0m || TradeVolume <= 0m)
return false;

SellMarket(TradeVolume);
_shortEntryPrice = candle.ClosePrice;
ResetLongState();

if (_stopLossDistance > 0m)
UpdateShortStop(candle.ClosePrice + _stopLossDistance);

return true;
}

private void UpdateCrossStates()
{
	var fastCurrent = GetMaValue(_fastHistory, 0);
	var fastPrev = GetMaValue(_fastHistory, 1);
	var slowCurrent = GetMaValue(_slowHistory, 0);
	var slowPrev = GetMaValue(_slowHistory, 1);

	if (fastCurrent.HasValue && fastPrev.HasValue && slowCurrent.HasValue && slowPrev.HasValue)
	{
		if (fastCurrent.Value > slowCurrent.Value && fastPrev.Value < slowPrev.Value)
		{
			_emaCrossedUp = true;
			_emaCrossedDown = false;
		}
	else if (fastCurrent.Value < slowCurrent.Value && fastPrev.Value > slowPrev.Value)
	{
		_emaCrossedUp = false;
		_emaCrossedDown = true;
	}
}

if (_emaCrossedUp)
{
	_emaCrossUpTimer++;
	if (_emaCrossUpTimer >= CrossEffectiveBars)
	{
		_emaCrossedUp = false;
		_emaCrossUpTimer = 0;
	}
}
else
{
	_emaCrossUpTimer = 0;
}

if (_emaCrossedDown)
{
	_emaCrossDownTimer++;
	if (_emaCrossDownTimer >= CrossEffectiveBars)
	{
		_emaCrossedDown = false;
		_emaCrossDownTimer = 0;
	}
}
else
{
	_emaCrossDownTimer = 0;
}

var rsiCurrent = GetSeriesValue(_rsiHistory, 0);
var rsiPrev = GetSeriesValue(_rsiHistory, 1);

if (rsiCurrent.HasValue && rsiPrev.HasValue)
{
	if (rsiCurrent.Value > FiftyLevel && rsiPrev.Value < FiftyLevel)
	{
		_rsiCrossedUp = true;
		_rsiCrossedDown = false;
	}
else if (rsiCurrent.Value < FiftyLevel && rsiPrev.Value > FiftyLevel)
{
	_rsiCrossedUp = false;
	_rsiCrossedDown = true;
}
}

if (_rsiCrossedUp)
{
	_rsiCrossUpTimer++;
	if (_rsiCrossUpTimer >= CrossEffectiveBars)
	{
		_rsiCrossedUp = false;
		_rsiCrossUpTimer = 0;
	}
}
else
{
	_rsiCrossUpTimer = 0;
}

if (_rsiCrossedDown)
{
	_rsiCrossDownTimer++;
	if (_rsiCrossDownTimer >= CrossEffectiveBars)
	{
		_rsiCrossedDown = false;
		_rsiCrossDownTimer = 0;
	}
}
else
{
	_rsiCrossDownTimer = 0;
}

var currentCandle = GetCandle(0);
var previousCandle = GetCandle(1);
var vegasFast = GetMaValue(_vegasFastHistory, 0);
var vegasSlow = GetMaValue(_vegasSlowHistory, 0);

if (currentCandle != null && previousCandle != null && vegasFast.HasValue && vegasSlow.HasValue)
{
	var aboveTunnel = currentCandle.Close > vegasFast.Value && currentCandle.Close > vegasSlow.Value;
	var belowTunnel = currentCandle.Close < vegasFast.Value && currentCandle.Close < vegasSlow.Value;
	var prevAbove = previousCandle.Close > vegasFast.Value || previousCandle.Close > vegasSlow.Value;
	var prevBelow = previousCandle.Close < vegasFast.Value || previousCandle.Close < vegasSlow.Value;

	if (aboveTunnel && prevBelow)
	{
		_tunnelCrossedUp = true;
		_tunnelCrossedDown = false;
	}
else if (belowTunnel && prevAbove)
{
	_tunnelCrossedUp = false;
	_tunnelCrossedDown = true;
}
}
}

private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType type)
{
	return type switch
	{
		AppliedPriceType.Open => candle.OpenPrice,
		AppliedPriceType.High => candle.HighPrice,
		AppliedPriceType.Low => candle.LowPrice,
		AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
		AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
		AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
		_ => candle.ClosePrice,
	};
}

private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageType type, int length)
{
	return type switch
	{
		MovingAverageType.Simple => new SimpleMovingAverage { Length = length },
		MovingAverageType.Smoothed => new SmoothedMovingAverage { Length = length },
		MovingAverageType.LinearWeighted => new WeightedMovingAverage { Length = length },
		_ => new ExponentialMovingAverage { Length = length },
	};
}

private void AddToHistory(List<decimal> history, decimal value)
{
	history.Add(value);
	if (history.Count > HistoryLimit)
	history.RemoveAt(0);
}

private void AddCandleToHistory(CandleSnapshot snapshot)
{
	_candleHistory.Add(snapshot);
	if (_candleHistory.Count > HistoryLimit)
	_candleHistory.RemoveAt(0);
}

private decimal? GetSeriesValue(List<decimal> values, int offset)
{
	var index = values.Count - 1 - offset;
	if (index < 0)
	return null;

	return values[index];
}

private decimal? GetMaValue(List<decimal> values, int offset)
{
	var shift = Math.Max(0, MaShift);
	var index = values.Count - 1 - shift - offset;
	if (index < 0)
	return null;

	return values[index];
}

private CandleSnapshot? GetCandle(int offset)
{
	var index = _candleHistory.Count - 1 - offset;
	if (index < 0)
	return null;

	return _candleHistory[index];
}

private bool IsTradingSession(DateTimeOffset time)
{
	var hour = time.Hour;

	var london = EnableLondonSession && hour >= 7 && hour <= 16;
	var newYork = EnableNewYorkSession && hour >= 12 && hour <= 21;
	var tokyo = EnableTokyoSession && hour >= 0 && hour <= 8;
	var lateHour = hour >= 23;

	return london || newYork || tokyo || lateHour;
}

private decimal GetMinimalStopMove()
{
	var step = Security?.PriceStep ?? 0m;
	if (step > 0m)
	return step;

	return _pipSize > 0m ? _pipSize / 10m : 0.00001m;
}

private readonly struct CandleSnapshot
{
	public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
	{
		Open = open;
		High = high;
		Low = low;
		Close = close;
	}

public decimal Open { get; }
public decimal High { get; }
public decimal Low { get; }
public decimal Close { get; }
}
}

/// <summary>
/// Moving average calculation options supported by the strategy.
/// </summary>
public enum MovingAverageType
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
	/// Smoothed moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	LinearWeighted
}

/// <summary>
/// Price extraction modes replicated from MetaTrader.
/// </summary>
public enum AppliedPriceType
{
	/// <summary>
	/// Closing price.
	/// </summary>
	Close,

	/// <summary>
	/// Opening price.
	/// </summary>
	Open,

	/// <summary>
	/// Highest price.
	/// </summary>
	High,

	/// <summary>
	/// Lowest price.
	/// </summary>
	Low,

	/// <summary>
	/// Median price (high + low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// Typical price (high + low + close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted price (high + low + 2 * close) / 4.
	/// </summary>
	Weighted
}

