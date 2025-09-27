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
/// ZigZag breakout strategy that brackets the last swing range with stop orders.
/// </summary>
public class ZigZagEAStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<int> _entryOffsetPips;
	private readonly StrategyParam<int> _minCorridorPips;
	private readonly StrategyParam<int> _maxCorridorPips;
	private readonly StrategyParam<FiboLevels> _fiboStopLoss;
	private readonly StrategyParam<FiboLevels> _fiboTakeProfit;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<int> _stopMinute;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _drawCorridorLevels;

	private decimal _currentPivot;
	private decimal _previousPivot;
	private decimal _olderPivot;
	private int _direction;

	private decimal _pipSize;
	private decimal _entryOffset;
	private decimal _minCorridorSize;
	private decimal _maxCorridorSize;
	private decimal _plannedStopDistance;
	private decimal _plannedTakeDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;
	private decimal _stopLevelBuffer;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;

	private bool _trailingActivated;

	/// <summary>
	/// Trading candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ZigZag depth in candles.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// Offset above the swing high and below the swing low in pips.
	/// </summary>
	public int EntryOffsetPips
	{
		get => _entryOffsetPips.Value;
		set => _entryOffsetPips.Value = value;
	}

	/// <summary>
	/// Minimum corridor size in pips.
	/// </summary>
	public int MinCorridorPips
	{
		get => _minCorridorPips.Value;
		set => _minCorridorPips.Value = value;
	}

	/// <summary>
	/// Maximum corridor size in pips.
	/// </summary>
	public int MaxCorridorPips
	{
		get => _maxCorridorPips.Value;
		set => _maxCorridorPips.Value = value;
	}

	/// <summary>
	/// Fibonacci level for the stop loss distance.
	/// </summary>
	public FiboLevels FiboStopLoss
	{
		get => _fiboStopLoss.Value;
		set => _fiboStopLoss.Value = value;
	}

	/// <summary>
	/// Fibonacci level for the take profit distance.
	/// </summary>
	public FiboLevels FiboTakeProfit
	{
		get => _fiboTakeProfit.Value;
		set => _fiboTakeProfit.Value = value;
	}

	/// <summary>
	/// Trading start hour (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Trading stop hour (0-23).
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Trading stop minute.
	/// </summary>
	public int StopMinute
	{
		get => _stopMinute.Value;
		set => _stopMinute.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Draw the corridor levels on the chart.
	/// </summary>
	public bool DrawCorridorLevels
	{
		get => _drawCorridorLevels.Value;
		set => _drawCorridorLevels.Value = value;
	}

	public ZigZagEAStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for analysis", "General");
		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
			.SetDisplay("ZigZag Depth", "Lookback used to define pivots", "ZigZag")
			.SetGreaterThanZero()
			.SetCanOptimize(true);
		_entryOffsetPips = Param(nameof(EntryOffsetPips), 5)
			.SetDisplay("Entry Offset", "Distance above/below swings in pips", "Orders")
			.SetNotNegative()
			.SetCanOptimize(true);
		_minCorridorPips = Param(nameof(MinCorridorPips), 20)
			.SetDisplay("Min Corridor", "Lower bound for swing range", "Orders")
			.SetNotNegative()
			.SetCanOptimize(true);
		_maxCorridorPips = Param(nameof(MaxCorridorPips), 100)
			.SetDisplay("Max Corridor", "Upper bound for swing range", "Orders")
			.SetNotNegative()
			.SetCanOptimize(true);
		_fiboStopLoss = Param(nameof(FiboStopLoss), FiboLevels.Level61_8)
			.SetDisplay("Stop Loss Fibo", "Fibonacci ratio for stop distance", "Risk")
			.SetCanOptimize(true);
		_fiboTakeProfit = Param(nameof(FiboTakeProfit), FiboLevels.Level161_8)
			.SetDisplay("Take Profit Fibo", "Fibonacci ratio for target", "Risk")
			.SetCanOptimize(true);
		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading session start hour", "Time Filter")
			.SetNotNegative()
			.SetCanOptimize(true);
		_startMinute = Param(nameof(StartMinute), 1)
			.SetDisplay("Start Minute", "Trading session start minute", "Time Filter")
			.SetNotNegative()
			.SetCanOptimize(true);
		_stopHour = Param(nameof(StopHour), 23)
			.SetDisplay("Stop Hour", "Trading session end hour", "Time Filter")
			.SetNotNegative()
			.SetCanOptimize(true);
		_stopMinute = Param(nameof(StopMinute), 59)
			.SetDisplay("Stop Minute", "Trading session end minute", "Time Filter")
			.SetNotNegative()
			.SetCanOptimize(true);
		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetNotNegative()
			.SetCanOptimize(true);
		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step", "Increment required to move trailing", "Risk")
			.SetNotNegative()
			.SetCanOptimize(true);
		_drawCorridorLevels = Param(nameof(DrawCorridorLevels), false)
			.SetDisplay("Draw Corridor", "Plot swing corridor on chart", "Visuals");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentPivot = 0m;
		_previousPivot = 0m;
		_olderPivot = 0m;
		_direction = 0;

		_pipSize = 0m;
		_entryOffset = 0m;
		_minCorridorSize = 0m;
		_maxCorridorSize = 0m;
		_plannedStopDistance = 0m;
		_plannedTakeDistance = 0m;
		_trailingStopDistance = 0m;
		_trailingStepDistance = 0m;
		_stopLevelBuffer = 0m;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;

		_trailingActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = ZigZagDepth };
		var lowest = new Lowest { Length = ZigZagDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		InitializeScaling();
	}

	private void InitializeScaling()
	{
		// Pre-calculate price offsets based on the security tick size.
		var step = Security?.PriceStep ?? 1m;
		var decimals = GetDecimalScale(step);
		_pipSize = decimals == 3 || decimals == 5 ? step * 10m : step;

		_entryOffset = EntryOffsetPips * _pipSize;
		_minCorridorSize = MinCorridorPips * _pipSize;
		_maxCorridorSize = MaxCorridorPips * _pipSize;
		_trailingStopDistance = TrailingStopPips * _pipSize;
		_trailingStepDistance = TrailingStepPips * _pipSize;

		var minStep = Security?.MinPriceStep;
		_stopLevelBuffer = minStep ?? step;
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Adjust the trailing stop before analysing new signals.
		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingWindow(candle.OpenTime))
		{
			// Skip order placement outside the configured trading session.
			CancelEntryOrders();
			return;
		}

		UpdatePivots(candle, highest, lowest);

		if (_olderPivot == 0m || _previousPivot == 0m)
			return;

		var high = _previousPivot > _olderPivot ? _previousPivot : _olderPivot;
		var low = _previousPivot < _olderPivot ? _previousPivot : _olderPivot;
		var corridor = high - low;

		if (corridor <= 0m)
		{
			CancelEntryOrders();
			return;
		}

		var lastPivot = _currentPivot;

		var withinCorridor = high > lastPivot + _stopLevelBuffer && low < lastPivot - _stopLevelBuffer;
		var corridorOk = corridor >= _minCorridorSize && corridor <= _maxCorridorSize;

		if (!corridorOk || !withinCorridor || Position != 0)
		{
			CancelEntryOrders();
			return;
		}

		var stopDistance = corridor * GetFiboValue(FiboStopLoss) / 100m;
		var takeDistance = corridor * (GetFiboValue(FiboTakeProfit) / 100m - 1m);
		if (takeDistance < 0m)
			takeDistance = 0m;

		var buyPrice = RoundPrice(high + _entryOffset);
		var sellPrice = RoundPrice(low - _entryOffset);

		UpdateEntryOrders(buyPrice, sellPrice, stopDistance, takeDistance);

		if (DrawCorridorLevels)
			DrawLine(candle.OpenTime, high, candle.OpenTime, low);
	}

	private void UpdatePivots(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.HighPrice >= highest && _direction != 1)
		{
			RegisterPivot(candle.HighPrice);
			_direction = 1;
		}
		else if (candle.LowPrice <= lowest && _direction != -1)
		{
			RegisterPivot(candle.LowPrice);
			_direction = -1;
		}
	}

	private void RegisterPivot(decimal value)
	{
		if (_currentPivot == 0m)
		{
			_currentPivot = value;
			return;
		}

		if (_previousPivot == 0m)
		{
			_previousPivot = _currentPivot;
			_currentPivot = value;
			return;
		}

		_olderPivot = _previousPivot;
		_previousPivot = _currentPivot;
		_currentPivot = value;
	}

	private void UpdateEntryOrders(decimal buyPrice, decimal sellPrice, decimal stopDistance, decimal takeDistance)
	{
		var volume = Volume;

		// Place or refresh breakout stop orders on both sides of the corridor.

		if (buyPrice > 0m)
		{
			if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			{
				if (!ArePricesEqual(_buyStopOrder.Price, buyPrice))
				{
					CancelOrder(_buyStopOrder);
					_buyStopOrder = BuyStop(volume, buyPrice);
				}
			}
			else
			{
				_buyStopOrder = BuyStop(volume, buyPrice);
			}
		}

		if (sellPrice > 0m)
		{
			if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			{
				if (!ArePricesEqual(_sellStopOrder.Price, sellPrice))
				{
					CancelOrder(_sellStopOrder);
					_sellStopOrder = SellStop(volume, sellPrice);
				}
			}
			else
			{
				_sellStopOrder = SellStop(volume, sellPrice);
			}
		}

		_plannedStopDistance = stopDistance;
		_plannedTakeDistance = takeDistance;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		// Move the protective stop when the trailing conditions are satisfied.
		if (Position == 0 || _trailingStopDistance <= 0m || _stopLossOrder == null)
			return;

		var volume = Math.Abs(Position);

		if (Position > 0)
		{
			var profit = candle.HighPrice - PositionPrice;
			if (profit <= _trailingStopDistance + _trailingStepDistance)
				return;

			var newStop = RoundPrice(candle.HighPrice - _trailingStopDistance);
			var currentStop = _stopLossOrder.Price;

			if (newStop <= 0m || (currentStop != 0m && newStop - currentStop < _trailingStepDistance))
				return;

			if (_stopLossOrder.State == OrderStates.Active)
				CancelOrder(_stopLossOrder);

			_stopLossOrder = SellStop(volume, newStop);
			_trailingActivated = true;
		}
		else if (Position < 0)
		{
			var profit = PositionPrice - candle.LowPrice;
			if (profit <= _trailingStopDistance + _trailingStepDistance)
				return;

			var newStop = RoundPrice(candle.LowPrice + _trailingStopDistance);
			var currentStop = _stopLossOrder.Price;

			if (newStop <= 0m || (currentStop != 0m && currentStop - newStop < _trailingStepDistance))
				return;

			if (_stopLossOrder.State == OrderStates.Active)
				CancelOrder(_stopLossOrder);

			_stopLossOrder = BuyStop(volume, newStop);
			_trailingActivated = true;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			CancelProtectionOrders();
			_plannedStopDistance = 0m;
			_plannedTakeDistance = 0m;
			_trailingActivated = false;
			return;
		}

		CancelEntryOrders();
		RegisterProtection(Position > 0);
	}

	private void RegisterProtection(bool isLong)
	{
		// Cancel previous protection and register the new stop-loss and take-profit orders.
		CancelProtectionOrders();

		if (_plannedStopDistance <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var stopPrice = isLong
			? RoundPrice(entryPrice - _plannedStopDistance)
			: RoundPrice(entryPrice + _plannedStopDistance);

		_stopLossOrder = isLong
			? SellStop(volume, stopPrice)
			: BuyStop(volume, stopPrice);

		if (_plannedTakeDistance > 0m)
		{
			var takePrice = isLong
				? RoundPrice(entryPrice + _plannedTakeDistance)
				: RoundPrice(entryPrice - _plannedTakeDistance);

			_takeProfitOrder = isLong
				? SellLimit(volume, takePrice)
				: BuyLimit(volume, takePrice);
		}

		_trailingActivated = false;
	}

	private void CancelEntryOrders()
	{
		if (_buyStopOrder != null)
		{
			if (_buyStopOrder.State == OrderStates.Active)
				CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			if (_sellStopOrder.State == OrderStates.Active)
				CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
	}

	private void CancelProtectionOrders()
	{
		if (_stopLossOrder != null)
		{
			if (_stopLossOrder.State == OrderStates.Active)
				CancelOrder(_stopLossOrder);
			_stopLossOrder = null;
		}

		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active)
				CancelOrder(_takeProfitOrder);
			_takeProfitOrder = null;
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		// Support trading windows that do not cross midnight or wrap to the next day.
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var stop = new TimeSpan(StopHour, StopMinute, 0);
		var current = time.TimeOfDay;

		if (stop > start)
			return current >= start && current <= stop;

		return current >= start || current <= stop;
	}

	private decimal RoundPrice(decimal price)
	{
		// Align prices with the security tick size to avoid invalid order levels.
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		var ratio = price / step;
		var rounded = Math.Round(ratio, MidpointRounding.AwayFromZero);
		return rounded * step;
	}

	private bool ArePricesEqual(decimal left, decimal right)
	{
		var tolerance = (Security?.PriceStep ?? 0m) / 2m;
		if (tolerance <= 0m)
			tolerance = 0.0000001m;
		return Math.Abs(left - right) <= tolerance;
	}

	private static int GetDecimalScale(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	private static decimal GetFiboValue(FiboLevels level)
		=> (decimal)level / 10m;

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelEntryOrders();
		CancelProtectionOrders();
		base.OnStopped();
	}

	public enum FiboLevels
	{
		Level0_0 = 0,
		Level23_6 = 236,
		Level38_2 = 382,
		Level50_0 = 500,
		Level61_8 = 618,
		Level100_0 = 1000,
		Level161_8 = 1618,
		Level261_8 = 2618,
		Level423_6 = 4236
	}
}