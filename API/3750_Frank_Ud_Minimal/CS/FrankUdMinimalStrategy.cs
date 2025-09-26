using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Minimal port of the Frank Ud averaging expert from MetaTrader.
/// The strategy opens hedged martingale grids and liquidates both sides
/// once the newest position reaches the configured profit in pips.
/// </summary>
public class FrankUdMinimalStrategy : Strategy
{
	private const decimal ExtraTakeProfitPips = 25m;

	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _reEntryPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _minimumFreeMarginRatio;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();
	private readonly Dictionary<long, OrderAction> _orderActions = new();

	private decimal _pointValue;
	private decimal _takeProfitThreshold;
	private decimal _takeProfitDistance;
	private decimal _reEntryDistance;
	private decimal _baseVolume;
	private decimal _lastBid;
	private decimal _lastAsk;

	/// <summary>
	/// Creates a new instance of <see cref="FrankUdMinimalStrategy"/> with default parameters.
	/// </summary>
	public FrankUdMinimalStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 65m)
		.SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions.", "Risk")
		.SetGreaterThanZero();

		_reEntryPips = Param(nameof(ReEntryPips), 41m)
		.SetDisplay("Re-entry distance (pips)", "Pip distance required before adding the next grid order.", "Grid")
		.SetGreaterThanZero();

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetDisplay("Initial volume", "Base lot used for the very first order.", "Risk")
		.SetGreaterThanZero();

		_minimumFreeMarginRatio = Param(nameof(MinimumFreeMarginRatio), 0.5m)
		.SetDisplay("Free margin ratio", "Free margin must stay above Balance × Ratio before adding orders.", "Risk")
		.SetGreaterThanZero();
	}

	/// <summary>
	/// Profit threshold expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance in pips between consecutive martingale entries.
	/// </summary>
	public decimal ReEntryPips
	{
		get => _reEntryPips.Value;
		set => _reEntryPips.Value = value;
	}

	/// <summary>
	/// Base lot volume for the very first order.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Minimal free margin ratio required to send new orders.
	/// </summary>
	public decimal MinimumFreeMarginRatio
	{
		get => _minimumFreeMarginRatio.Value;
		set => _minimumFreeMarginRatio.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longEntries.Clear();
		_shortEntries.Clear();
		_orderActions.Clear();

		_pointValue = 0m;
		_takeProfitThreshold = 0m;
		_takeProfitDistance = 0m;
		_reEntryDistance = 0m;
		_baseVolume = 0m;
		_lastBid = 0m;
		_lastAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security is not assigned.");
		var priceStep = security.PriceStep ?? throw new InvalidOperationException("Security.PriceStep is unknown.");

		_pointValue = priceStep;
		_takeProfitThreshold = TakeProfitPips;
		_takeProfitDistance = (TakeProfitPips + ExtraTakeProfitPips) * _pointValue;
		_reEntryDistance = ReEntryPips * _pointValue;
		_baseVolume = AdjustVolume(InitialVolume);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidPrice))
		_lastBid = (decimal)bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askPrice))
		_lastAsk = (decimal)askPrice;

		if (_lastBid <= 0m || _lastAsk <= 0m)
		return;

		if (ShouldCloseLong())
		CloseLongPositions();

		if (ShouldCloseShort())
		CloseShortPositions();

		if (ShouldOpenLong())
		OpenLongPosition();

		if (ShouldOpenShort())
		OpenShortPosition();
	}

	private bool ShouldCloseLong()
	{
		if (_longEntries.Count == 0)
		return false;

		var entry = GetMaxVolumeEntry(_longEntries);
		if (entry == null)
		return false;

		var profitPips = (_lastBid - entry.Price) / _pointValue;
		var bufferedTarget = entry.Price + _takeProfitDistance;
		var reachedBufferedTarget = _takeProfitDistance > 0m && _lastBid >= bufferedTarget;

		return profitPips > _takeProfitThreshold || reachedBufferedTarget;
	}

	private bool ShouldCloseShort()
	{
		if (_shortEntries.Count == 0)
		return false;

		var entry = GetMaxVolumeEntry(_shortEntries);
		if (entry == null)
		return false;

		var profitPips = (entry.Price - _lastAsk) / _pointValue;
		var bufferedTarget = entry.Price - _takeProfitDistance;
		var reachedBufferedTarget = _takeProfitDistance > 0m && _lastAsk <= bufferedTarget;

		return profitPips > _takeProfitThreshold || reachedBufferedTarget;
	}

	private bool ShouldOpenLong()
	{
		if (_baseVolume <= 0m)
		return false;

		if (!HasEnoughMargin())
		return false;

		if (_longEntries.Count == 0)
		return true;

		var lowestPrice = GetExtremePrice(_longEntries, true);
		return lowestPrice - _reEntryDistance > _lastAsk;
	}

	private bool ShouldOpenShort()
	{
		if (_baseVolume <= 0m)
		return false;

		if (!HasEnoughMargin())
		return false;

		if (_shortEntries.Count == 0)
		return true;

		var highestPrice = GetExtremePrice(_shortEntries, false);
		return highestPrice + _reEntryDistance < _lastBid;
	}

	private void OpenLongPosition()
	{
		var volume = DetermineNextVolume(_longEntries);
		if (volume <= 0m)
		return;

		var order = BuyMarket(volume);
		RegisterOrder(order, OrderAction.OpenLong);
	}

	private void OpenShortPosition()
	{
		var volume = DetermineNextVolume(_shortEntries);
		if (volume <= 0m)
		return;

		var order = SellMarket(volume);
		RegisterOrder(order, OrderAction.OpenShort);
	}

	private void CloseLongPositions()
	{
		var volume = GetTotalVolume(_longEntries);
		if (volume <= 0m)
		return;

		var order = SellMarket(volume);
		RegisterOrder(order, OrderAction.CloseLong);
	}

	private void CloseShortPositions()
	{
		var volume = GetTotalVolume(_shortEntries);
		if (volume <= 0m)
		return;

		var order = BuyMarket(volume);
		RegisterOrder(order, OrderAction.CloseShort);
	}

	private void RegisterOrder(Order order, OrderAction action)
	{
		if (order == null)
		return;

		_orderActions[order.Id] = action;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (!_orderActions.TryGetValue(trade.Order.Id, out var action))
		return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		switch (action)
		{
			case OrderAction.OpenLong:
			AddEntry(_longEntries, price, volume);
			break;

			case OrderAction.OpenShort:
			AddEntry(_shortEntries, price, volume);
			break;

			case OrderAction.CloseLong:
			RemoveVolume(_longEntries, volume);
			break;

			case OrderAction.CloseShort:
			RemoveVolume(_shortEntries, volume);
			break;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_orderActions.Remove(order.Id);
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		_orderActions.Remove(order.Id);
	}

	private decimal DetermineNextVolume(List<PositionEntry> entries)
	{
		if (_baseVolume <= 0m)
		return 0m;

		var volume = entries.Count == 0
		? _baseVolume
		: GetMaxVolume(entries) * 2m;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;

		if (security?.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (security?.MinVolume is decimal min && min > 0m && volume < min)
		volume = min;

		if (security?.MaxVolume is decimal max && max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private bool HasEnoughMargin()
	{
		if (MinimumFreeMarginRatio <= 0m)
		return true;

		var portfolio = Portfolio;
		if (portfolio == null)
		return true;

		var balance = portfolio.CurrentBalance ?? portfolio.BeginBalance ?? portfolio.CurrentValue ?? 0m;
		if (balance <= 0m)
		return true;

		var blocked = portfolio.BlockedValue ?? 0m;
		var baseValue = portfolio.CurrentValue ?? portfolio.CurrentBalance ?? portfolio.BeginBalance;
		if (baseValue == null)
		return true;

		var freeMargin = baseValue.Value - blocked;
		return freeMargin > balance * MinimumFreeMarginRatio;
	}

	private static void AddEntry(List<PositionEntry> entries, decimal price, decimal volume)
	{
		if (volume <= 0m)
		return;

		entries.Add(new PositionEntry(price, volume));
	}

	private static void RemoveVolume(List<PositionEntry> entries, decimal volume)
	{
		var remaining = volume;

		for (var i = entries.Count - 1; i >= 0 && remaining > 0m; i--)
		{
			var entry = entries[i];

			if (entry.Volume <= remaining)
			{
				remaining -= entry.Volume;
				entries.RemoveAt(i);
			}
			else
			{
				entries[i] = entry.WithVolume(entry.Volume - remaining);
				remaining = 0m;
			}
		}
	}

	private static decimal GetTotalVolume(List<PositionEntry> entries)
	{
		decimal total = 0m;

		foreach (var entry in entries)
		total += entry.Volume;

		return total;
	}

	private static PositionEntry GetMaxVolumeEntry(List<PositionEntry> entries)
	{
		PositionEntry result = null;
		decimal maxVolume = 0m;

		foreach (var entry in entries)
		{
			if (entry.Volume > maxVolume)
			{
				maxVolume = entry.Volume;
				result = entry;
			}
		}

		return result;
	}

	private static decimal GetMaxVolume(List<PositionEntry> entries)
	{
		decimal maxVolume = 0m;

		foreach (var entry in entries)
		if (entry.Volume > maxVolume)
		maxVolume = entry.Volume;

		return maxVolume;
	}

	private static decimal GetExtremePrice(List<PositionEntry> entries, bool isLong)
	{
		var hasValue = false;
		decimal result = 0m;

		foreach (var entry in entries)
		{
			var price = entry.Price;

			if (!hasValue)
			{
				result = price;
				hasValue = true;
				continue;
			}

			if (isLong)
			{
				if (price < result)
				result = price;
			}
			else if (price > result)
			{
				result = price;
			}
		}

		return result;
	}

	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }

		public decimal Volume { get; }

		public PositionEntry WithVolume(decimal volume)
		{
			return new PositionEntry(Price, volume);
		}
	}

	private enum OrderAction
	{
		OpenLong,
		CloseLong,
		OpenShort,
		CloseShort
	}
}
