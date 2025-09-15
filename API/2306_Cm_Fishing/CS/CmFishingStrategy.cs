using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy based on fixed price steps.
/// </summary>
public class CmFishingStrategy : Strategy
{
	private readonly StrategyParam<bool> _buy;
	private readonly StrategyParam<bool> _sell;
	private readonly StrategyParam<decimal> _stepBuy;
	private readonly StrategyParam<decimal> _stepSell;
	private readonly StrategyParam<decimal> _closeProfitBuy;
	private readonly StrategyParam<decimal> _closeProfitSell;
	private readonly StrategyParam<decimal> _closeProfitAll;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;

	private decimal _level;
	private decimal _entryPrice;

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool Buy
	{
		get => _buy.Value;
		set => _buy.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool Sell
	{
		get => _sell.Value;
		set => _sell.Value = value;
	}

	/// <summary>
	/// Price step for additional long trades.
	/// </summary>
	public decimal StepBuy
	{
		get => _stepBuy.Value;
		set => _stepBuy.Value = value;
	}

	/// <summary>
	/// Price step for additional short trades.
	/// </summary>
	public decimal StepSell
	{
		get => _stepSell.Value;
		set => _stepSell.Value = value;
	}

	/// <summary>
	/// Profit target to close long positions.
	/// </summary>
	public decimal CloseProfitBuy
	{
		get => _closeProfitBuy.Value;
		set => _closeProfitBuy.Value = value;
	}

	/// <summary>
	/// Profit target to close short positions.
	/// </summary>
	public decimal CloseProfitSell
	{
		get => _closeProfitSell.Value;
		set => _closeProfitSell.Value = value;
	}

	/// <summary>
	/// Profit target to close any position.
	/// </summary>
	public decimal CloseProfit
	{
		get => _closeProfitAll.Value;
		set => _closeProfitAll.Value = value;
	}

	/// <summary>
	/// Volume for buy orders.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Volume for sell orders.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	public CmFishingStrategy()
	{
		_buy = Param(nameof(Buy), true).SetDisplay("Buy", "Enable long trades", "Parameters");
		_sell = Param(nameof(Sell), true).SetDisplay("Sell", "Enable short trades", "Parameters");
		_stepBuy = Param(nameof(StepBuy), 10m).SetDisplay("Step Buy", "Price step for long trades", "Parameters");
		_stepSell = Param(nameof(StepSell), 10m).SetDisplay("Step Sell", "Price step for short trades", "Parameters");
		_closeProfitBuy = Param(nameof(CloseProfitBuy), 100m).SetDisplay("Close Profit Buy", "Profit to close long positions", "Parameters");
		_closeProfitSell = Param(nameof(CloseProfitSell), 100m).SetDisplay("Close Profit Sell", "Profit to close short positions", "Parameters");
		_closeProfitAll = Param(nameof(CloseProfit), 10m).SetDisplay("Close Profit", "Profit to close any position", "Parameters");
		_buyVolume = Param(nameof(BuyVolume), 0.1m).SetDisplay("Buy Volume", "Volume for buy orders", "Parameters");
		_sellVolume = Param(nameof(SellVolume), 0.1m).SetDisplay("Sell Volume", "Volume for sell orders", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_level = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var lastTrade = Security.LastTrade;
		_level = lastTrade?.Price ?? 0m;
		_entryPrice = 0m;

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = trade.TradePrice ?? 0m;
		var absPos = Math.Abs(Position);

		if (Buy && Position >= 0m && price <= _level - StepBuy)
		{
			BuyMarket(BuyVolume);
			UpdateEntryPrice(price, BuyVolume, absPos);
			_level = price;
		}

		if (Sell && Position <= 0m && price >= _level + StepSell)
		{
			SellMarket(SellVolume);
			UpdateEntryPrice(price, SellVolume, absPos);
			_level = price;
		}

		var profit = Position > 0m
			? (price - _entryPrice) * Position
			: (_entryPrice - price) * -Position;

		if (Position > 0m && (profit >= CloseProfitBuy || profit >= CloseProfit))
		{
			SellMarket(Position);
			_entryPrice = 0m;
			_level = price;
			return;
		}

		if (Position < 0m && (profit >= CloseProfitSell || profit >= CloseProfit))
		{
			BuyMarket(-Position);
			_entryPrice = 0m;
			_level = price;
		}
	}

	private void UpdateEntryPrice(decimal price, decimal volume, decimal currentPos)
	{
		var newPos = currentPos + volume;
		if (newPos <= 0m)
			return;

		_entryPrice = (_entryPrice * currentPos + price * volume) / newPos;
	}
}
