using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hans123 breakout strategy that arms stop orders twice per day based on the recent 5-minute range.
/// Ported from the MetaTrader expert advisor Hans123Trader.
/// </summary>
public class Hans123TraderStrategy : Strategy
{
	private readonly StrategyParam<int> _beginSession1;
	private readonly StrategyParam<int> _endSession1;
	private readonly StrategyParam<int> _beginSession2;
	private readonly StrategyParam<int> _endSession2;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialStopLossPoints;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<decimal> _breakoutOffsetPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private Order _session1BuyStop;
	private Order _session1SellStop;
	private Order _session2BuyStop;
	private Order _session2SellStop;

	private DateTime? _session1OrderDate;
	private DateTime? _session2OrderDate;
	private DateTime? _lastManagedDate;

	private DateTimeOffset? _positionEntryTime;
	private decimal? _positionEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// First session start hour (kept for compatibility).
	/// </summary>
	public int BeginSession1
	{
		get => _beginSession1.Value;
		set => _beginSession1.Value = value;
	}

	/// <summary>
	/// First session end hour when pending orders are armed.
	/// </summary>
	public int EndSession1
	{
		get => _endSession1.Value;
		set => _endSession1.Value = value;
	}

	/// <summary>
	/// Second session start hour (kept for compatibility).
	/// </summary>
	public int BeginSession2
	{
		get => _beginSession2.Value;
		set => _beginSession2.Value = value;
	}

