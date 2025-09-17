using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid-style strategy translated from the MQL4 expert "exp_Amstell".
/// Scales into both directions when the market moves by a configurable distance.
/// Closes individual layers once the profit target measured in points is reached.
/// </summary>
public class ExpAmstellStrategy : Strategy
{
	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; set; }

		public decimal Volume { get; set; }

		public bool IsClosing { get; set; }
	}

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _reentryDistancePoints;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;
	private decimal _pointSize;

	/// <summary>
	/// Volume of a single market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Profit target expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimal distance from the last entry before another layer can be added.
	/// </summary>
	public int ReentryDistancePoints
	{
		get => _reentryDistancePoints.Value;
		set => _reentryDistancePoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpAmstellStrategy"/> class.
	/// </summary>
	public ExpAmstellStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for each grid leg.", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Distance in points to close profitable trades.", "Risk");

		_reentryDistancePoints = Param(nameof(ReentryDistancePoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Re-entry Distance (points)", "Minimal distance from the last entry before adding a new layer.", "Grid");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longEntries.Clear();
		_shortEntries.Clear();
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;
		_pointSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Calculate the MetaTrader-like point size once trading begins.
		_pointSize = CalculatePointSize();

		// Enable default protection to close remaining positions on stop.
		StartProtection();

		// React to best bid/ask updates exactly like the MQL tick handler.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		// Capture best bid updates.
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		// Capture best ask updates.
		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		// Run trading logic only when both sides are known.
		if (_hasBestBid && _hasBestAsk)
			ProcessPrices();
	}

	private void ProcessPrices()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var takeProfitDistance = GetTakeProfitDistance();
		if (takeProfitDistance > 0m && TryClosePositions(takeProfitDistance))
			return;

		var reentryDistance = GetReentryDistance();

		var ask = _bestAsk;
		if (CanOpenBuy(ask, reentryDistance))
		{
			BuyMarket(volume);
			return;
		}

		var bid = _bestBid;
		if (CanOpenSell(bid, reentryDistance))
			SellMarket(volume);
	}

	private bool TryClosePositions(decimal takeProfitDistance)
	{
		var bid = _bestBid;
		foreach (var entry in _longEntries)
		{
			if (entry.IsClosing)
				continue;

			// Close long layers when the bid advanced by the configured profit distance.
			if (bid - entry.Price >= takeProfitDistance)
			{
				entry.IsClosing = true;
				SellMarket(entry.Volume);
				return true;
			}
		}

		var ask = _bestAsk;
		foreach (var entry in _shortEntries)
		{
			if (entry.IsClosing)
				continue;

			// Close short layers symmetrically using the ask price.
			if (entry.Price - ask >= takeProfitDistance)
			{
				entry.IsClosing = true;
				BuyMarket(entry.Volume);
				return true;
			}
		}

		return false;
	}

	private bool CanOpenBuy(decimal ask, decimal reentryDistance)
	{
		if (_longEntries.Count == 0)
			return true;

		if (reentryDistance <= 0m)
			return true;

		var lastEntry = _longEntries[^1];
		return lastEntry.Price - ask >= reentryDistance;
	}

	private bool CanOpenSell(decimal bid, decimal reentryDistance)
	{
		if (_shortEntries.Count == 0)
			return true;

		if (reentryDistance <= 0m)
			return true;

		var lastEntry = _shortEntries[^1];
		return bid - lastEntry.Price >= reentryDistance;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Order.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			// Buy trades first offset active short layers.
			var remainder = ReduceEntries(_shortEntries, volume);

			if (remainder > 0m)
			{
				// Remaining volume becomes a new long layer.
				_longEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			// Sell trades first offset active long layers.
			var remainder = ReduceEntries(_longEntries, volume);

			if (remainder > 0m)
			{
				// Remaining volume becomes a new short layer.
				_shortEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
			}
		}

		ResetClosingFlags();
	}

	private static decimal ReduceEntries(List<PositionEntry> entries, decimal volume)
	{
		var remaining = volume;

		// Consume volume using FIFO to emulate ticket-based position accounting.
		while (remaining > 0m && entries.Count > 0)
		{
			var entry = entries[0];
			var used = Math.Min(entry.Volume, remaining);
			entry.Volume -= used;
			remaining -= used;

			if (entry.Volume <= 0m)
			{
				entries.RemoveAt(0);
			}
			else
			{
				entry.IsClosing = false;
			}
		}

		return remaining;
	}

	private void ResetClosingFlags()
	{
		for (var i = 0; i < _longEntries.Count; i++)
		{
			_longEntries[i].IsClosing = false;
		}

		for (var i = 0; i < _shortEntries.Count; i++)
		{
			_shortEntries[i].IsClosing = false;
		}
	}

	private decimal GetTakeProfitDistance()
	{
		var point = EnsurePointSize();
		return TakeProfitPoints > 0 ? TakeProfitPoints * point : 0m;
	}

	private decimal GetReentryDistance()
	{
		var point = EnsurePointSize();
		return ReentryDistancePoints > 0 ? ReentryDistancePoints * point : 0m;
	}

	private decimal EnsurePointSize()
	{
		if (_pointSize > 0m)
			return _pointSize;

		_pointSize = CalculatePointSize();
		return _pointSize;
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var scaled = step;
		var digits = 0;
		while (scaled < 1m && digits < 10)
		{
			scaled *= 10m;
			digits++;
		}

		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * adjust;
	}
}
