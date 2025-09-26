using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Alexav SpeedUp M1" MetaTrader expert advisor.
/// The strategy opens market positions when the previous candle body exceeds a configurable threshold
/// and manages protective stop-loss, take-profit, and trailing stop orders in a MetaTrader-like manner.
/// </summary>
public class AlexavSpeedUpM1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialStopPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _openCloseDifference;
	private readonly StrategyParam<DataType> _candleType;

	private Order _protectiveStopOrder;
	private Order _takeProfitOrder;

	private decimal? _entryPrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	/// <summary>
	/// Trading volume expressed in lots.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader "points".
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in MetaTrader "points".
	/// </summary>
	public decimal InitialStopPoints
	{
		get => _initialStopPoints.Value;
		set => _initialStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in MetaTrader "points".
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum difference between candle close and open that triggers a trade.
	/// </summary>
	public decimal OpenCloseDifference
	{
		get => _openCloseDifference.Value;
		set => _openCloseDifference.Value = value;
	}

	/// <summary>
	/// Time frame of the candle series used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AlexavSpeedUpM1Strategy"/> class.
	/// </summary>
	public AlexavSpeedUpM1Strategy()
	{
		_lotSize = Param(nameof(LotSize), 1m)
		.SetDisplay("Lot Size", "Base trading volume in lots", "Risk Management")
		.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 26m)
		.SetDisplay("Take Profit (points)", "Distance from entry to the take-profit price", "Risk Management")
		.SetNotNegative();

		_initialStopPoints = Param(nameof(InitialStopPoints), 23m)
		.SetDisplay("Initial Stop (points)", "Initial stop-loss distance from the entry price", "Risk Management")
		.SetNotNegative();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 23m)
		.SetDisplay("Trailing Stop (points)", "Trailing stop distance maintained after price advances", "Risk Management")
		.SetNotNegative();

		_openCloseDifference = Param(nameof(OpenCloseDifference), 0.001m)
		.SetDisplay("Body Threshold", "Minimum candle body size required to open a trade", "Signal")
		.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used to analyse candle bodies", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		CancelProtectionOrders();
		_entryPrice = null;
		_bestBid = null;
		_bestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Reproduce the MetaTrader layout by drawing candles and executed trades.
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var volume = LotSize;
		if (volume <= 0m)
		return;

		var body = candle.ClosePrice - candle.OpenPrice;
		var threshold = OpenCloseDifference;

		if (body > threshold && Position <= 0m)
		{
			var orderVolume = volume + Math.Max(0m, -Position);
			if (orderVolume > 0m)
			{
				// Open a long trade when the candle closes significantly above its open.
				BuyMarket(orderVolume);
			}
		}
		else if (-body > threshold && Position >= 0m)
		{
			var orderVolume = volume + Math.Max(0m, Position);
			if (orderVolume > 0m)
			{
				// Open a short trade when the candle closes significantly below its open.
				SellMarket(orderVolume);
			}
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_bestAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateTrailingStop();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			_entryPrice = null;
			return;
		}

		var averagePrice = Position.AveragePrice;
		if (averagePrice == null || averagePrice <= 0m)
		{
			// Fall back to the previously recorded entry price if the broker does not provide an average.
			if (_entryPrice == null || _entryPrice <= 0m)
			return;
		}
		else
		{
			_entryPrice = averagePrice;
		}

		var volume = Math.Abs(Position);
		if (volume <= 0m || _entryPrice is not decimal entry)
		return;

		// Register protective orders that mimic MetaTrader stop-loss and take-profit behaviour.
		if (Position > 0m && delta > 0m)
		{
			SetupProtectionOrders(true, volume, entry);
		}
		else if (Position < 0m && delta < 0m)
		{
			SetupProtectionOrders(false, volume, entry);
		}
	}

	private void SetupProtectionOrders(bool isLong, decimal volume, decimal entryPrice)
	{
		CancelIfActive(ref _protectiveStopOrder);
		CancelIfActive(ref _takeProfitOrder);

		var stopDistance = ConvertPoints(InitialStopPoints);
		var takeDistance = ConvertPoints(TakeProfitPoints);

		if (isLong)
		{
			if (stopDistance > 0m)
			{
				var stopPrice = ShrinkPrice(entryPrice - stopDistance);
				if (stopPrice > 0m)
				_protectiveStopOrder = SellStop(volume, stopPrice);
			}

			if (takeDistance > 0m)
			{
				var takePrice = ShrinkPrice(entryPrice + takeDistance);
				if (takePrice > 0m)
				_takeProfitOrder = SellLimit(volume, takePrice);
			}
		}
		else
		{
			if (stopDistance > 0m)
			{
				var stopPrice = ShrinkPrice(entryPrice + stopDistance);
				if (stopPrice > 0m)
				_protectiveStopOrder = BuyStop(volume, stopPrice);
			}

			if (takeDistance > 0m)
			{
				var takePrice = ShrinkPrice(entryPrice - takeDistance);
				if (takePrice > 0m)
				_takeProfitOrder = BuyLimit(volume, takePrice);
			}
		}
	}

	private void UpdateTrailingStop()
	{
		var trailingDistance = ConvertPoints(TrailingStopPoints);
		if (trailingDistance <= 0m || _entryPrice is not decimal entry)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var step = Security?.PriceStep ?? 0m;
		var tolerance = step > 0m ? step / 2m : 0m;

		if (Position > 0m && _bestBid is decimal bid && bid > 0m)
		{
			var profit = bid - entry;
			if (profit <= trailingDistance)
			return;

			var newStop = ShrinkPrice(bid - trailingDistance);
			UpdateTrailingOrder(true, newStop, volume, tolerance);
		}
		else if (Position < 0m && _bestAsk is decimal ask && ask > 0m)
		{
			var profit = entry - ask;
			if (profit <= trailingDistance)
			return;

			var newStop = ShrinkPrice(ask + trailingDistance);
			UpdateTrailingOrder(false, newStop, volume, tolerance);
		}
	}

	private void UpdateTrailingOrder(bool isLong, decimal newStop, decimal volume, decimal tolerance)
	{
		if (newStop <= 0m)
		return;

		if (_protectiveStopOrder != null)
		{
			if (_protectiveStopOrder.State == OrderStates.Active)
			{
				if (isLong)
				{
					if (newStop <= _protectiveStopOrder.Price + tolerance)
					return;
				}
				else
				{
					if (newStop >= _protectiveStopOrder.Price - tolerance)
					return;
				}

				CancelOrder(_protectiveStopOrder);
			}
			else if (_protectiveStopOrder.State == OrderStates.Done)
			{
				_protectiveStopOrder = null;
			}
		}

		_protectiveStopOrder = isLong
		? SellStop(volume, newStop)
		: BuyStop(volume, newStop);
	}

	private void CancelProtectionOrders()
	{
		CancelIfActive(ref _protectiveStopOrder);
		CancelIfActive(ref _takeProfitOrder);
	}

	private void CancelIfActive(ref Order order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private decimal ConvertPoints(decimal points)
	{
		var step = Security?.PriceStep;
		if (step == null || step == 0m)
		return points;

		return points * step.Value;
	}

	private decimal ShrinkPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}
}
