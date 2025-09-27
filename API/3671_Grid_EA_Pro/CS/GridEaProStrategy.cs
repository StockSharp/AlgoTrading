using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy that reproduces the behaviour of the Grid EA Pro expert advisor.
/// Combines RSI or timed breakout entries with martingale-style position scaling and virtual risk control.
/// </summary>
public class GridEaProStrategy : Strategy
{
	public enum GridTradeMode
	{
		Buy,
		Sell,
		Both,
	}

	public enum GridEntryMode
	{
		Rsi,
		FixedPoints,
		Manual,
	}

	private readonly StrategyParam<GridTradeMode> _mode;
	private readonly StrategyParam<GridEntryMode> _entryMode;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _distance;
	private readonly StrategyParam<int> _timerSeconds;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _fromBalance;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _maxLot;
	private readonly StrategyParam<int> _stepOrders;
	private readonly StrategyParam<decimal> _stepMultiplier;
	private readonly StrategyParam<int> _maxStep;
	private readonly StrategyParam<int> _overlapOrders;
	private readonly StrategyParam<int> _overlapPips;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _breakEvenStop;
	private readonly StrategyParam<int> _breakEvenStep;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingStep;
	private readonly StrategyParam<string> _startTime;
	private readonly StrategyParam<string> _endTime;

	private readonly List<decimal> _longVolumes = new();
	private readonly List<decimal> _shortVolumes = new();

	private RelativeStrengthIndex _rsi = null!;
	private decimal _tickSize;
	private decimal _stepValue;
	private decimal _tickValue;

	private decimal _lastLongPrice;
	private decimal _lastShortPrice;
	private decimal _lastLongVolume;
	private decimal _lastShortVolume;

	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;

	private bool _longBreakEven;
	private bool _shortBreakEven;

	private decimal _longTrailAnchor;
	private decimal _shortTrailAnchor;

	private decimal? _longNextLevel;
	private decimal? _shortNextLevel;
	private decimal? _pendingLong;
	private decimal? _pendingShort;
	private DateTimeOffset? _nextTimer;

	/// <summary>
	/// Trade direction filter.
	/// </summary>
	public GridTradeMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Entry mode selection.
	/// </summary>
	public GridEntryMode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// Candle type for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Distance for timed breakout entry (in points).
	/// </summary>
	public int Distance
	{
		get => _distance.Value;
		set => _distance.Value = value;
	}

