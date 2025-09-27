namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Translated version of the TargetEA utility that supervises existing positions and pending orders.
/// Closes baskets of buy and sell orders independently or jointly when user defined profit or loss thresholds are reached.
/// </summary>
public class TargetEaManagerStrategy : Strategy
{
	private readonly StrategyParam<ManageMode> _manageMode;
	private readonly StrategyParam<bool> _closeBuyOrders;
	private readonly StrategyParam<bool> _closeSellOrders;
	private readonly StrategyParam<bool> _cancelBuyPendings;
	private readonly StrategyParam<bool> _cancelSellPendings;
	private readonly StrategyParam<TargetCalculationMode> _targetMode;
	private readonly StrategyParam<bool> _closeInProfit;
	private readonly StrategyParam<decimal> _profitTargetPips;
	private readonly StrategyParam<decimal> _profitTargetCurrency;
	private readonly StrategyParam<decimal> _profitTargetPercent;
	private readonly StrategyParam<bool> _closeInLoss;
	private readonly StrategyParam<decimal> _lossTargetPips;
	private readonly StrategyParam<decimal> _lossTargetCurrency;
	private readonly StrategyParam<decimal> _lossTargetPercent;

	private decimal? _lastBid;
	private decimal? _lastAsk;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;

	private enum ManageMode
	{
		Separate,
		Combined
	}

	private enum TargetCalculationMode
	{
		Pips,
		CurrencyPerLot,
		PercentageOfBalance
	}

	public TargetEaManagerStrategy()
	{
		_manageMode = Param(nameof(ManageBuySellOrders), ManageMode.Separate)
			.SetDisplay("Manage buy/sell", "Decide whether buy and sell orders are handled separately or as one basket.", "General");

		_closeBuyOrders = Param(nameof(CloseBuyOrders), true)
			.SetDisplay("Close buys", "Allow liquidation of buy positions when conditions are met.", "General");

		_closeSellOrders = Param(nameof(CloseSellOrders), true)
			.SetDisplay("Close sells", "Allow liquidation of sell positions when conditions are met.", "General");

		_cancelBuyPendings = Param(nameof(DeleteBuyPendingPositions), true)
			.SetDisplay("Cancel buy pendings", "Cancel active buy pending orders when the basket closes.", "General");

		_cancelSellPendings = Param(nameof(DeleteSellPendingPositions), true)
			.SetDisplay("Cancel sell pendings", "Cancel active sell pending orders when the basket closes.", "General");

		_targetMode = Param(nameof(TypeTargetUse), TargetCalculationMode.PercentageOfBalance)
			.SetDisplay("Target mode", "Select the metric used to measure floating profit and loss.", "Targets");

		_closeInProfit = Param(nameof(CloseInProfit), true)
			.SetDisplay("Close in profit", "Enable liquidation when floating profit reaches the target.", "Targets");

		_profitTargetPips = Param(nameof(TargetProfitInPips), 5m)
			.SetDisplay("Profit target (pips)", "Floating profit target expressed in pips.", "Targets");

		_profitTargetCurrency = Param(nameof(TargetProfitInCurrency), 50m)
			.SetDisplay("Profit target (currency)", "Floating profit per lot required before closing.", "Targets");

		_profitTargetPercent = Param(nameof(TargetProfitInPercentage), 10m)
			.SetDisplay("Profit target (%)", "Percentage of account balance required before closing.", "Targets");

		_closeInLoss = Param(nameof(CloseInLoss), true)
			.SetDisplay("Close in loss", "Enable liquidation when floating loss reaches the target.", "Targets");

		_lossTargetPips = Param(nameof(TargetLossInPips), -10m)
			.SetDisplay("Loss target (pips)", "Floating loss threshold expressed in pips.", "Targets");

		_lossTargetCurrency = Param(nameof(TargetLossInCurrency), -100m)
			.SetDisplay("Loss target (currency)", "Floating loss per lot that forces liquidation.", "Targets");

		_lossTargetPercent = Param(nameof(TargetLossInPercentage), -10m)
			.SetDisplay("Loss target (%)", "Percentage drawdown of balance that forces liquidation.", "Targets");
	}

	public ManageMode ManageBuySellOrders
	{
		get => _manageMode.Value;
		set => _manageMode.Value = value;
	}

	public bool CloseBuyOrders
	{
		get => _closeBuyOrders.Value;
		set => _closeBuyOrders.Value = value;
	}

	public bool CloseSellOrders
	{
		get => _closeSellOrders.Value;
		set => _closeSellOrders.Value = value;
	}

	public bool DeleteBuyPendingPositions
	{
		get => _cancelBuyPendings.Value;
		set => _cancelBuyPendings.Value = value;
	}

	public bool DeleteSellPendingPositions
	{
		get => _cancelSellPendings.Value;
		set => _cancelSellPendings.Value = value;
	}

