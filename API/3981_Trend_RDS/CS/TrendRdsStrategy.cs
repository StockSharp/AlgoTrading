using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader Trend_RDS expert advisor.
/// Detects three-bar momentum patterns near the session start and reverses into the move.
/// Includes configurable trading window, optional signal reversal, and automated exit management.
/// </summary>
public class TrendRdsStrategy : Strategy
{
	private const int MaxPatternDepth = 100;
	private static readonly TimeSpan SignalWindow = TimeSpan.FromMinutes(10);
	private static readonly TimeSpan CloseWindow = TimeSpan.FromMinutes(15);

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal High, decimal Low)> _recentExtremes = new(MaxPatternDepth + 2);

	private decimal _pipSize;
	private decimal _previousPosition;
	private decimal? _entryPrice;
	private Order _stopOrder;
	private Order _takeOrder;

	/// <summary>
	/// Trading volume for market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Time of day when the strategy scans for the pattern.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Time of day when all open trades are closed.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Inverts the buy and sell conditions when enabled.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
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
	/// Additional movement (in pips) required before the trailing stop updates again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Profit in pips required before moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendRdsStrategy"/> class.
	/// </summary>
	public TrendRdsStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Market order volume", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_startTime = Param(nameof(StartTime), new TimeSpan(8, 0, 0))
			.SetDisplay("Start Time", "Time of day to search for the pattern", "Schedule");

		_closeTime = Param(nameof(CloseTime), TimeSpan.Zero)
			.SetDisplay("Close Time", "Time of day to force exits", "Schedule");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert buy and sell signals", "Filters");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetDisplay("Trailing Step (pips)", "Extra pips before trailing adjusts again", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 20m, 1m);

		_breakEvenPips = Param(nameof(BreakEvenPips), 0m)
			.SetDisplay("Break-Even (pips)", "Profit in pips that moves the stop to break-even", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 50m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
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

		_recentExtremes.Clear();
		_entryPrice = null;
		_previousPosition = 0m;
		_stopOrder = null;
		_takeOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		var step = Security.PriceStep ?? Security.MinPriceStep ?? 0m;
		if (step <= 0m)
			throw new InvalidOperationException("Security price step is not available.");

		_pipSize = step;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(candle);

		var candleTime = candle.CloseTime;
		var startActive = IsWithinWindow(candleTime, StartTime, SignalWindow);
		var closeActive = IsWithinWindow(candleTime, CloseTime, CloseWindow);

		if (closeActive && Position != 0m)
		{
			ExitPosition();
			return;
		}

		if (startActive)
		{
			var (buySignal, sellSignal) = DetectSignals();
			if (ProcessSignals(buySignal, sellSignal))
				return;
		}

		ManageActivePosition(candle);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_recentExtremes.Insert(0, (candle.HighPrice, candle.LowPrice));
		if (_recentExtremes.Count > MaxPatternDepth + 2)
			_recentExtremes.RemoveAt(_recentExtremes.Count - 1);
	}

	private (bool Buy, bool Sell) DetectSignals()
	{
		var depth = Math.Min(_recentExtremes.Count - 2, MaxPatternDepth);
		if (depth <= 0)
			return (false, false);

		for (var index = 0; index < depth; index++)
		{
			var first = _recentExtremes[index];
			var second = _recentExtremes[index + 1];
			var third = _recentExtremes[index + 2];

			var conflict = first.High < second.High && second.High < third.High &&
				first.Low > second.Low && second.Low > third.Low;

			if (!conflict && first.Low > second.Low && second.Low > third.Low)
			{
				return ReverseSignals ? (false, true) : (true, false);
			}

			if (!conflict && first.High < second.High && second.High < third.High)
			{
				return ReverseSignals ? (true, false) : (false, true);
			}
		}

		return (false, false);
	}

	private bool ProcessSignals(bool buySignal, bool sellSignal)
	{
		if (!buySignal && !sellSignal)
			return false;

		var volume = TradeVolume;
		if (volume <= 0m)
			return false;

		if (buySignal)
		{
			if (Position < 0m)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (Position == 0m)
			{
				BuyMarket(volume);
				return true;
			}
		}

		if (sellSignal)
		{
			if (Position > 0m)
			{
				SellMarket(Math.Abs(Position));
				return true;
			}

			if (Position == 0m)
			{
				SellMarket(volume);
				return true;
			}
		}

		return false;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0m || _pipSize <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (_entryPrice == null)
			_entryPrice = PositionPrice;

		if (_entryPrice == null || _entryPrice <= 0m)
			return;

		var tolerance = _pipSize / 2m;

		if (BreakEvenPips > 0m)
		{
			var trigger = BreakEvenPips * _pipSize;
			if (Position > 0m)
			{
				var profit = candle.ClosePrice - _entryPrice.Value;
				if (profit >= trigger)
				{
					var targetStop = Math.Max(_entryPrice.Value, candle.ClosePrice - _pipSize);
					if (_stopOrder == null || _stopOrder.Price < targetStop - tolerance)
						UpdateStopPrice(targetStop);
				}
			}
			else if (Position < 0m)
			{
				var profit = _entryPrice.Value - candle.ClosePrice;
				if (profit >= trigger)
				{
					var targetStop = Math.Min(_entryPrice.Value, candle.ClosePrice + _pipSize);
					if (_stopOrder == null || _stopOrder.Price > targetStop + tolerance)
						UpdateStopPrice(targetStop);
				}
			}
		}

		if (TrailingStopPips > 0m)
		{
			var trailingDistance = TrailingStopPips * _pipSize;
			var minimalImprovement = TrailingStepPips > 0m ? TrailingStepPips * _pipSize : tolerance;

			if (Position > 0m)
			{
				var move = candle.ClosePrice - _entryPrice.Value;
				if (move >= trailingDistance)
				{
					var newStop = candle.ClosePrice - trailingDistance;
					if (_stopOrder == null || newStop > _stopOrder.Price + minimalImprovement)
						UpdateStopPrice(newStop);
				}
			}
			else if (Position < 0m)
			{
				var move = _entryPrice.Value - candle.ClosePrice;
				if (move >= trailingDistance)
				{
					var newStop = candle.ClosePrice + trailingDistance;
					if (_stopOrder == null || newStop < _stopOrder.Price - minimalImprovement)
						UpdateStopPrice(newStop);
				}
			}
		}
	}

	private void ExitPosition()
	{
		if (Position > 0m)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void UpdateStopPrice(decimal price)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m || price <= 0m)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = Position > 0m
			? SellStop(volume, price)
			: BuyStop(volume, price);
	}

	private void RegisterProtection(bool isLong)
	{
		CancelProtectionOrders();

		var volume = Math.Abs(Position);
		if (volume <= 0m || _entryPrice == null || _pipSize <= 0m)
			return;

		if (StopLossPips > 0m)
		{
			var offset = StopLossPips * _pipSize;
			var stopPrice = isLong
				? _entryPrice.Value - offset
				: _entryPrice.Value + offset;

			if (stopPrice > 0m)
			{
				_stopOrder = isLong
					? SellStop(volume, stopPrice)
					: BuyStop(volume, stopPrice);
			}
		}

		if (TakeProfitPips > 0m)
		{
			var offset = TakeProfitPips * _pipSize;
			var takePrice = isLong
				? _entryPrice.Value + offset
				: _entryPrice.Value - offset;

			_takeOrder = isLong
				? SellLimit(volume, takePrice)
				: BuyLimit(volume, takePrice);
		}
	}

	private void CancelProtectionOrders()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			_entryPrice = null;
		}
		else if (_previousPosition == 0m)
		{
			_entryPrice = PositionPrice;
			RegisterProtection(Position > 0m);
		}

		_previousPosition = Position;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelProtectionOrders();
		base.OnStopped();
	}

	private static bool IsWithinWindow(DateTimeOffset time, TimeSpan targetTime, TimeSpan tolerance)
	{
		var reference = new DateTimeOffset(
			time.Year,
			time.Month,
			time.Day,
			targetTime.Hours,
			targetTime.Minutes,
			targetTime.Seconds,
			time.Offset);

		var diff = time - reference;
		return diff >= TimeSpan.Zero && diff <= tolerance;
	}
}
