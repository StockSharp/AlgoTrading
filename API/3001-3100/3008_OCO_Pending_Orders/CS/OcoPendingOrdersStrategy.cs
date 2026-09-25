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
		_orderVolume = Param(nameof(OrderVolume), 1m).SetGreaterThanZero();
		_buyLimitPrice = Param(nameof(BuyLimitPrice), 0m).SetNotNegative();
		_buyStopPrice = Param(nameof(BuyStopPrice), 0m).SetNotNegative();
		_sellLimitPrice = Param(nameof(SellLimitPrice), 0m).SetNotNegative();
		_sellStopPrice = Param(nameof(SellStopPrice), 0m).SetNotNegative();
		_stopLossPips = Param(nameof(StopLossPips), 0).SetNotNegative();
		_takeProfitPips = Param(nameof(TakeProfitPips), 0).SetNotNegative();
		_useOcoLink = Param(nameof(UseOcoLink), true);
		_armed = Param(nameof(Armed), false);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var take = TakeProfitPips > 0 ? new Unit(TakeProfitPips * step, UnitTypes.Absolute) : null;
		var stop = StopLossPips > 0 ? new Unit(StopLossPips * step, UnitTypes.Absolute) : null;
		if (take is not null || stop is not null)
			StartProtection(takeProfit: take, stopLoss: stop, useMarketOrders: true);

		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (!Armed)
			return;

		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);

		if (ask is decimal bestAsk)
		{
			if (BuyLimitPrice > 0m && bestAsk <= BuyLimitPrice)
			{
				BuyLimitPrice = 0m;
				Execute(Sides.Buy);
				return;
			}

			if (BuyStopPrice > 0m && bestAsk >= BuyStopPrice)
			{
				BuyStopPrice = 0m;
				Execute(Sides.Buy);
				return;
			}
		}

		if (bid is decimal bestBid)
		{
			if (SellLimitPrice > 0m && bestBid >= SellLimitPrice)
			{
				SellLimitPrice = 0m;
				Execute(Sides.Sell);
				return;
			}

			if (SellStopPrice > 0m && bestBid <= SellStopPrice)
			{
				SellStopPrice = 0m;
				Execute(Sides.Sell);
				return;
			}
		}

		DisarmIfEmpty();
	}

	private void Execute(Sides side)
	{
		if (side == Sides.Buy)
			BuyMarket(OrderVolume);
		else
			SellMarket(OrderVolume);

		if (UseOcoLink)
		{
			ClearLevels();
			Armed = false;
		}
		else
			DisarmIfEmpty();
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
		if (BuyLimitPrice <= 0m && BuyStopPrice <= 0m && SellLimitPrice <= 0m && SellStopPrice <= 0m)
			Armed = false;
	}
}
