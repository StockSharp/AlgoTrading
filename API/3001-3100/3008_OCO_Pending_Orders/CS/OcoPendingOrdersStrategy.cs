using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manually armed OCO price triggers driven by Level1 bid/ask updates.
/// </summary>
public class OcoPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _buyLimitPrice;
	private readonly StrategyParam<decimal> _buyStopPrice;
	private readonly StrategyParam<decimal> _sellLimitPrice;
	private readonly StrategyParam<decimal> _sellStopPrice;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useOcoLink;
	private readonly StrategyParam<bool> _armed;
	private decimal[] _initialLevels;
	private bool _initialArmed;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public decimal BuyLimitPrice { get => _buyLimitPrice.Value; set => _buyLimitPrice.Value = value; }
	public decimal BuyStopPrice { get => _buyStopPrice.Value; set => _buyStopPrice.Value = value; }
	public decimal SellLimitPrice { get => _sellLimitPrice.Value; set => _sellLimitPrice.Value = value; }
	public decimal SellStopPrice { get => _sellStopPrice.Value; set => _sellStopPrice.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public bool UseOcoLink { get => _useOcoLink.Value; set => _useOcoLink.Value = value; }
	public bool Armed { get => _armed.Value; set => _armed.Value = value; }

	public OcoPendingOrdersStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Volume sent with each market order", "Trading");
		_buyLimitPrice = Param(nameof(BuyLimitPrice), 0m)
			.SetNotNegative()
			.SetDisplay("Buy limit price", "Ask price threshold that activates a limit-style long entry, 0 disables", "Levels");
		_buyStopPrice = Param(nameof(BuyStopPrice), 0m)
			.SetNotNegative()
			.SetDisplay("Buy stop price", "Ask price threshold that activates a stop-style long entry, 0 disables", "Levels");
		_sellLimitPrice = Param(nameof(SellLimitPrice), 0m)
			.SetNotNegative()
			.SetDisplay("Sell limit price", "Bid price threshold that activates a limit-style short entry, 0 disables", "Levels");
		_sellStopPrice = Param(nameof(SellStopPrice), 0m)
			.SetNotNegative()
			.SetDisplay("Sell stop price", "Bid price threshold that activates a stop-style short entry, 0 disables", "Levels");
		_stopLossPips = Param(nameof(StopLossPips), 0)
			.SetNotNegative()
			.SetDisplay("Stop loss (pips)", "Protective stop distance in instrument points, multiplied by the price step", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 0)
			.SetNotNegative()
			.SetDisplay("Take profit (pips)", "Profit target distance in instrument points, multiplied by the price step", "Risk");
		_useOcoLink = Param(nameof(UseOcoLink), true)
			.SetDisplay("Use OCO link", "The first filled order clears the remaining price levels and disarms the strategy", "Control");
		_armed = Param(nameof(Armed), false)
			.SetDisplay("Armed", "Safety switch for the triggers, reset to false when no active level remains", "Control");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		if (_initialLevels != null)
		{
			BuyLimitPrice = _initialLevels[0];
			BuyStopPrice = _initialLevels[1];
			SellLimitPrice = _initialLevels[2];
			SellStopPrice = _initialLevels[3];
			Armed = _initialArmed;
		}
		_initialLevels = null;
		_initialArmed = false;
		_bestBid = null;
		_bestAsk = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_initialLevels = [BuyLimitPrice, BuyStopPrice, SellLimitPrice, SellStopPrice];
		_initialArmed = Armed;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var take = TakeProfitPips > 0 ? new Unit(TakeProfitPips * step, UnitTypes.Absolute) : null;
		var stop = StopLossPips > 0 ? new Unit(StopLossPips * step, UnitTypes.Absolute) : null;
		if (take is not null || stop is not null)
			StartProtection(takeProfit: take, stopLoss: stop, useMarketOrders: true);

		// A Level1 binding only sees updates carrying its build field, so bid-only and ask-only updates need one binding each.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessLevel1).Start();
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_bestAsk = ask;

		if (!Armed)
			return;

		if (_bestAsk is decimal bestAsk)
		{
			if (BuyLimitPrice > 0m && bestAsk <= BuyLimitPrice)
			{
				var level = BuyLimitPrice;
				BuyLimitPrice = 0m;
				Execute(Sides.Buy, $"Buy limit {level} hit by ask {bestAsk}");

				if (!Armed)
					return;
			}

			if (BuyStopPrice > 0m && bestAsk >= BuyStopPrice)
			{
				var level = BuyStopPrice;
				BuyStopPrice = 0m;
				Execute(Sides.Buy, $"Buy stop {level} hit by ask {bestAsk}");

				if (!Armed)
					return;
			}
		}

		if (_bestBid is decimal bestBid)
		{
			if (SellLimitPrice > 0m && bestBid >= SellLimitPrice)
			{
				var level = SellLimitPrice;
				SellLimitPrice = 0m;
				Execute(Sides.Sell, $"Sell limit {level} hit by bid {bestBid}");

				if (!Armed)
					return;
			}

			if (SellStopPrice > 0m && bestBid <= SellStopPrice)
			{
				var level = SellStopPrice;
				SellStopPrice = 0m;
				Execute(Sides.Sell, $"Sell stop {level} hit by bid {bestBid}");

				if (!Armed)
					return;
			}
		}

		DisarmIfEmpty();
	}

	private void Execute(Sides side, string trigger)
	{
		LogInfo($"{trigger}: {side} {OrderVolume} at market.");

		if (side == Sides.Buy)
			BuyMarket(OrderVolume);
		else
			SellMarket(OrderVolume);

		if (!UseOcoLink)
			return;

		ClearLevels();
		Armed = false;
		LogInfo("OCO link cleared all trigger levels; strategy disarmed.");
	}

	private void ClearLevels()
	{
		BuyLimitPrice = 0m;
		BuyStopPrice = 0m;
		SellLimitPrice = 0m;
		SellStopPrice = 0m;
	}

	private void DisarmIfEmpty()
	{
		if (BuyLimitPrice > 0m || BuyStopPrice > 0m || SellLimitPrice > 0m || SellStopPrice > 0m)
			return;

		Armed = false;
		LogInfo("No trigger levels remain; strategy disarmed.");
	}
}