	public TargetCalculationMode TypeTargetUse
	{
		get => _targetMode.Value;
		set => _targetMode.Value = value;
	}

	public bool CloseInProfit
	{
		get => _closeInProfit.Value;
		set => _closeInProfit.Value = value;
	}

	public decimal TargetProfitInPips
	{
		get => _profitTargetPips.Value;
		set => _profitTargetPips.Value = value;
	}

	public decimal TargetProfitInCurrency
	{
		get => _profitTargetCurrency.Value;
		set => _profitTargetCurrency.Value = value;
	}

	public decimal TargetProfitInPercentage
	{
		get => _profitTargetPercent.Value;
		set => _profitTargetPercent.Value = value;
	}

	public bool CloseInLoss
	{
		get => _closeInLoss.Value;
		set => _closeInLoss.Value = value;
	}

	public decimal TargetLossInPips
	{
		get => _lossTargetPips.Value;
		set => _lossTargetPips.Value = value;
	}

	public decimal TargetLossInCurrency
	{
		get => _lossTargetCurrency.Value;
		set => _lossTargetCurrency.Value = value;
	}

	public decimal TargetLossInPercentage
	{
		get => _lossTargetPercent.Value;
		set => _lossTargetPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ResetState()
	{
		_lastBid = null;
		_lastAsk = null;
		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_lastBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_lastAsk = (decimal)ask;

		EvaluateTargets();
	}

	private void EvaluateTargets()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hasPositions = _longVolume > 0m || _shortVolume > 0m;
		if (!hasPositions)
			return;

		var bid = _lastBid ?? _lastAsk;
		var ask = _lastAsk ?? _lastBid;

		if (bid is null || ask is null)
			return;

		var bidPrice = bid.Value;
		var askPrice = ask.Value;

		if (bidPrice <= 0m || askPrice <= 0m)
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		var volumeStep = Security?.VolumeStep ?? 1m;

		if (volumeStep <= 0m)
			volumeStep = 1m;

		var longLots = volumeStep > 0m ? _longVolume / volumeStep : 0m;
		var shortLots = volumeStep > 0m ? _shortVolume / volumeStep : 0m;

		var longPips = 0m;
		var shortPips = 0m;
		if (priceStep > 0m)
		{
			if (_longVolume > 0m)
				longPips = (bidPrice - _longAveragePrice) / priceStep * longLots;

			if (_shortVolume > 0m)
				shortPips = (_shortAveragePrice - askPrice) / priceStep * shortLots;
		}

		var longProfit = _longVolume > 0m ? _longVolume * (bidPrice - _longAveragePrice) : 0m;
		var shortProfit = _shortVolume > 0m ? _shortVolume * (_shortAveragePrice - askPrice) : 0m;

		var totalPips = longPips + shortPips;
		var totalProfit = longProfit + shortProfit;
		var totalVolume = _longVolume + _shortVolume;

		var balance = GetPortfolioValue();

		switch (TypeTargetUse)
		{
			case TargetCalculationMode.Pips:
				HandleTargets(longPips, shortPips, totalPips, totalVolume);
				break;

			case TargetCalculationMode.CurrencyPerLot:
				HandleCurrencyTargets(longProfit, shortProfit, totalProfit, totalVolume);
				break;

			case TargetCalculationMode.PercentageOfBalance:
				HandlePercentageTargets(longProfit, shortProfit, totalProfit, totalVolume, balance);
				break;
		}
	}

	private void HandleTargets(decimal longValue, decimal shortValue, decimal totalValue, decimal totalVolume)
	{
		if (CloseInProfit)
		{
			CheckProfit(longValue, shortValue, totalValue, totalVolume, TargetProfitInPips);
		}

		if (CloseInLoss)
		{
			CheckLoss(longValue, shortValue, totalValue, totalVolume, TargetLossInPips);
		}
	}

	private void HandleCurrencyTargets(decimal longValue, decimal shortValue, decimal totalValue, decimal totalVolume)
	{
		var profitPerLotTarget = TargetProfitInCurrency;
		var lossPerLotTarget = TargetLossInCurrency;

		if (CloseInProfit)
		{
			CheckProfit(longValue, shortValue, totalValue, totalVolume, profitPerLotTarget, true);
		}

		if (CloseInLoss)
		{
			CheckLoss(longValue, shortValue, totalValue, totalVolume, lossPerLotTarget, true);
		}
	}

	private void HandlePercentageTargets(decimal longValue, decimal shortValue, decimal totalValue, decimal totalVolume, decimal balance)
	{
		var profitPercent = TargetProfitInPercentage;
		var lossPercent = TargetLossInPercentage;

		if (CloseInProfit)
		{
			var target = balance + balance * profitPercent / 100m;
			CheckProfit(longValue, shortValue, totalValue, totalVolume, target, false, target, target);
		}

		if (CloseInLoss)
		{
			var target = balance - balance * Math.Abs(lossPercent) / 100m;
			CheckLoss(longValue, shortValue, totalValue, totalVolume, target, false, target, target);
		}
	}

