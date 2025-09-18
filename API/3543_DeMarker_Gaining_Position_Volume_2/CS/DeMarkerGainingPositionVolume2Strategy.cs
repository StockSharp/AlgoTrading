namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from "DeMarker gaining position volume 2" expert advisor.
/// Uses DeMarker oscillator thresholds, optional time filter and protective orders.
/// </summary>
public class DeMarkerGainingPositionVolume2Strategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private DeMarker _deMarker = null!;
	private DateTimeOffset? _lastLongSignalTime;
	private DateTimeOffset? _lastShortSignalTime;
	private decimal _priceStep;

	/// <summary>
	/// DeMarker averaging period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Upper DeMarker threshold.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// Lower DeMarker threshold.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step in price steps.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enable trading session filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Session start time (inclusive).
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time (exclusive).
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Trading volume per entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type to analyse.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DeMarkerGainingPositionVolume2Strategy"/> class.
	/// </summary>
	public DeMarkerGainingPositionVolume2Strategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "DeMarker averaging period", "Indicator");

		_upperLevel = Param(nameof(UpperLevel), 0.7m)
		.SetRange(0.1m, 0.99m)
		.SetDisplay("Upper Level", "Sell when DeMarker exceeds this value", "Levels");

		_lowerLevel = Param(nameof(LowerLevel), 0.3m)
		.SetRange(0.01m, 0.9m)
		.SetDisplay("Lower Level", "Buy when DeMarker drops below this value", "Levels");

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse", "Invert long/short logic", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 150m)
		.SetRange(0m, 10000m)
		.SetDisplay("Stop Loss", "Initial stop-loss in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
		.SetRange(0m, 10000m)
		.SetDisplay("Take Profit", "Take-profit in price steps", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), false)
		.SetDisplay("Enable Trailing", "Activate trailing stop logic", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 250m)
		.SetRange(0m, 10000m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in steps", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 50m)
		.SetRange(0m, 10000m)
		.SetDisplay("Trailing Step", "Trailing adjustment step in steps", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
		.SetDisplay("Use Time Filter", "Restrict trading to a session", "Time");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(10, 0, 0))
		.SetDisplay("Session Start", "Trading session start time", "Time");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 0, 0))
		.SetDisplay("Session End", "Trading session end time", "Time");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume to trade per signal", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_lastLongSignalTime = null;
	_lastShortSignalTime = null;
	_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	// Use the instrument price step to convert point-based risk settings.
	_priceStep = Security?.PriceStep ?? 0m;
	if (_priceStep <= 0m)
	_priceStep = 1m;

	_deMarker = new DeMarker { Length = DeMarkerPeriod };

	// Apply the preferred fixed trade volume.
	Volume = TradeVolume;

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_deMarker, ProcessCandle)
	.Start();

	var takeProfit = TakeProfitPoints > 0m
	? new Unit(TakeProfitPoints * _priceStep, UnitTypes.Point)
	: new Unit();

	var baseStop = StopLossPoints > 0m
	? new Unit(StopLossPoints * _priceStep, UnitTypes.Point)
	: new Unit();

	var trailingStop = TrailingStopPoints > 0m
	? new Unit(TrailingStopPoints * _priceStep, UnitTypes.Point)
	: baseStop;

	var trailingStep = TrailingStepPoints > 0m
	? new Unit(TrailingStepPoints * _priceStep, UnitTypes.Point)
	: new Unit();

	// Delegate stop-loss, take-profit and trailing stop handling to the built-in engine.
	StartProtection(
	takeProfit: takeProfit,
	stopLoss: EnableTrailing ? trailingStop : baseStop,
	isStopTrailing: EnableTrailing,
	trailingStopStep: trailingStep,
	useMarketOrders: true);

	var priceArea = CreateChartArea();
	if (priceArea != null)
	{
	DrawCandles(priceArea, subscription);
	DrawIndicator(priceArea, _deMarker);
	DrawOwnTrades(priceArea);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
	// Process only finished candles to avoid double-counting intra-bar data.
	if (candle.State != CandleStates.Finished)
	return;

	// Respect the optional trading session filter.
	if (!IsWithinSession(candle.CloseTime))
	return;

	// Ignore signals during warm-up or when the environment disallows trading.
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (!_deMarker.IsFormed)
	return;

	// Determine the desired direction based on DeMarker thresholds.
	var shouldBuy = deMarkerValue <= LowerLevel;
	var shouldSell = deMarkerValue >= UpperLevel;

	if (ReverseSignals)
	{
	(shouldBuy, shouldSell) = (shouldSell, shouldBuy);
	}

	var tradeVolume = Volume > 0m ? Volume : TradeVolume;
	if (tradeVolume <= 0m)
	tradeVolume = 1m;

	if (shouldBuy)
	{
	// Close shorts before opening fresh longs to mimic the MT5 expert.
	if (Position < 0m)
	BuyMarket(Math.Abs(Position));

	// Allow only one long entry per candle while topping up existing exposure.
	if (Position <= 0m && _lastLongSignalTime != candle.CloseTime)
	{
	BuyMarket(tradeVolume + Math.Abs(Position));
	_lastLongSignalTime = candle.CloseTime;
	}
	}
	else if (shouldSell)
	{
	// Symmetrically exit longs before entering new shorts.
	if (Position > 0m)
	SellMarket(Position);

	if (Position >= 0m && _lastShortSignalTime != candle.CloseTime)
	{
	SellMarket(tradeVolume + Math.Abs(Position));
	_lastShortSignalTime = candle.CloseTime;
	}
	}
	}

	private bool IsWithinSession(DateTimeOffset candleTime)
	{
	if (!UseTimeFilter)
	return true;

	var timeOfDay = candleTime.TimeOfDay;
	var start = SessionStart;
	var end = SessionEnd;

	if (start == end)
	return true;

	return start <= end
	? timeOfDay >= start && timeOfDay < end
	: timeOfDay >= start || timeOfDay < end;
	}
}
