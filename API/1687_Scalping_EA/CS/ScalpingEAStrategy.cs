using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple scalping strategy that keeps pending buy and sell stop orders around current price.
/// Orders are shifted when the market moves away.
/// </summary>
public class ScalpingEAStrategy : Strategy
{
	private readonly StrategyParam<int> _profitTarget;
	private readonly StrategyParam<int> _stopLoss;

	private Order _buyOrder;
	private Order _sellOrder;

	/// <summary>
	/// Profit target in points.
	/// </summary>
	public int ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ScalpingEAStrategy"/>.
	/// </summary>
	public ScalpingEAStrategy()
	{
		_profitTarget = Param(nameof(ProfitTarget), 20)
			.SetDisplay("Profit Target", "Take profit points", "Risk")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 18)
			.SetDisplay("Stop Loss", "Stop loss points", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security.PriceStep ?? 1m;
		StartProtection(new Unit(ProfitTarget * step, UnitTypes.Price), new Unit(StopLoss * step, UnitTypes.Price));

		SubscribeTrades().Bind(ProcessTrade).Start();

		PlaceOrders();
	}

	private void PlaceOrders()
	{
		var price = Security.LastPrice ?? 0m;
		var step = Security.PriceStep ?? 1m;

		var buyPrice = price + 100m * step;
		var sellPrice = price - 100m * step;

		_buyOrder = BuyStop(Volume, buyPrice);
		_sellOrder = SellStop(Volume, sellPrice);
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;
		var step = Security.PriceStep ?? 1m;

		if (_buyOrder?.State == OrderStates.Active)
		{
			var diff = _buyOrder.Price - price;

			if (diff < 5m * step || diff > 70m * step)
			{
				CancelOrder(_buyOrder);
				var buyPrice = price + 100m * step;
				_buyOrder = BuyStop(Volume, buyPrice);
			}
		}

		if (_sellOrder?.State == OrderStates.Active)
		{
			var diff = price - _sellOrder.Price;

			if (diff < 5m * step || diff > 70m * step)
			{
				CancelOrder(_sellOrder);
				var sellPrice = price - 100m * step;
				_sellOrder = SellStop(Volume, sellPrice);
			}
		}
	}
}
