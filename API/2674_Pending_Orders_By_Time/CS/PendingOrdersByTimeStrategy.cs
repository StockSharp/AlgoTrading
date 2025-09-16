using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Places symmetric stop orders at scheduled hours and manages them with daily resets.
/// </summary>
public class PendingOrdersByTimeStrategy : Strategy
{
	private readonly StrategyParam<int> _openingHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	public int OpeningHour
	{
		get => _openingHour.Value;
		set => _openingHour.Value = value;
	}

	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PendingOrdersByTimeStrategy()
	{
		_openingHour = Param(nameof(OpeningHour), 9)
			.SetDisplay("Opening Hour", "Hour to activate pending orders", "Schedule")
			.SetRange(0, 23);

		_closingHour = Param(nameof(ClosingHour), 2)
			.SetDisplay("Closing Hour", "Hour to cancel orders and flat positions", "Schedule")
			.SetRange(0, 23);

		_distancePips = Param(nameof(DistancePips), 20m)
			.SetDisplay("Distance (pips)", "Offset for entry stop orders", "Orders")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
			.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
			.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk")
			.SetGreaterThanZero();

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume for pending orders", "Orders")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe for the schedule", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var stopLossUnit = StopLossPips > 0m
			? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute)
			: new Unit();

		var takeProfitUnit = TakeProfitPips > 0m
			? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute)
			: new Unit();

		// Attach platform-managed stop loss and take profit orders.
		StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit, useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
			return 1m;

		var digits = 0;
		var tmp = step;

		// Count decimals to emulate the pip adjustment used in the original MQL version.
		while (tmp < 1m && digits < 10)
		{
			tmp *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			step *= 10m;

		return step;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Evaluate risk rules on each completed bar to mimic tick-level supervision.
	ManageRisk(candle.ClosePrice);

		var hour = candle.CloseTime.Hour;

		if (hour == ClosingHour)
		{
			// Closing hour: cancel remaining pending orders and exit any open trades.
			CancelPendingOrders();
			ExitPosition();
		}

		if (hour == OpeningHour && hour != ClosingHour && Position == 0m)
		{
			// Opening hour: deploy the new batch of pending stop orders.
			PlacePendingOrders(candle.ClosePrice);
		}
	}

	private void ManageRisk(decimal closePrice)
	{
		if (_pipSize <= 0m)
			return;

		var takeProfitDistance = TakeProfitPips * _pipSize;
		var stopLossDistance = StopLossPips * _pipSize;

		if (Position > 0m)
		{
			// Lock profits for long trades when the target distance is reached.
			if (takeProfitDistance > 0m && closePrice - PositionPrice >= takeProfitDistance)
				SellMarket(Position);

			// Exit long trades if price retraces by the configured stop distance.
			if (stopLossDistance > 0m && PositionPrice - closePrice >= stopLossDistance)
				SellMarket(Position);
		}
		else if (Position < 0m)
		{
			// Lock profits for short trades when the target distance is reached.
			if (takeProfitDistance > 0m && PositionPrice - closePrice >= takeProfitDistance)
				BuyMarket(-Position);

			// Exit short trades if price rallies by the configured stop distance.
			if (stopLossDistance > 0m && closePrice - PositionPrice >= stopLossDistance)
				BuyMarket(-Position);
		}
	}

	private void ExitPosition()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(-Position);
	}

	private void PlacePendingOrders(decimal referencePrice)
	{
		CancelPendingOrders();

		if (_pipSize <= 0m || OrderVolume <= 0m)
			return;

		var distance = DistancePips * _pipSize;

		if (distance <= 0m)
			return;

		var bestBid = Security?.BestBid?.Price ?? referencePrice;
		var bestAsk = Security?.BestAsk?.Price ?? referencePrice;

		var sellStopPrice = bestBid - distance;
		var buyStopPrice = bestAsk + distance;

		if (sellStopPrice <= 0m || buyStopPrice <= 0m)
			return;

		// Register stop orders symmetrically around the current spread.
		_sellStopOrder = SellStop(OrderVolume, sellStopPrice);
		_buyStopOrder = BuyStop(OrderVolume, buyStopPrice);
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
	}
}