	/// <summary>
	/// Interval for recalculating breakout levels.
	/// </summary>
	public int TimerSeconds
	{
		get => _timerSeconds.Value;
		set => _timerSeconds.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Reference balance for fixed-lot calculation.
	/// </summary>
	public decimal FromBalance
	{
		get => _fromBalance.Value;
		set => _fromBalance.Value = value;
	}

	/// <summary>
	/// Risk per trade percentage.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	/// <summary>
	/// Multiplier applied to each additional grid order.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum allowed order volume.
	/// </summary>
	public decimal MaxLot
	{
		get => _maxLot.Value;
		set => _maxLot.Value = value;
	}

	/// <summary>
	/// Grid step expressed in points.
	/// </summary>
	public int StepOrders
	{
		get => _stepOrders.Value;
		set => _stepOrders.Value = value;
	}

	/// <summary>
	/// Multiplier used to expand the grid step after each fill.
	/// </summary>
	public decimal StepMultiplier
	{
		get => _stepMultiplier.Value;
		set => _stepMultiplier.Value = value;
	}

	/// <summary>
	/// Cap for the adaptive grid step.
	/// </summary>
	public int MaxStep
	{
		get => _maxStep.Value;
		set => _maxStep.Value = value;
	}

	/// <summary>
	/// Number of opposite orders required to activate overlap logic.
	/// </summary>
	public int OverlapOrders
	{
		get => _overlapOrders.Value;
		set => _overlapOrders.Value = value;
	}

	/// <summary>
	/// Overlap exit offset in points.
	/// </summary>
	public int OverlapPips
	{
		get => _overlapPips.Value;
		set => _overlapPips.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Breakeven stop offset in points.
	/// </summary>
	public int BreakEvenStop
	{
		get => _breakEvenStop.Value;
		set => _breakEvenStop.Value = value;
	}

	/// <summary>
	/// Breakeven activation distance in points.
	/// </summary>
	public int BreakEvenStep
	{
		get => _breakEvenStep.Value;
		set => _breakEvenStep.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Trailing step distance in points.
	/// </summary>
	public int TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Trading window start time (HH:mm).
	/// </summary>
	public string StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading window end time (HH:mm).
	/// </summary>
	public string EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Initialize a new instance of <see cref="GridEaProStrategy"/>.
	/// </summary>
	public GridEaProStrategy()
	{
		_mode = Param(nameof(Mode), GridTradeMode.Both)
		.SetDisplay("Mode", "Allowed trade direction", "General");

		_entryMode = Param(nameof(EntryMode), GridEntryMode.Rsi)
		.SetDisplay("Entry Mode", "Signal source", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Lookback for RSI signal", "RSI")
		.SetCanOptimize(true);

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 70m)
		.SetDisplay("RSI Upper", "Overbought threshold", "RSI")
		.SetCanOptimize(true);

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 30m)
		.SetDisplay("RSI Lower", "Oversold threshold", "RSI")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signals", "General");

		_distance = Param(nameof(Distance), 50)
		.SetDisplay("Distance", "Breakout distance in points", "Entries")
		.SetCanOptimize(true);

		_timerSeconds = Param(nameof(TimerSeconds), 10)
		.SetDisplay("Timer Seconds", "Interval between breakout recalculations", "Entries");

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
		.SetDisplay("Initial Volume", "Base order volume", "Money Management");

		_fromBalance = Param(nameof(FromBalance), 1000m)
		.SetDisplay("Balance Divider", "Reference balance for lot calculation", "Money Management");

		_riskPerTrade = Param(nameof(RiskPerTrade), 0m)
		.SetDisplay("Risk %", "Risk per trade percentage", "Money Management")
		.SetCanOptimize(true);

		_lotMultiplier = Param(nameof(LotMultiplier), 1.1m)
		.SetDisplay("Lot Multiplier", "Multiplier applied to grid additions", "Money Management");

		_maxLot = Param(nameof(MaxLot), 999.9m)
		.SetDisplay("Max Lot", "Upper cap for volume", "Money Management");

		_stepOrders = Param(nameof(StepOrders), 100)
		.SetDisplay("Step", "Base grid distance in points", "Grid")
		.SetCanOptimize(true);

		_stepMultiplier = Param(nameof(StepMultiplier), 1.1m)
		.SetDisplay("Step Multiplier", "Adaptive grid expansion factor", "Grid");

		_maxStep = Param(nameof(MaxStep), 1000)
		.SetDisplay("Max Step", "Maximum grid step in points", "Grid");

		_overlapOrders = Param(nameof(OverlapOrders), 5)
		.SetDisplay("Overlap Orders", "Required orders for overlap exit", "Grid");

		_overlapPips = Param(nameof(OverlapPips), 10)
		.SetDisplay("Overlap Pips", "Offset used by overlap exit", "Grid");

		_stopLoss = Param(nameof(StopLoss), -1)
		.SetDisplay("Stop Loss", "Initial stop in points (-1 disables)", "Risk")
		.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 500)
		.SetDisplay("Take Profit", "Initial profit target in points", "Risk")
		.SetCanOptimize(true);

		_breakEvenStop = Param(nameof(BreakEvenStop), -1)
		.SetDisplay("Break Even Stop", "Offset once breakeven triggers (-1 disables)", "Risk");

		_breakEvenStep = Param(nameof(BreakEvenStep), 10)
		.SetDisplay("Break Even Step", "Distance to activate breakeven", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 50)
		.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 50)
		.SetDisplay("Trailing Step", "Minimum move before trailing update", "Risk");

		_startTime = Param(nameof(StartTime), "00:00")
		.SetDisplay("Start Time", "Trading window start (HH:mm)", "Sessions");

		_endTime = Param(nameof(EndTime), "00:00")
		.SetDisplay("End Time", "Trading window end (HH:mm)", "Sessions");
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

		_longVolumes.Clear();
		_shortVolumes.Clear();
		_rsi = null!;
		_tickSize = 0m;
		_stepValue = 0m;
		_tickValue = 0m;
		_lastLongPrice = 0m;
		_lastShortPrice = 0m;
		_lastLongVolume = 0m;
		_lastShortVolume = 0m;
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_longBreakEven = false;
		_shortBreakEven = false;
		_longTrailAnchor = 0m;
		_shortTrailAnchor = 0m;
		_longNextLevel = null;
		_shortNextLevel = null;
		_pendingLong = null;
		_pendingShort = null;
		_nextTimer = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 0.0001m;
		_stepValue = _tickSize;
		_tickValue = Security?.StepPrice ?? 1m;
		Volume = InitialVolume;

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinTradingHours(candle.CloseTime))
		return;

