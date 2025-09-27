using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "JK synchro".
/// Counts the number of bearish and bullish candles inside a rolling window
/// and opens trades when one side dominates while respecting time and pause filters.
/// Includes optional stop-loss, take-profit and trailing stop controls expressed in pips.
/// </summary>
public class JkSynchroStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _analysisPeriod;
	private readonly StrategyParam<int> _pauseBetweenTradesSeconds;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<int> _directionWindow = new();

	private int _bearishCount;
	private int _bullishCount;

	private decimal _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	private DateTimeOffset? _lastEntryTime;

	/// <summary>
	/// Default trade volume used for each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Minimum absolute position size treated as non-zero.
	/// </summary>
	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <summary>
	/// Maximum number of lots (per direction) allowed at the same time.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Number of finished candles evaluated for dominance.
	/// </summary>
	public int AnalysisPeriod
	{
		get => _analysisPeriod.Value;
		set => _analysisPeriod.Value = value;
	}

	/// <summary>
	/// Pause in seconds between consecutive entries.
	/// </summary>
	public int PauseBetweenTradesSeconds
	{
		get => _pauseBetweenTradesSeconds.Value;
		set => _pauseBetweenTradesSeconds.Value = value;
	}

	/// <summary>
	/// Trading window start hour (inclusive).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window end hour (inclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips. Use zero to disable.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips. Use zero to disable.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips. Use zero to disable trailing.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Extra distance in pips required before adjusting the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle data type used for the calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public JkSynchroStrategy()
	{
	_volumeTolerance = Param(nameof(VolumeTolerance), 1e-8m)
	.SetGreaterThanOrEqualZero()
	.SetDisplay("Volume Tolerance", "Threshold below which positions are considered flat", "Risk");

	_orderVolume = Param(nameof(OrderVolume), 0.1m)
	.SetGreaterThanZero()
	.SetDisplay("Order Volume", "Volume placed with every market order", "Trading")
	.SetCanOptimize(true)
	.SetOptimize(0.1m, 2m, 0.1m);

	_maxPositions = Param(nameof(MaxPositions), 10)
	.SetGreaterThanZero()
	.SetDisplay("Max Positions", "Maximum number of lots allowed per direction", "Risk");

	_analysisPeriod = Param(nameof(AnalysisPeriod), 18)
	.SetGreaterThanZero()
	.SetDisplay("Analysis Period", "Number of finished candles used in the vote", "Signals");

	_pauseBetweenTradesSeconds = Param(nameof(PauseBetweenTradesSeconds), 540)
	.SetDisplay("Pause Between Trades (sec)", "Cooldown after an entry in seconds", "Risk");

	_startHour = Param(nameof(StartHour), 3)
	.SetDisplay("Start Hour", "Hour of day when trading becomes active", "Time Filter");

	_endHour = Param(nameof(EndHour), 6)
	.SetDisplay("End Hour", "Hour of day when trading stops", "Time Filter");

	_stopLossPips = Param(nameof(StopLossPips), 50)
	.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

	_takeProfitPips = Param(nameof(TakeProfitPips), 150)
	.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

	_trailingStopPips = Param(nameof(TrailingStopPips), 15)
	.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

	_trailingStepPips = Param(nameof(TrailingStepPips), 5)
	.SetDisplay("Trailing Step (pips)", "Extra move required before trailing updates", "Risk");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("Candle Type", "Candle source used for the calculations", "General");
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

	_directionWindow.Clear();
	_bearishCount = 0;
	_bullishCount = 0;
	_pipSize = 0m;
	_longEntryPrice = null;
	_shortEntryPrice = null;
	_longStopPrice = null;
	_shortStopPrice = null;
	_longTakeProfitPrice = null;
	_shortTakeProfitPrice = null;
	_lastEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	if (StartHour < 0 || StartHour > 24)
	throw new InvalidOperationException("Start hour must be within 0-24 range.");

	if (EndHour < 0 || EndHour > 24)
	throw new InvalidOperationException("End hour must be within 0-24 range.");

	if (StartHour >= EndHour)
	throw new InvalidOperationException("Start hour must be strictly less than end hour.");

	if (TrailingStopPips > 0 && TrailingStepPips <= 0)
	throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

	Volume = OrderVolume;

	_pipSize = CalculatePipSize();

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawOwnTrades(area);
	}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
	base.OnPositionChanged(delta);

	var previousPosition = Position - delta;

	if (Math.Abs(Position) > Math.Abs(previousPosition) + VolumeTolerance)
	_lastEntryTime = CurrentTime;

	if (Math.Abs(Position) <= VolumeTolerance)
	{
	ResetLongState();
	ResetShortState();
	_lastEntryTime = null;
	return;
	}

	if (Position > VolumeTolerance)
	{
	InitializeLongState();
	ResetShortState();
	return;
	}

	if (Position < -VolumeTolerance)
	{
	InitializeShortState();
	ResetLongState();
	}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var hasFullWindow = UpdateDirectionWindow(candle);

	UpdateTrailingStops(candle);

	if (TryHandleExits(candle))
	return;

	if (!hasFullWindow)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var time = GetCandleTime(candle);

	if (!IsWithinTradingHours(time))
	return;

	if (!IsPauseElapsed(time))
	return;

	if (_bearishCount > _bullishCount)
	{
	TryEnterLong();
	}
	else if (_bullishCount > _bearishCount)
	{
	TryEnterShort();
	}
	}

	private bool UpdateDirectionWindow(ICandleMessage candle)
	{
	var direction = 0;

	if (candle.OpenPrice > candle.ClosePrice)
	direction = 1;
	else if (candle.OpenPrice < candle.ClosePrice)
	direction = -1;

	_directionWindow.Enqueue(direction);

	if (direction > 0)
	_bearishCount++;
	else if (direction < 0)
	_bullishCount++;

	while (_directionWindow.Count > AnalysisPeriod)
	{
	var removed = _directionWindow.Dequeue();
	if (removed > 0)
	_bearishCount--;
	else if (removed < 0)
	_bullishCount--;
	}

	return _directionWindow.Count >= AnalysisPeriod;
	}

	private void TryEnterLong()
	{
	if (Position < -VolumeTolerance)
	{
	BuyMarket(Math.Abs(Position));
	return;
	}

	if (!CanIncreaseExposure(true))
	return;

	BuyMarket(OrderVolume);
	}

	private void TryEnterShort()
	{
	if (Position > VolumeTolerance)
	{
	SellMarket(Position);
	return;
	}

	if (!CanIncreaseExposure(false))
	return;

	SellMarket(OrderVolume);
	}

	private bool CanIncreaseExposure(bool isLong)
	{
	var targetPosition = Position + (isLong ? OrderVolume : -OrderVolume);
	var maxExposure = MaxPositions * OrderVolume;

	return Math.Abs(targetPosition) <= maxExposure + VolumeTolerance;
	}

	private bool TryHandleExits(ICandleMessage candle)
	{
	if (Position > VolumeTolerance)
	{
	var volume = Position;

	if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
	{
	SellMarket(volume);
	return true;
	}

	if (_longTakeProfitPrice is decimal take && candle.HighPrice >= take)
	{
	SellMarket(volume);
	return true;
	}
	}
	else if (Position < -VolumeTolerance)
	{
	var volume = Math.Abs(Position);

	if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
	{
	BuyMarket(volume);
	return true;
	}

	if (_shortTakeProfitPrice is decimal take && candle.LowPrice <= take)
	{
	BuyMarket(volume);
	return true;
	}
	}

	return false;
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
	if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
	return;

	var trailingDistance = GetPriceOffset(TrailingStopPips);
	var trailingStep = GetPriceOffset(TrailingStepPips);

	if (trailingDistance <= 0m)
	return;

	if (Position > VolumeTolerance && _longEntryPrice is decimal entry)
	{
	var profit = candle.ClosePrice - entry;
	if (profit <= trailingDistance + trailingStep)
	return;

	var newStop = candle.ClosePrice - trailingDistance;

	if (_longStopPrice is decimal current)
	{
	if (newStop - current >= trailingStep)
	_longStopPrice = Math.Max(current, newStop);
	}
	else
	{
	_longStopPrice = newStop;
	}
	}
	else if (Position < -VolumeTolerance && _shortEntryPrice is decimal entryShort)
	{
	var profit = entryShort - candle.ClosePrice;
	if (profit <= trailingDistance + trailingStep)
	return;

	var newStop = candle.ClosePrice + trailingDistance;

	if (_shortStopPrice is decimal current)
	{
	if (current - newStop >= trailingStep)
	_shortStopPrice = Math.Min(current, newStop);
	}
	else
	{
	_shortStopPrice = newStop;
	}
	}
	}

	private void InitializeLongState()
	{
	if (PositionPrice is not decimal entry || entry <= 0m)
	return;

	_longEntryPrice = entry;
	_longStopPrice = StopLossPips > 0 ? entry - GetPriceOffset(StopLossPips) : null;
	_longTakeProfitPrice = TakeProfitPips > 0 ? entry + GetPriceOffset(TakeProfitPips) : null;
	}

	private void InitializeShortState()
	{
	if (PositionPrice is not decimal entry || entry <= 0m)
	return;

	_shortEntryPrice = entry;
	_shortStopPrice = StopLossPips > 0 ? entry + GetPriceOffset(StopLossPips) : null;
	_shortTakeProfitPrice = TakeProfitPips > 0 ? entry - GetPriceOffset(TakeProfitPips) : null;
	}

	private void ResetLongState()
	{
	_longEntryPrice = null;
	_longStopPrice = null;
	_longTakeProfitPrice = null;
	}

	private void ResetShortState()
	{
	_shortEntryPrice = null;
	_shortStopPrice = null;
	_shortTakeProfitPrice = null;
	}

	private decimal CalculatePipSize()
	{
	var step = Security?.PriceStep ?? 0.0001m;
	var decimals = Security?.Decimals ?? 4;

	var multiplier = decimals is 3 or 5 ? 10m : 1m;

	return step * multiplier;
	}

	private decimal GetPriceOffset(int pips)
	{
	if (pips <= 0)
	return 0m;

	return pips * _pipSize;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
	var hour = time.Hour;
	return hour >= StartHour && hour <= EndHour;
	}

	private bool IsPauseElapsed(DateTimeOffset time)
	{
	var pauseSeconds = PauseBetweenTradesSeconds;

	if (pauseSeconds <= 0)
	return true;

	if (_lastEntryTime is null)
	return true;

	return time - _lastEntryTime.Value >= TimeSpan.FromSeconds(pauseSeconds);
	}

	private static DateTimeOffset GetCandleTime(ICandleMessage candle)
	{
	return candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
	}
}
