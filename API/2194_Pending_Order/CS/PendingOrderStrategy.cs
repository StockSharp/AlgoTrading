using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places four pending orders around current price during specific hours.
/// </summary>
public class PendingOrderStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _distance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _tickSize;

	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int Distance { get => _distance.Value; set => _distance.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PendingOrderStrategy()
	{
		_startHour = Param(nameof(StartHour), 6)
			.SetDisplay("Start Hour", "Hour to start placing pending orders", "General")
			.SetCanOptimize();

		_endHour = Param(nameof(EndHour), 20)
			.SetDisplay("End Hour", "Hour to stop placing pending orders", "General")
			.SetCanOptimize();

		_takeProfit = Param(nameof(TakeProfit), 20)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk")
			.SetCanOptimize();

		_stopLoss = Param(nameof(StopLoss), 100)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk")
			.SetCanOptimize();

		_distance = Param(nameof(Distance), 15)
			.SetDisplay("Distance", "Distance from market price to place orders", "General")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for time tracking", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security.PriceStep ?? 1m;

		SubscribeCandles(CandleType)
			.Bind(OnProcessCandle)
			.Start();
	}

	private void OnProcessCandle(ICandleMessage candle)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check that strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = candle.OpenTime.Hour;
		var canTrade = StartHour > EndHour
			? hour >= StartHour || hour < EndHour
			: hour >= StartHour && hour < EndHour;

		if (!canTrade)
			return;

		var bestBid = Security.BestBid?.Price ?? candle.ClosePrice;
		var bestAsk = Security.BestAsk?.Price ?? candle.ClosePrice;

		// Calculate prices for pending orders
		var buyLimitPrice = bestAsk - Distance * _tickSize;
		var sellLimitPrice = bestBid + Distance * _tickSize;
		var buyStopPrice = bestAsk + Distance * _tickSize;
		var sellStopPrice = bestBid - Distance * _tickSize;

		// Place orders only if similar active order is absent
		if (!HasActiveOrder(OrderTypes.Limit, Sides.Buy))
			BuyLimit(buyLimitPrice);

		if (!HasActiveOrder(OrderTypes.Limit, Sides.Sell))
			SellLimit(sellLimitPrice);

		if (!HasActiveOrder(OrderTypes.Stop, Sides.Buy))
			BuyStop(buyStopPrice);

		if (!HasActiveOrder(OrderTypes.Stop, Sides.Sell))
			SellStop(sellStopPrice);
	}

	private bool HasActiveOrder(OrderTypes type, Sides side)
	{
		foreach (var order in Orders)
		{
			if (order.State == OrderStates.Active && order.Type == type && order.Direction == side)
				return true;
		}

		return false;
	}
}