	private void CheckProfit(decimal longValue, decimal shortValue, decimal totalValue, decimal totalVolume, decimal threshold, bool perLot = false, decimal? customShortThreshold = null, decimal? customTotalThreshold = null)
	{
		switch (ManageBuySellOrders)
		{
			case ManageMode.Separate:
				if (CloseBuyOrders && _longVolume > 0m)
				{
					var target = perLot ? threshold * _longVolume : threshold;
					if (longValue >= target)
					{
						if (DeleteBuyPendingPositions)
							CancelPendingOrders(Sides.Buy);
						CloseLongPositions();
					}
				}

				if (CloseSellOrders && _shortVolume > 0m)
				{
					var shortThreshold = customShortThreshold ?? threshold;
					var target = perLot ? shortThreshold * _shortVolume : shortThreshold;
					if (shortValue >= target)
					{
						if (DeleteSellPendingPositions)
							CancelPendingOrders(Sides.Sell);
						CloseShortPositions();
					}
				}
				break;

			case ManageMode.Combined:
				if (totalVolume <= 0m)
					return;

				var totalThreshold = customTotalThreshold ?? threshold;
				var targetCombined = perLot ? totalThreshold * totalVolume : totalThreshold;
				if (totalValue >= targetCombined)
				{
					if (CloseBuyOrders && DeleteBuyPendingPositions)
						CancelPendingOrders(Sides.Buy);

					if (CloseSellOrders && DeleteSellPendingPositions)
						CancelPendingOrders(Sides.Sell);

					if (CloseBuyOrders)
						CloseLongPositions();

					if (CloseSellOrders)
						CloseShortPositions();
				}
				break;
		}
	}

	private void CheckLoss(decimal longValue, decimal shortValue, decimal totalValue, decimal totalVolume, decimal threshold, bool perLot = false, decimal? customShortThreshold = null, decimal? customTotalThreshold = null)
	{
		switch (ManageBuySellOrders)
		{
			case ManageMode.Separate:
				if (CloseBuyOrders && _longVolume > 0m)
				{
					var target = perLot ? threshold * _longVolume : threshold;
					if (longValue <= target)
					{
						if (DeleteBuyPendingPositions)
							CancelPendingOrders(Sides.Buy);
						CloseLongPositions();
					}
				}

				if (CloseSellOrders && _shortVolume > 0m)
				{
					var shortThreshold = customShortThreshold ?? threshold;
					var target = perLot ? shortThreshold * _shortVolume : shortThreshold;
					if (shortValue <= target)
					{
						if (DeleteSellPendingPositions)
							CancelPendingOrders(Sides.Sell);
						CloseShortPositions();
					}
				}
				break;

			case ManageMode.Combined:
				if (totalVolume <= 0m)
					return;

				var totalThreshold = customTotalThreshold ?? threshold;
				var targetCombined = perLot ? totalThreshold * totalVolume : totalThreshold;
				if (totalValue <= targetCombined)
				{
					if (CloseBuyOrders && DeleteBuyPendingPositions)
						CancelPendingOrders(Sides.Buy);

					if (CloseSellOrders && DeleteSellPendingPositions)
						CancelPendingOrders(Sides.Sell);

					if (CloseBuyOrders)
						CloseLongPositions();

					if (CloseSellOrders)
						CloseShortPositions();
				}
				break;
		}
	}

	private void CancelPendingOrders(Sides side)
	{
		var pendingOrders = Orders
			.Where(o => o.Security == Security)
			.Where(o => o.Direction == side)
			.Where(o => o.State == OrderStates.Active)
			.Where(o => o.Type != OrderTypes.Market)
			.ToArray();

		foreach (var order in pendingOrders)
			CancelOrder(order);
	}

	private void CloseLongPositions()
	{
		var volume = _longVolume;
		if (volume > 0m)
			SellMarket(volume);
	}

	private void CloseShortPositions()
	{
		var volume = _shortVolume;
		if (volume > 0m)
			BuyMarket(volume);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0m)
			{
				var closingVolume = Math.Min(_shortVolume, volume);
				_shortVolume -= closingVolume;
				volume -= closingVolume;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _longVolume + volume;
				_longAveragePrice = newVolume == 0m ? 0m : (_longAveragePrice * _longVolume + price * volume) / newVolume;
				_longVolume = newVolume;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0m)
			{
				var closingVolume = Math.Min(_longVolume, volume);
				_longVolume -= closingVolume;
				volume -= closingVolume;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _shortVolume + volume;
				_shortAveragePrice = newVolume == 0m ? 0m : (_shortAveragePrice * _shortVolume + price * volume) / newVolume;
				_shortVolume = newVolume;
			}
		}
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
			return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
			return portfolio.BeginValue;

		return 0m;
	}
}