		ManageExits(candle);
		HandleEntries(candle, rsi);
		ProcessPendingBreakouts(candle);
		ProcessGridExpansions(candle);
	}

	private void HandleEntries(ICandleMessage candle, decimal rsi)
	{
		switch (EntryMode)
		{
			case GridEntryMode.Rsi:
				HandleRsiEntries(candle, rsi);
				break;
			case GridEntryMode.FixedPoints:
				ScheduleBreakoutLevels(candle);
				break;
			case GridEntryMode.Manual:
				break;
		}
	}

	private void HandleRsiEntries(ICandleMessage candle, decimal rsi)
	{
		if (AllowShortEntries() && _shortVolumes.Count == 0 && rsi >= RsiUpperLevel)
		EnterShort(candle.ClosePrice);

		if (AllowLongEntries() && _longVolumes.Count == 0 && rsi <= RsiLowerLevel)
		EnterLong(candle.ClosePrice);
	}

	private void ScheduleBreakoutLevels(ICandleMessage candle)
	{
		if (TimerSeconds <= 0)
		{
			PrepareBreakoutLevels(candle);
			return;
		}

		if (_nextTimer == null)
		_nextTimer = candle.CloseTime.AddSeconds(TimerSeconds);

		if (candle.CloseTime >= _nextTimer)
		{
			PrepareBreakoutLevels(candle);
			_nextTimer = candle.CloseTime.AddSeconds(TimerSeconds);
		}
	}

	private void PrepareBreakoutLevels(ICandleMessage candle)
	{
		var offset = Distance * _stepValue;

		if (AllowLongEntries() && _longVolumes.Count == 0)
		{
			_pendingLong = candle.ClosePrice + offset;
		}

		if (AllowShortEntries() && _shortVolumes.Count == 0)
		{
			_pendingShort = candle.ClosePrice - offset;
		}
	}

	private void ProcessPendingBreakouts(ICandleMessage candle)
	{
		if (_pendingLong.HasValue && AllowLongEntries())
		{
			if (candle.HighPrice >= _pendingLong.Value)
			{
				EnterLong(_pendingLong.Value);
				_pendingLong = null;
			}
		}

		if (_pendingShort.HasValue && AllowShortEntries())
		{
			if (candle.LowPrice <= _pendingShort.Value)
			{
				EnterShort(_pendingShort.Value);
				_pendingShort = null;
			}
		}
	}

	private void ProcessGridExpansions(ICandleMessage candle)
	{
		if (_longVolumes.Count > 0 && _longNextLevel.HasValue && AllowLongEntries())
		{
			if (candle.LowPrice <= _longNextLevel.Value)
			{
				EnterLong(_longNextLevel.Value);
				_longNextLevel = null;
			}
		}

		if (_shortVolumes.Count > 0 && _shortNextLevel.HasValue && AllowShortEntries())
		{
			if (candle.HighPrice >= _shortNextLevel.Value)
			{
				EnterShort(_shortNextLevel.Value);
				_shortNextLevel = null;
			}
		}
	}

	private void ManageExits(ICandleMessage candle)
	{
		if (_longVolumes.Count > 0 && Position > 0)
		{
			UpdateLongRisk(candle);

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				CloseLong();
				return;
			}

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				CloseLong();
				return;
			}
		}

		if (_shortVolumes.Count > 0 && Position < 0)
		{
			UpdateShortRisk(candle);

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				CloseShort();
				return;
			}

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				CloseShort();
			}
		}
	}

	private void EnterLong(decimal referencePrice)
	{
		var volume = CalculateNextLongVolume();
		if (volume <= 0m)
		return;

		BuyMarket(volume);

		_lastLongPrice = referencePrice;
		_lastLongVolume = volume;
	}

	private void EnterShort(decimal referencePrice)
	{
		var volume = CalculateNextShortVolume();
		if (volume <= 0m)
		return;

		SellMarket(volume);

		_lastShortPrice = referencePrice;
		_lastShortVolume = volume;
	}

	private decimal CalculateNextLongVolume()
	{
		var volume = CalculateBaseVolume();

		if (_longVolumes.Count > 0 && LotMultiplier > 1m)
		{
			var lastVolume = _longVolumes[_longVolumes.Count - 1];
			volume = Math.Min(lastVolume * LotMultiplier, MaxLot);
		}

		return Math.Min(volume, MaxLot);
	}

	private decimal CalculateNextShortVolume()
	{
		var volume = CalculateBaseVolume();

		if (_shortVolumes.Count > 0 && LotMultiplier > 1m)
		{
			var lastVolume = _shortVolumes[_shortVolumes.Count - 1];
			volume = Math.Min(lastVolume * LotMultiplier, MaxLot);
		}

		return Math.Min(volume, MaxLot);
	}

	private decimal CalculateBaseVolume()
	{
		var volume = InitialVolume;

		if (RiskPerTrade > 0m && StopLoss > 0 && _tickValue > 0m)
		{
			var balance = Portfolio?.CurrentValue ?? 0m;
			var riskAmount = balance * RiskPerTrade / 100m;
			var stopDistance = StopLoss * _stepValue;
			if (stopDistance > 0m)
			{
				var valuePerLot = (_tickValue * stopDistance) / _tickSize;
				if (valuePerLot > 0m)
				volume = Math.Max(volume, riskAmount / valuePerLot);
			}
		}
		else if (FromBalance > 0m)
		{
			var balance = Portfolio?.CurrentValue ?? 0m;
			if (balance > 0m)
			volume = Math.Max(volume, balance / FromBalance * InitialVolume);
		}

		return Math.Max(volume, 0m);
	}

	private void UpdateLongRisk(ICandleMessage candle)
	{
		var stopDistance = StopLoss > 0 ? StopLoss * _stepValue : 0m;
		var takeDistance = TakeProfit > 0 ? TakeProfit * _stepValue : 0m;

		if (!_longTake.HasValue && takeDistance > 0m)
		_longTake = _lastLongPrice + takeDistance;

		if (!_longStop.HasValue && stopDistance > 0m)
		_longStop = _lastLongPrice - stopDistance;

		var breakEvenDistance = BreakEvenStep > 0 ? BreakEvenStep * _stepValue : 0m;
		var breakEvenOffset = BreakEvenStop >= 0 ? BreakEvenStop * _stepValue : 0m;

		if (!_longBreakEven && breakEvenDistance > 0m)
		{
			if (candle.HighPrice - _lastLongPrice >= breakEvenDistance)
			{
				_longBreakEven = true;
				var breakEvenPrice = _lastLongPrice + breakEvenOffset;
				if (!_longStop.HasValue || _longStop.Value < breakEvenPrice)
				_longStop = breakEvenPrice;
			}
		}

		var trailingDistance = TrailingStop > 0 ? TrailingStop * _stepValue : 0m;
		var trailingStep = TrailingStep > 0 ? TrailingStep * _stepValue : trailingDistance;

		if (trailingDistance > 0m)
		{
			if (_longTrailAnchor == 0m)
			_longTrailAnchor = _lastLongPrice;

			if (candle.HighPrice - _longTrailAnchor >= trailingStep)
			{
				_longTrailAnchor = candle.HighPrice;
				var trailStop = candle.HighPrice - trailingDistance;
				if (!_longStop.HasValue || _longStop.Value < trailStop)
				_longStop = trailStop;
			}
		}
	}

	private void UpdateShortRisk(ICandleMessage candle)
	{
		var stopDistance = StopLoss > 0 ? StopLoss * _stepValue : 0m;
		var takeDistance = TakeProfit > 0 ? TakeProfit * _stepValue : 0m;

		if (!_shortTake.HasValue && takeDistance > 0m)
		_shortTake = _lastShortPrice - takeDistance;

		if (!_shortStop.HasValue && stopDistance > 0m)
		_shortStop = _lastShortPrice + stopDistance;

		var breakEvenDistance = BreakEvenStep > 0 ? BreakEvenStep * _stepValue : 0m;
		var breakEvenOffset = BreakEvenStop >= 0 ? BreakEvenStop * _stepValue : 0m;

		if (!_shortBreakEven && breakEvenDistance > 0m)
		{
			if (_lastShortPrice - candle.LowPrice >= breakEvenDistance)
			{
				_shortBreakEven = true;
				var breakEvenPrice = _lastShortPrice - breakEvenOffset;
				if (!_shortStop.HasValue || _shortStop.Value > breakEvenPrice)
				_shortStop = breakEvenPrice;
			}
		}

		var trailingDistance = TrailingStop > 0 ? TrailingStop * _stepValue : 0m;
		var trailingStep = TrailingStep > 0 ? TrailingStep * _stepValue : trailingDistance;

		if (trailingDistance > 0m)
		{
			if (_shortTrailAnchor == 0m)
			_shortTrailAnchor = _lastShortPrice;

			if (_shortTrailAnchor - candle.LowPrice >= trailingStep)
			{
				_shortTrailAnchor = candle.LowPrice;
				var trailStop = candle.LowPrice + trailingDistance;
				if (!_shortStop.HasValue || _shortStop.Value > trailStop)
				_shortStop = trailStop;
			}
		}
	}

	private void CloseLong()
	{
		if (Position <= 0)
		return;

		SellMarket(Position);
		ResetLongState();
	}

	private void CloseShort()
	{
		if (Position >= 0)
		return;

		BuyMarket(Math.Abs(Position));
		ResetShortState();
	}

	private void ResetLongState()
	{
		_longVolumes.Clear();
		_longStop = null;
		_longTake = null;
		_longBreakEven = false;
		_longTrailAnchor = 0m;
		_longNextLevel = null;
		_pendingLong = null;
	}

	private void ResetShortState()
	{
		_shortVolumes.Clear();
		_shortStop = null;
		_shortTake = null;
		_shortBreakEven = false;
		_shortTrailAnchor = 0m;
		_shortNextLevel = null;
		_pendingShort = null;
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
		return;

		if (order.Direction == Sides.Buy)
		{
			if (Position > 0)
			{
				_longVolumes.Add(trade.Trade.Volume);
				_lastLongPrice = trade.Trade.Price;
				_lastLongVolume = trade.Trade.Volume;
				ResetShortState();
				RecalculateLongLevels();
			}
			else
			{
				ResetShortState();
			}
		}
		else if (order.Direction == Sides.Sell)
		{
			if (Position < 0)
			{
				_shortVolumes.Add(trade.Trade.Volume);
				_lastShortPrice = trade.Trade.Price;
				_lastShortVolume = trade.Trade.Volume;
				ResetLongState();
				RecalculateShortLevels();
			}
			else
			{
				ResetLongState();
			}
		}
	}

	private void RecalculateLongLevels()
	{
		_longStop = StopLoss > 0 ? _lastLongPrice - StopLoss * _stepValue : null;
		_longTake = TakeProfit > 0 ? _lastLongPrice + TakeProfit * _stepValue : null;
		_longBreakEven = false;
		_longTrailAnchor = _lastLongPrice;
		_longNextLevel = ComputeNextLongLevel();
	}

	private void RecalculateShortLevels()
	{
		_shortStop = StopLoss > 0 ? _lastShortPrice + StopLoss * _stepValue : null;
		_shortTake = TakeProfit > 0 ? _lastShortPrice - TakeProfit * _stepValue : null;
		_shortBreakEven = false;
		_shortTrailAnchor = _lastShortPrice;
		_shortNextLevel = ComputeNextShortLevel();
	}

	private decimal? ComputeNextLongLevel()
	{
		if (StepOrders <= 0)
		return null;

		var step = CalculateAdaptiveStep(_longVolumes.Count);
		return _lastLongPrice - step;
	}

	private decimal? ComputeNextShortLevel()
	{
		if (StepOrders <= 0)
		return null;

		var step = CalculateAdaptiveStep(_shortVolumes.Count);
		return _lastShortPrice + step;
	}

	private decimal CalculateAdaptiveStep(int ordersCount)
	{
		var baseStep = Math.Min(StepOrders, MaxStep);
		var step = (decimal)baseStep * _stepValue;

		if (StepMultiplier > 1m && ordersCount > 1)
		{
			var scaled = Math.Min(MaxStep, (int)Math.Round((ordersCount - 1) * StepMultiplier * StepOrders, MidpointRounding.AwayFromZero));
			step = scaled * _stepValue;
		}

		return step;
	}

	private bool AllowLongEntries()
	{
		return Mode == GridTradeMode.Both || Mode == GridTradeMode.Buy;
	}

	private bool AllowShortEntries()
	{
		return Mode == GridTradeMode.Both || Mode == GridTradeMode.Sell;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		if (StartTime.EqualsIgnoreCase("00:00") && EndTime.EqualsIgnoreCase("00:00"))
		return true;

		if (!TimeSpan.TryParseExact(StartTime, "hh\\:mm", CultureInfo.InvariantCulture, out var start))
		return true;

		if (!TimeSpan.TryParseExact(EndTime, "hh\\:mm", CultureInfo.InvariantCulture, out var end))
		return true;

		var current = time.TimeOfDay;

		if (start == end)
		return true;

		var min = start < end ? start : end;
		var max = start > end ? start : end;
		var inRange = current >= min && current < max;

		return start <= end ? inRange : !inRange;
	}
}
