using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-breakout session strategy converted from the original MQL implementation.
/// Places stop entries around the previous hourly range and manages exits with staged take-profits and break-even protection.
/// </summary>
public class MultiBreakoutV001kStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _numberOfOrdersPerSide;
	private readonly StrategyParam<int> _takeProfitIncrement;
	private readonly StrategyParam<int> _pipsForEntry;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _breakEven;
	private readonly StrategyParam<bool> _movingBreakEven;
	private readonly StrategyParam<int> _movingBreakEvenHoursToStart;
	private readonly StrategyParam<int> _brokerOffsetToGmt;
	private readonly StrategyParam<bool> _tradeSession1;
	private readonly StrategyParam<int> _sessionHour1;
	private readonly StrategyParam<bool> _tradeSession2;
	private readonly StrategyParam<int> _sessionHour2;
	private readonly StrategyParam<bool> _tradeSession3;
	private readonly StrategyParam<int> _sessionHour3;
	private readonly StrategyParam<bool> _tradeSession4;
	private readonly StrategyParam<int> _sessionHour4;
	private readonly StrategyParam<int> _exitMinute;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<Order> _longEntryOrders = new();
	private readonly List<Order> _shortEntryOrders = new();
	private readonly Dictionary<Order, decimal> _entryTargets = new();
	private readonly List<decimal> _longTargets = new();
	private readonly List<decimal> _shortTargets = new();
	private readonly Dictionary<int, DateTime> _lastEntryDate = new();
	private readonly Dictionary<int, DateTime> _lastExitDate = new();

	private decimal? _bestAsk;
	private decimal? _bestBid;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longStopDistance;
	private decimal? _shortStopDistance;
	private bool _longBreakEvenActivated;
	private bool _shortBreakEvenActivated;
	private DateTime? _longMovingBreakEvenTime;
	private DateTime? _shortMovingBreakEvenTime;

	private decimal? _previousHourLow;
	private decimal? _previousHourLow2;
	private decimal? _previousHourHigh;
	private decimal? _previousHourHigh2;

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiBreakoutV001kStrategy"/> class.
	/// </summary>
	public MultiBreakoutV001kStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Volume", "Order volume for each breakout order", "Trading")
		.SetGreaterThanZero();

		_numberOfOrdersPerSide = Param(nameof(NumberOfOrdersPerSide), 20)
		.SetDisplay("Orders Per Side", "How many stop orders to stack on each side", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1, 30, 1);

		_takeProfitIncrement = Param(nameof(TakeProfitIncrement), 5)
		.SetDisplay("TP Increment", "Distance in points between staged take profits", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_pipsForEntry = Param(nameof(PipsForEntry), 5)
		.SetDisplay("Entry Buffer", "Extra points added beyond the range breakout", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1, 20, 1);

		_stopLoss = Param(nameof(StopLoss), 20)
		.SetDisplay("Stop Loss", "Initial protective stop distance in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 5);

		_breakEven = Param(nameof(BreakEven), 10)
		.SetDisplay("Break Even", "Distance in points before moving the stop to break-even", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_movingBreakEven = Param(nameof(MovingBreakEven), true)
		.SetDisplay("Moving Break-Even", "Enable adaptive break-even trailing", "Risk");

		_movingBreakEvenHoursToStart = Param(nameof(MovingBreakEvenHoursToStart), 3)
		.SetDisplay("MBE Hours", "Hours after session start to activate moving break-even", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1, 6, 1);

		_brokerOffsetToGmt = Param(nameof(BrokerOffsetToGmt), 0)
		.SetDisplay("Broker Offset", "Broker server offset relative to UTC", "General");

		_tradeSession1 = Param(nameof(TradeSession1), true)
		.SetDisplay("Trade Session 1", "Enable breakout trading around the first hour", "Schedule");

		_sessionHour1 = Param(nameof(SessionHour1), 6)
		.SetDisplay("Session Hour 1", "Hour for the first trading session (0-23)", "Schedule")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_tradeSession2 = Param(nameof(TradeSession2), true)
		.SetDisplay("Trade Session 2", "Enable breakout trading around the second hour", "Schedule");

		_sessionHour2 = Param(nameof(SessionHour2), 12)
		.SetDisplay("Session Hour 2", "Hour for the second trading session (0-23)", "Schedule")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_tradeSession3 = Param(nameof(TradeSession3), true)
		.SetDisplay("Trade Session 3", "Enable breakout trading around the third hour", "Schedule");

		_sessionHour3 = Param(nameof(SessionHour3), 18)
		.SetDisplay("Session Hour 3", "Hour for the third trading session (0-23)", "Schedule")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_tradeSession4 = Param(nameof(TradeSession4), true)
		.SetDisplay("Trade Session 4", "Enable breakout trading around the fourth hour", "Schedule");

		_sessionHour4 = Param(nameof(SessionHour4), 0)
		.SetDisplay("Session Hour 4", "Hour for the fourth trading session (0-23)", "Schedule")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_exitMinute = Param(nameof(ExitMinute), 55)
		.SetDisplay("Exit Minute", "Minute within the session hour to liquidate everything", "Schedule")
		.SetCanOptimize(true)
		.SetOptimize(0, 59, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for range detection", "General");
	}

	/// <summary>
	/// Order volume for each breakout order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Number of stop orders to place on each side of the market.
	/// </summary>
	public int NumberOfOrdersPerSide
	{
		get => _numberOfOrdersPerSide.Value;
		set => _numberOfOrdersPerSide.Value = value;
	}

	/// <summary>
	/// Take-profit increment in points.
	/// </summary>
	public int TakeProfitIncrement
	{
		get => _takeProfitIncrement.Value;
		set => _takeProfitIncrement.Value = value;
	}

	/// <summary>
	/// Entry buffer in points above/below the hourly range.
	/// </summary>
	public int PipsForEntry
	{
		get => _pipsForEntry.Value;
		set => _pipsForEntry.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Break-even activation distance in points.
	/// </summary>
	public int BreakEven
	{
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}

	/// <summary>
	/// Enable or disable moving break-even trailing.
	/// </summary>
	public bool MovingBreakEven
	{
		get => _movingBreakEven.Value;
		set => _movingBreakEven.Value = value;
	}

	/// <summary>
	/// Hours after session start before moving break-even can trail.
	/// </summary>
	public int MovingBreakEvenHoursToStart
	{
		get => _movingBreakEvenHoursToStart.Value;
		set => _movingBreakEvenHoursToStart.Value = value;
	}

	/// <summary>
	/// Broker server offset relative to UTC.
	/// </summary>
	public int BrokerOffsetToGmt
	{
		get => _brokerOffsetToGmt.Value;
		set => _brokerOffsetToGmt.Value = value;
	}

	/// <summary>
	/// Enable the first trading session.
	/// </summary>
	public bool TradeSession1
	{
		get => _tradeSession1.Value;
		set => _tradeSession1.Value = value;
	}

	/// <summary>
	/// Hour for the first session.
	/// </summary>
	public int SessionHour1
	{
		get => _sessionHour1.Value;
		set => _sessionHour1.Value = value;
	}

	/// <summary>
	/// Enable the second trading session.
	/// </summary>
	public bool TradeSession2
	{
		get => _tradeSession2.Value;
		set => _tradeSession2.Value = value;
	}

	/// <summary>
	/// Hour for the second session.
	/// </summary>
	public int SessionHour2
	{
		get => _sessionHour2.Value;
		set => _sessionHour2.Value = value;
	}

	/// <summary>
	/// Enable the third trading session.
	/// </summary>
	public bool TradeSession3
	{
		get => _tradeSession3.Value;
		set => _tradeSession3.Value = value;
	}

	/// <summary>
	/// Hour for the third session.
	/// </summary>
	public int SessionHour3
	{
		get => _sessionHour3.Value;
		set => _sessionHour3.Value = value;
	}

	/// <summary>
	/// Enable the fourth trading session.
	/// </summary>
	public bool TradeSession4
	{
		get => _tradeSession4.Value;
		set => _tradeSession4.Value = value;
	}

	/// <summary>
	/// Hour for the fourth session.
	/// </summary>
	public int SessionHour4
	{
		get => _sessionHour4.Value;
		set => _sessionHour4.Value = value;
	}

	/// <summary>
	/// Minute of the session hour to close all activity.
	/// </summary>
	public int ExitMinute
	{
		get => _exitMinute.Value;
		set => _exitMinute.Value = value;
	}

	/// <summary>
	/// Candle type used for range detection.
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

		_longEntryOrders.Clear();
		_shortEntryOrders.Clear();
		_entryTargets.Clear();
		_longTargets.Clear();
		_shortTargets.Clear();
		_lastEntryDate.Clear();
		_lastExitDate.Clear();

		_bestAsk = null;
		_bestBid = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longStopDistance = null;
		_shortStopDistance = null;
		_longBreakEvenActivated = false;
		_shortBreakEvenActivated = false;
		_longMovingBreakEvenTime = null;
		_shortMovingBreakEvenTime = null;

		_previousHourLow = null;
		_previousHourLow2 = null;
		_previousHourHigh = null;
		_previousHourHigh2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
		.Bind(ProcessHourCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			var candleSubscription = GetSubscription(Security, CandleType);
			if (candleSubscription != null)
			{
				DrawCandles(area, candleSubscription);
				DrawOwnTrades(area);
			}
		}
	}

	private void ProcessHourCandle(ICandleMessage candle)
	{
		// Use the just-finished hourly candle to set breakout orders for the upcoming hour.
		if (candle.State != CandleStates.Finished)
		return;

		UpdateHourlyExtremes(candle);

		var rangeTime = ApplyBrokerOffset(candle.OpenTime);
		if (!TryGetSessionIndexForEntry(rangeTime.Hour, out var sessionIndex, out var sessionHour))
		return;

		if (_lastEntryDate.TryGetValue(sessionIndex, out var lastDate) && lastDate.Date == rangeTime.Date)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var entryTime = ApplyBrokerOffset(candle.CloseTime);
		ScheduleBreakoutOrders(candle, entryTime, sessionIndex, sessionHour);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		_bestAsk = (decimal)askObj;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		_bestBid = (decimal)bidObj;

		var time = level1.ServerTime != default ? level1.ServerTime : level1.LocalTime;
		if (time != default)
		{
			var adjustedTime = ApplyBrokerOffset(time);
			CheckExitSchedule(adjustedTime);
			ManagePositions(adjustedTime);
		}
	}

	private void ManagePositions(DateTime serverTime)
	{
		if (_bestBid is decimal bid && Position > 0 && _longEntryPrice is decimal longEntry)
		{
			EnsureLongProtection(serverTime, bid, longEntry);
		}

		if (_bestAsk is decimal ask && Position < 0 && _shortEntryPrice is decimal shortEntry)
		{
			EnsureShortProtection(serverTime, ask, shortEntry);
		}
	}

	private void EnsureLongProtection(DateTime serverTime, decimal bid, decimal longEntry)
	{
		var step = GetPointValue();
		if (step <= 0m)
		return;

		var breakEvenDistance = BreakEven > 0 ? BreakEven * step : 0m;
		if (!_longBreakEvenActivated && breakEvenDistance > 0m && bid - longEntry >= breakEvenDistance)
		{
			_longStopPrice = Math.Max(_longStopPrice ?? decimal.MinValue, longEntry);
			_longBreakEvenActivated = true;
		}

		if (MovingBreakEven && _longBreakEvenActivated && _longMovingBreakEvenTime is DateTime mbeTime && serverTime >= mbeTime)
		{
			if (_previousHourLow is decimal prevLow && _previousHourLow2 is decimal prevLow2 && prevLow > prevLow2)
			{
				_longStopPrice = Math.Max(_longStopPrice ?? decimal.MinValue, prevLow);
			}
		}

		if (_longTargets.Count > 0 && bid >= _longTargets[0])
		{
			// Close one tranche whenever the price tags the next staged profit target.
			var volumeToClose = Math.Min(Position, TradeVolume);
			if (volumeToClose > 0m)
			{
				SellMarket(volumeToClose);
				_longTargets.RemoveAt(0);
			}
		}
		else if (_longStopPrice is decimal stop && bid <= stop)
		{
			SellMarket(Position);
			_longTargets.Clear();
		}
	}

	private void EnsureShortProtection(DateTime serverTime, decimal ask, decimal shortEntry)
	{
		var step = GetPointValue();
		if (step <= 0m)
		return;

		var breakEvenDistance = BreakEven > 0 ? BreakEven * step : 0m;
		if (!_shortBreakEvenActivated && breakEvenDistance > 0m && shortEntry - ask >= breakEvenDistance)
		{
			_shortStopPrice = Math.Min(_shortStopPrice ?? decimal.MaxValue, shortEntry);
			_shortBreakEvenActivated = true;
		}

		if (MovingBreakEven && _shortBreakEvenActivated && _shortMovingBreakEvenTime is DateTime mbeTime && serverTime >= mbeTime)
		{
			if (_previousHourHigh is decimal prevHigh && _previousHourHigh2 is decimal prevHigh2 && prevHigh < prevHigh2)
			{
				_shortStopPrice = Math.Min(_shortStopPrice ?? decimal.MaxValue, prevHigh);
			}
		}

		if (_shortTargets.Count > 0)
		{
			var index = _shortTargets.Count - 1;
			var target = _shortTargets[index];
			if (ask <= target)
			{
				// Mirror the staged exits for short trades as price drops through the profit ladder.
				var volumeToClose = Math.Min(-Position, TradeVolume);
				if (volumeToClose > 0m)
				{
					BuyMarket(volumeToClose);
					_shortTargets.RemoveAt(index);
				}
			}
		}
		else if (_shortStopPrice is decimal stop && ask >= stop)
		{
			BuyMarket(-Position);
			_shortTargets.Clear();
		}
	}

	private void ScheduleBreakoutOrders(ICandleMessage candle, DateTime serverTime, int sessionIndex, int sessionHour)
	{
		CancelEntryOrders(_longEntryOrders);
		CancelEntryOrders(_shortEntryOrders);
		_longEntryOrders.Clear();
		_shortEntryOrders.Clear();
		_entryTargets.Clear();
		_longTargets.Clear();
		_shortTargets.Clear();

		var step = GetPointValue();
		if (step <= 0m)
		return;

		var spread = _bestAsk is decimal ask && _bestBid is decimal bid ? Math.Max(0m, ask - bid) : 0m;
		var entryOffset = PipsForEntry > 0 ? PipsForEntry * step : 0m;
		var tpIncrement = TakeProfitIncrement > 0 ? TakeProfitIncrement * step : step;
		var stopDistance = StopLoss > 0 ? StopLoss * step : 0m;

		var buyPrice = candle.HighPrice + spread + entryOffset;
		var sellPrice = candle.LowPrice - entryOffset;

		if (buyPrice > 0m)
		{
			for (var i = 1; i <= NumberOfOrdersPerSide; i++)
			{
				var target = buyPrice + tpIncrement * i;
				var order = BuyStop(TradeVolume, buyPrice);
				if (order != null)
				{
					_longEntryOrders.Add(order);
					_entryTargets[order] = target;
				}
			}

			_longEntryPrice = buyPrice;
			_longStopPrice = stopDistance > 0m ? buyPrice - stopDistance : null;
			_longStopDistance = stopDistance > 0m ? stopDistance : null;
			_longBreakEvenActivated = false;
			_longMovingBreakEvenTime = MovingBreakEven
			? CalculateMovingBreakEvenStart(serverTime.Date, sessionHour)
			: null;
		}

		if (sellPrice > 0m)
		{
			for (var i = 1; i <= NumberOfOrdersPerSide; i++)
			{
				var target = sellPrice - tpIncrement * i;
				var order = SellStop(TradeVolume, sellPrice);
				if (order != null)
				{
					_shortEntryOrders.Add(order);
					_entryTargets[order] = target;
				}
			}

			_shortEntryPrice = sellPrice;
			_shortStopPrice = stopDistance > 0m ? sellPrice + stopDistance : null;
			_shortStopDistance = stopDistance > 0m ? stopDistance : null;
			_shortBreakEvenActivated = false;
			_shortMovingBreakEvenTime = MovingBreakEven
			? CalculateMovingBreakEvenStart(serverTime.Date, sessionHour)
			: null;
		}

		_lastEntryDate[sessionIndex] = serverTime;
	}

	private void CheckExitSchedule(DateTime serverTime)
	{
		foreach (var session in EnumerateSessions())
		{
			if (!session.enabled)
			continue;

			if (serverTime.Hour != session.hour || serverTime.Minute < ExitMinute)
			continue;

			if (_lastExitDate.TryGetValue(session.index, out var last) && last.Date == serverTime.Date)
			continue;

			CloseAllPositionsAndOrders();
			_lastExitDate[session.index] = serverTime;
		}
	}

	private void CloseAllPositionsAndOrders()
	{
		CancelEntryOrders(_longEntryOrders);
		CancelEntryOrders(_shortEntryOrders);
		_longEntryOrders.Clear();
		_shortEntryOrders.Clear();
		_entryTargets.Clear();
		_longTargets.Clear();
		_shortTargets.Clear();

		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longStopDistance = null;
		_shortStopDistance = null;
		_longBreakEvenActivated = false;
		_shortBreakEvenActivated = false;
		_longMovingBreakEvenTime = null;
		_shortMovingBreakEvenTime = null;
	}

	private void CancelEntryOrders(List<Order> orders)
	{
		foreach (var order in orders)
		{
			if (order.State == OrderStates.Active)
			{
				CancelOrder(order);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (!_entryTargets.TryGetValue(trade.Order, out var target))
		return;

		var volume = trade.Trade.Volume;
		if (trade.Order.Direction == Sides.Buy)
		{
			_longEntryPrice = trade.Trade.Price;
			if (_longStopDistance is decimal stopDistance)
			{
				_longStopPrice = _longEntryPrice - stopDistance;
			}

			for (var remaining = volume; remaining > 0m; remaining -= TradeVolume)
			{
				InsertAscending(_longTargets, target);
			}

			_longEntryOrders.Remove(trade.Order);
		}
		else if (trade.Order.Direction == Sides.Sell)
		{
			_shortEntryPrice = trade.Trade.Price;
			if (_shortStopDistance is decimal stopDistance)
			{
				_shortStopPrice = _shortEntryPrice + stopDistance;
			}

			for (var remaining = volume; remaining > 0m; remaining -= TradeVolume)
			{
				InsertAscending(_shortTargets, target);
			}

			_shortEntryOrders.Remove(trade.Order);
		}

		if (trade.Order.State == OrderStates.Done)
		{
			_entryTargets.Remove(trade.Order);
		}
	}

	private void UpdateHourlyExtremes(ICandleMessage candle)
	{
		_previousHourLow2 = _previousHourLow;
		_previousHourLow = candle.LowPrice;
		_previousHourHigh2 = _previousHourHigh;
		_previousHourHigh = candle.HighPrice;
	}

	private bool TryGetSessionIndexForEntry(int hour, out int index, out int sessionHour)
	{
		foreach (var session in EnumerateSessions())
		{
			if (!session.enabled)
			continue;

			// Orders are placed one hour after the configured range session finishes.
			var entryHour = NormalizeHour(session.hour + 1);
			if (entryHour == hour)
			{
				index = session.index;
				sessionHour = session.hour;
				return true;
			}
		}

		index = default;
		sessionHour = default;
		return false;
	}

	private IEnumerable<(int index, bool enabled, int hour)> EnumerateSessions()
	{
		yield return (1, TradeSession1, SessionHour1);
		yield return (2, TradeSession2, SessionHour2);
		yield return (3, TradeSession3, SessionHour3);
		yield return (4, TradeSession4, SessionHour4);
	}

	private DateTime ApplyBrokerOffset(DateTimeOffset time)
	{
		var utc = time.UtcDateTime;
		return utc.AddHours(BrokerOffsetToGmt);
	}

	private DateTime? CalculateMovingBreakEvenStart(DateTime date, int sessionHour)
	{
		var normalizedSessionHour = NormalizeHour(sessionHour);
		// Align the moving break-even activation with the MQL schedule (session hour + offset + delay).
		var mbeHour = NormalizeHour(normalizedSessionHour + BrokerOffsetToGmt + MovingBreakEvenHoursToStart);
		var mbeTime = date.Date.AddHours(mbeHour);
		if (mbeHour <= normalizedSessionHour)
		{
			mbeTime = mbeTime.AddDays(1);
		}

		return mbeTime;
	}

	private static int NormalizeHour(int hour)
	{
		hour %= 24;
		if (hour < 0)
		{
			hour += 24;
		}

		return hour;
	}

	private static void InsertAscending(List<decimal> values, decimal value)
	{
		var index = values.BinarySearch(value);
		if (index < 0)
		{
			index = ~index;
		}
		else
		{
			while (index < values.Count && values[index] == value)
			{
				index++;
			}
		}

		values.Insert(index, value);
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep ?? Security?.Step ?? 0m;
		if (step <= 0m)
		{
			return 0m;
		}

		return step;
	}
}