	/// <summary>
	/// Second session end hour when pending orders are armed.
	/// </summary>
	public int EndSession2
	{
		get => _endSession2.Value;
		set => _endSession2.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in points.
	/// </summary>
	public decimal InitialStopLoss
	{
		get => _initialStopLossPoints.Value;
		set => _initialStopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to analyse the 5-minute range.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles analysed when computing the breakout range.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// Distance in points between the range extremes and the breakout stop orders.
	/// </summary>
	public decimal BreakoutOffsetPoints
	{
		get => _breakoutOffsetPoints.Value;
		set => _breakoutOffsetPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Hans123TraderStrategy"/> class.
	/// </summary>
	public Hans123TraderStrategy()
	{
		_rangeLength = Param(nameof(RangeLength), 80)
			.SetDisplay("Range Length", "Number of candles used to compute the breakout range", "Breakout")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_breakoutOffsetPoints = Param(nameof(BreakoutOffsetPoints), 5m)
			.SetDisplay("Breakout Offset", "Distance in points added above/below the range for stop orders", "Breakout")
			.SetNotNegative()
			.SetCanOptimize(true);

		_beginSession1 = Param(nameof(BeginSession1), 6)
			.SetDisplay("Begin Session 1", "First monitoring window start hour", "Time")
			.SetCanOptimize(true);

		_endSession1 = Param(nameof(EndSession1), 10)
			.SetDisplay("End Session 1", "Hour when first breakout orders are armed", "Time")
			.SetCanOptimize(true);

		_beginSession2 = Param(nameof(BeginSession2), 10)
			.SetDisplay("Begin Session 2", "Second monitoring window start hour", "Time")
			.SetCanOptimize(true);

		_endSession2 = Param(nameof(EndSession2), 14)
			.SetDisplay("End Session 2", "Hour when second breakout orders are armed", "Time")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStop), 0m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
			.SetCanOptimize(true);

		_initialStopLossPoints = Param(nameof(InitialStopLoss), 40m)
			.SetDisplay("Initial Stop Loss", "Initial stop-loss distance in points", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for range detection", "Data");

		Volume = 1m;
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

		_highest = null!;
		_lowest = null!;

		_session1BuyStop = null;
		_session1SellStop = null;
		_session2BuyStop = null;
		_session2SellStop = null;

		_session1OrderDate = null;
		_session2OrderDate = null;
		_lastManagedDate = null;

		_positionEntryTime = null;
		_positionEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Indicators track the highest high and lowest low over the recent range.
		_highest = new Highest { Length = RangeLength };
		_lowest = new Lowest { Length = RangeLength };

		// Subscribe to candle data and process new values once each bar is finished.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Visualize the working candles and executed trades when possible.
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentDate = candle.CloseTime.Date;

		// Reset pending orders at the start of each new calendar day.
		ResetDailyState(currentDate);

		// Update indicator values using the finished candle.
		var highestValue = _highest.Process(candle.HighPrice).ToNullableDecimal();
		var lowestValue = _lowest.Process(candle.LowPrice).ToNullableDecimal();

		// Manage open positions: apply stops, take-profit, trailing and daily close-out.
		ManageOpenPosition(candle, currentDate);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (highestValue is not decimal highest || lowestValue is not decimal lowest)
			return;

		if (candle.CloseTime.Minute != 0)
			return;

		// Arm breakout orders at configured hours for session 1 and session 2.
		if (candle.CloseTime.Hour == EndSession1)
			PlaceSessionOrders(SessionSlot.First, highest, lowest, currentDate);

		if (candle.CloseTime.Hour == EndSession2)
			PlaceSessionOrders(SessionSlot.Second, highest, lowest, currentDate);
	}

	private void ManageOpenPosition(ICandleMessage candle, DateTime currentDate)
	{
		if (Position == 0m)
			return;

		if (_positionEntryTime.HasValue && _positionEntryTime.Value.Date < currentDate)
		{
			// Close any position that survived into the next day.
			ClosePosition();
			return;
		}

		var entryPrice = _positionEntryPrice ?? Position.AveragePrice ?? candle.ClosePrice;

		if (Position > 0m)
		{
			// Exit on take-profit hits using the candle's high.
			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			// Exit on protective stop hits using the candle's low.
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			// Apply trailing stop only after the initial stop exists and profit is positive.
			if (TrailingStop > 0m && _longStopPrice.HasValue && candle.ClosePrice > entryPrice)
			{
				var distance = PointsToPrice(TrailingStop);
				if (distance > 0m)
				{
					var candidate = NormalizePrice(candle.ClosePrice - distance);
					if (candidate > _longStopPrice.Value)
						_longStopPrice = candidate;

					if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
					{
						SellMarket(Math.Abs(Position));
						return;
					}
				}
			}
		}
		else if (Position < 0m)
		{
			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (TrailingStop > 0m && _shortStopPrice.HasValue && candle.ClosePrice < entryPrice)
			{
				var distance = PointsToPrice(TrailingStop);
				if (distance > 0m)
				{
					var candidate = NormalizePrice(candle.ClosePrice + distance);
					if (candidate < _shortStopPrice.Value)
						_shortStopPrice = candidate;

					if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
					{
						BuyMarket(Math.Abs(Position));
						return;
					}
				}
			}
		}
	}

	private void ResetDailyState(DateTime currentDate)
	{
		if (_lastManagedDate == currentDate)
			return;

		_lastManagedDate = currentDate;

		// Cancel any pending orders that belong to the previous day.
		CancelIfActive(ref _session1BuyStop);
		CancelIfActive(ref _session1SellStop);
		CancelIfActive(ref _session2BuyStop);
		CancelIfActive(ref _session2SellStop);

		_session1OrderDate = null;
		_session2OrderDate = null;
	}

	private void PlaceSessionOrders(SessionSlot slot, decimal highest, decimal lowest, DateTime date)
	{
		ref Order buyOrder = ref slot == SessionSlot.First ? ref _session1BuyStop : ref _session2BuyStop;
		ref Order sellOrder = ref slot == SessionSlot.First ? ref _session1SellStop : ref _session2SellStop;
		ref DateTime? placedDate = ref slot == SessionSlot.First ? ref _session1OrderDate : ref _session2OrderDate;

		if (placedDate == date)
			return;

		var volume = Volume;
		if (volume <= 0m)
			return;

		CancelIfActive(ref buyOrder);
		CancelIfActive(ref sellOrder);

		var offset = PointsToPrice(BreakoutOffsetPoints);
		var buyPrice = NormalizePrice(highest + offset);
		var sellPrice = NormalizePrice(lowest - offset);

		buyOrder = BuyStop(volume, buyPrice);
		sellOrder = SellStop(volume, sellPrice);

		if (buyOrder != null || sellOrder != null)
			placedDate = date;
	}

	private void CancelIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		ClearIfCompleted(ref _session1BuyStop, order);
		ClearIfCompleted(ref _session1SellStop, order);
		ClearIfCompleted(ref _session2BuyStop, order);
		ClearIfCompleted(ref _session2SellStop, order);
	}

	private static void ClearIfCompleted(ref Order trackedOrder, Order changedOrder)
	{
		if (trackedOrder == null || trackedOrder != changedOrder)
			return;

		if (changedOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			trackedOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_positionEntryTime = null;
			_positionEntryPrice = null;
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			return;
		}

		_positionEntryTime = CurrentTime;
		var averagePrice = Position.AveragePrice ?? 0m;
		_positionEntryPrice = averagePrice;

		if (Position > 0m)
		{
			_longStopPrice = InitialStopLoss > 0m ? NormalizePrice(averagePrice - PointsToPrice(InitialStopLoss)) : null;
			_longTakePrice = TakeProfit > 0m ? NormalizePrice(averagePrice + PointsToPrice(TakeProfit)) : null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (Position < 0m)
		{
			_shortStopPrice = InitialStopLoss > 0m ? NormalizePrice(averagePrice + PointsToPrice(InitialStopLoss)) : null;
			_shortTakePrice = TakeProfit > 0m ? NormalizePrice(averagePrice - PointsToPrice(TakeProfit)) : null;
			_longStopPrice = null;
			_longTakePrice = null;
		}
	}

	private decimal PointsToPrice(decimal points)
	{
		var step = Security?.PriceStep;
		if (step.HasValue && step.Value > 0m)
			return points * step.Value;

		return points;
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private enum SessionSlot
	{
		First,
		Second
	}
}
