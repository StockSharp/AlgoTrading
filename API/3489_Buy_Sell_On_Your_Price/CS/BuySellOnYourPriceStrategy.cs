using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the BuySellonYourPrice expert advisor.
/// Allows choosing between market, limit, and stop orders with optional stop-loss and take-profit.
/// </summary>
public class BuySellOnYourPriceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _entryPrice;
	private readonly StrategyParam<decimal> _stopLossPrice;
	private readonly StrategyParam<decimal> _takeProfitPrice;
	private readonly StrategyParam<OrderMode> _orderMode;

	/// <summary>
	/// Type of order to execute when the strategy starts.
	/// </summary>
	public enum OrderMode
	{
		/// <summary>
		/// Do not send any order.
		/// </summary>
		None,

		/// <summary>
		/// Send a market buy order.
		/// </summary>
		Buy,

		/// <summary>
		/// Send a market sell order.
		/// </summary>
		Sell,

		/// <summary>
		/// Place a buy limit order.
		/// </summary>
		BuyLimit,

		/// <summary>
		/// Place a sell limit order.
		/// </summary>
		SellLimit,

		/// <summary>
		/// Place a buy stop order.
		/// </summary>
		BuyStop,

		/// <summary>
		/// Place a sell stop order.
		/// </summary>
		SellStop,
	}

	/// <summary>
	/// Requested order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Desired entry price for pending orders.
	/// </summary>
	public decimal EntryPrice
	{
		get => _entryPrice.Value;
		set => _entryPrice.Value = value;
	}

	/// <summary>
	/// Stop-loss level expressed as absolute price.
	/// </summary>
	public decimal StopLossPrice
	{
		get => _stopLossPrice.Value;
		set => _stopLossPrice.Value = value;
	}

	/// <summary>
	/// Take-profit level expressed as absolute price.
	/// </summary>
	public decimal TakeProfitPrice
	{
		get => _takeProfitPrice.Value;
		set => _takeProfitPrice.Value = value;
	}

	/// <summary>
	/// Selected order mode.
	/// </summary>
	public OrderMode Mode
	{
		get => _orderMode.Value;
		set => _orderMode.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BuySellOnYourPriceStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for the generated order", "General");

		_entryPrice = Param(nameof(EntryPrice), 0m)
			.SetDisplay("Entry Price", "Entry price for pending orders", "General");

		_stopLossPrice = Param(nameof(StopLossPrice), 0m)
			.SetDisplay("Stop Loss Price", "Absolute stop-loss level", "Risk Management");

		_takeProfitPrice = Param(nameof(TakeProfitPrice), 0m)
			.SetDisplay("Take Profit Price", "Absolute take-profit level", "Risk Management");

		_orderMode = Param(nameof(Mode), OrderMode.None)
			.SetDisplay("Order Mode", "Type of order to submit", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return Array.Empty<(Security, DataType)>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mode = Mode;
		if (mode == OrderMode.None)
		{
			LogInfo("Order mode is set to None. No orders will be sent.");
			return;
		}

		var volume = OrderVolume;
		if (volume <= 0m)
		{
			LogInfo("Order volume must be greater than zero.");
			return;
		}

		if (Position != 0)
		{
			LogInfo($"Existing position {Position} detected. New orders are not submitted.");
			return;
		}

		if (HasActiveOrders())
		{
			LogInfo("Active orders detected. The expert sends only one order when none are open.");
			return;
		}

		var isBuy = mode is OrderMode.Buy or OrderMode.BuyLimit or OrderMode.BuyStop;
		var entryPrice = ResolveEntryPrice(mode, isBuy);

		if (!ValidateEntryPrice(mode, entryPrice))
		{
			return;
		}

		ConfigureProtection(entryPrice, isBuy);

		switch (mode)
		{
			case OrderMode.Buy:
				BuyMarket(volume);
				LogInfo($"Market buy order sent. Volume={volume}.");
				break;

			case OrderMode.Sell:
				SellMarket(volume);
				LogInfo($"Market sell order sent. Volume={volume}.");
				break;

			case OrderMode.BuyLimit:
				BuyLimit(volume, entryPrice);
				LogInfo($"Buy limit order placed at {entryPrice}. Volume={volume}.");
				break;

			case OrderMode.SellLimit:
				SellLimit(volume, entryPrice);
				LogInfo($"Sell limit order placed at {entryPrice}. Volume={volume}.");
				break;

			case OrderMode.BuyStop:
				BuyStop(volume, entryPrice);
				LogInfo($"Buy stop order placed at {entryPrice}. Volume={volume}.");
				break;

			case OrderMode.SellStop:
				SellStop(volume, entryPrice);
				LogInfo($"Sell stop order placed at {entryPrice}. Volume={volume}.");
				break;
		}
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State == OrderStates.Active)
				return true;
		}

		return false;
	}

	private decimal ResolveEntryPrice(OrderMode mode, bool isBuy)
	{
		if (mode == OrderMode.Buy || mode == OrderMode.Sell)
		{
			var bestBid = Security.BestBid?.Price ?? 0m;
			var bestAsk = Security.BestAsk?.Price ?? 0m;
			var lastPrice = Security.LastPrice ?? 0m;

			if (isBuy && bestAsk > 0m)
				return bestAsk;

			if (!isBuy && bestBid > 0m)
				return bestBid;

			if (lastPrice > 0m)
				return lastPrice;

			return EntryPrice;
		}

		return EntryPrice;
	}

	private bool ValidateEntryPrice(OrderMode mode, decimal entryPrice)
	{
		if (mode == OrderMode.Buy || mode == OrderMode.Sell)
		{
			if (entryPrice <= 0m)
			{
				LogInfo("Market price is not available yet. Order submission will be postponed.");
				return false;
			}

			return true;
		}

		if (entryPrice <= 0m)
		{
			LogInfo("Entry price must be greater than zero for pending orders.");
			return false;
		}

		return true;
	}

	private void ConfigureProtection(decimal entryPrice, bool isBuy)
	{
		if (entryPrice <= 0m)
			return;

		var stopLossDiff = 0m;
		var takeProfitDiff = 0m;

		var stopLoss = StopLossPrice;
		var takeProfit = TakeProfitPrice;

		if (isBuy)
		{
			if (takeProfit > entryPrice)
				takeProfitDiff = takeProfit - entryPrice;

			if (stopLoss > 0m && stopLoss < entryPrice)
				stopLossDiff = entryPrice - stopLoss;
		}
		else
		{
			if (takeProfit > 0m && takeProfit < entryPrice)
				takeProfitDiff = entryPrice - takeProfit;

			if (stopLoss > entryPrice)
				stopLossDiff = stopLoss - entryPrice;
		}

		if (takeProfitDiff <= 0m && stopLossDiff <= 0m)
			return;

		var tpUnit = takeProfitDiff > 0m ? new Unit(takeProfitDiff, UnitTypes.Absolute) : new Unit(0m, UnitTypes.Absolute);
		var slUnit = stopLossDiff > 0m ? new Unit(stopLossDiff, UnitTypes.Absolute) : new Unit(0m, UnitTypes.Absolute);

		StartProtection(tpUnit, slUnit);
	}
}
