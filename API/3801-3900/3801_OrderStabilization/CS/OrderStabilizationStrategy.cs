using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.MatchingEngine;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Paired breakout stop orders with stabilization-based exits.
/// </summary>
public class OrderStabilizationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _orderDistancePoints;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<decimal> _absoluteFixation;
	private readonly StrategyParam<decimal> _stabilizationPoints;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyStop;
	private Order _sellStop;
	private DateTime? _createdAt;
	private decimal _filledPosition;
	private decimal _entryValue;
	private decimal _previousBody;
	private bool _hasPreviousBody;

	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public decimal OrderDistancePoints { get => _orderDistancePoints.Value; set => _orderDistancePoints.Value = value; }
	public decimal ProfitThreshold { get => _profitThreshold.Value; set => _profitThreshold.Value = value; }
	public decimal AbsoluteFixation { get => _absoluteFixation.Value; set => _absoluteFixation.Value = value; }
	public decimal StabilizationPoints { get => _stabilizationPoints.Value; set => _stabilizationPoints.Value = value; }
	public int ExpirationMinutes { get => _expirationMinutes.Value; set => _expirationMinutes.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrderStabilizationStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade volume in lots used for both stop entries", "Trading");
		_orderDistancePoints = Param(nameof(OrderDistancePoints), 20m).SetGreaterThanZero()
			.SetDisplay("Order Distance", "Distance between the current close price and each stop order, in points", "Trading");
		_profitThreshold = Param(nameof(ProfitThreshold), -2m)
			.SetDisplay("Profit Threshold", "Minimum floating profit (account currency) required before an exit triggered by stabilization is allowed", "Exit");
		_absoluteFixation = Param(nameof(AbsoluteFixation), 30m).SetNotNegative()
			.SetDisplay("Absolute Fixation", "Profit level (account currency) that forces an immediate exit", "Exit");
		_stabilizationPoints = Param(nameof(StabilizationPoints), 25m).SetGreaterThanZero()
			.SetDisplay("Stabilization", "Maximum candle body size (points) that signals a flat market", "Exit");
		_expirationMinutes = Param(nameof(ExpirationMinutes), 20).SetNotNegative()
			.SetDisplay("Expiration", "Lifetime of pending stop orders in minutes, 0 disables expiration", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to evaluate stabilization", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Ticks)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_buyStop = null;
		_sellStop = null;
		_createdAt = null;
		_filledPosition = 0m;
		_entryValue = 0m;
		_previousBody = 0m;
		_hasPreviousBody = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		// Stop orders are matched against trade prints, so they fire between candle closes.
		Subscribe(new Subscription(DataType.Ticks, Security));
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var point = Security?.PriceStep ?? 1m;
		if (point <= 0m)
			point = 1m;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (Position != 0m && IsExitDue(candle.ClosePrice, body, point))
		{
			CancelIfActive(_buyStop);
			CancelIfActive(_sellStop);
			Flatten();
		}
		else if ((Position == 0m && !IsWorking(_buyStop) && !IsWorking(_sellStop)) || IsExpired(candle.OpenTime))
		{
			ArmStops(candle.ClosePrice, candle.OpenTime, point);
		}

		_previousBody = body;
		_hasPreviousBody = true;
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var signed = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previous = _filledPosition;

		_filledPosition += signed;

		if (_filledPosition == 0m)
			_entryValue = 0m;
		else if (previous == 0m || Math.Sign(previous) == Math.Sign(signed))
			_entryValue += price * volume;
		else if (Math.Sign(_filledPosition) != Math.Sign(previous))
			_entryValue = price * Math.Abs(_filledPosition);
		else
			_entryValue -= _entryValue / Math.Abs(previous) * volume;
	}

	private bool IsExitDue(decimal price, decimal body, decimal point)
	{
		var bodyLimit = StabilizationPoints * point;
		var pnl = FloatingPnL(price, point);
		var oneSmall = body <= bodyLimit;
		var twoSmall = oneSmall && _hasPreviousBody && _previousBody <= bodyLimit;

		return (oneSmall && pnl > ProfitThreshold) ||
			twoSmall ||
			(AbsoluteFixation > 0m && pnl >= AbsoluteFixation);
	}

	private bool IsExpired(DateTime time)
		=> ExpirationMinutes > 0 &&
			_createdAt is DateTime created &&
			time - created >= TimeSpan.FromMinutes(ExpirationMinutes);

	// Flat: both stops are placed around the close. With a position open only the pending
	// opposite stop is moved; the side that opened the position is never placed again.
	private void ArmStops(decimal center, DateTime time, decimal point)
	{
		var distance = OrderDistancePoints * point;

		if (Position == 0m || (Position < 0m && IsWorking(_buyStop)))
			_buyStop = RenewStop(_buyStop, Sides.Buy, center + distance);

		if (Position == 0m || (Position > 0m && IsWorking(_sellStop)))
			_sellStop = RenewStop(_sellStop, Sides.Sell, center - distance);

		_createdAt = IsWorking(_buyStop) || IsWorking(_sellStop) ? time : null;
	}

	private Order RenewStop(Order order, Sides side, decimal activationPrice)
	{
		if (order is { State: OrderStates.Active })
			CancelOrder(order);
		else if (IsWorking(order))
			return order;

		var stop = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Side = side,
			Volume = OrderVolume,
			Type = OrderTypes.Conditional,
			Condition = new StopOrderCondition { ActivationPrice = activationPrice },
		};

		RegisterOrder(stop);
		return stop;
	}

	private static bool IsWorking(Order order)
		=> order is not null && order.State is not (OrderStates.Done or OrderStates.Failed);

	private void CancelIfActive(Order order)
	{
		if (order is { State: OrderStates.Active })
			CancelOrder(order);
	}

	private decimal FloatingPnL(decimal price, decimal point)
	{
		if (Position == 0m || _filledPosition == 0m)
			return 0m;

		var entryPrice = _entryValue / Math.Abs(_filledPosition);
		var direction = Position > 0m ? 1m : -1m;
		var move = (price - entryPrice) * direction;
		var stepPrice = Security?.StepPrice ?? 0m;

		return stepPrice > 0m
			? move / point * stepPrice * Math.Abs(Position)
			: move * (Security?.Multiplier ?? 1m) * Math.Abs(Position);
	}

	private void Flatten()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}
}
