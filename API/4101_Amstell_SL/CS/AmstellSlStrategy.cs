using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader "exp_Amstell-SL" averaging strategy.
/// Opens both directions, layers additional orders on adverse moves, and
/// closes positions with virtual take-profit and stop-loss thresholds.
/// </summary>
public class AmstellSlStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _reentryPoints;

	private readonly List<(decimal price, decimal volume)> _buyPositions = new();
	private readonly List<(decimal price, decimal volume)> _sellPositions = new();

	private decimal? _currentBid;
	private decimal? _currentAsk;

	/// <summary>
	/// Take-profit distance expressed in points (price steps).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points (price steps).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Order volume for each newly opened position.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Distance that price must move against the last trade to add another order.
	/// </summary>
	public decimal ReentryPoints
	{
		get => _reentryPoints.Value;
		set => _reentryPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AmstellSlStrategy"/>.
	/// </summary>
	public AmstellSlStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Lot size for each order", "Trading");

		_reentryPoints = Param(nameof(ReentryPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Reentry Step", "Adverse move needed to add orders", "Trading");
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

		_buyPositions.Clear();
		_sellPositions.Clear();
		_currentBid = null;
		_currentAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Listen to best bid/ask updates to reproduce tick-based MetaTrader logic.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		// Update cached best prices from incoming level1 snapshot.
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_currentBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_currentAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var point = Security.PriceStep ?? 1m;
		var takeProfitOffset = TakeProfitPoints * point;
		var stopLossOffset = StopLossPoints * point;
		var reentryOffset = ReentryPoints * point;

		// The original expert closes positions before evaluating new entries.
		if (TryClosePositions(takeProfitOffset, stopLossOffset))
			return;

		TryOpenPositions(reentryOffset);
	}

	private bool TryClosePositions(decimal takeProfitOffset, decimal stopLossOffset)
	{
		var hasBid = _currentBid is decimal bid;
		var hasAsk = _currentAsk is decimal ask;

		if (_buyPositions.Count > 0 && hasBid && hasAsk)
		{
			for (var i = 0; i < _buyPositions.Count; i++)
			{
				var (price, volume) = _buyPositions[i];
				var profit = bid - price;
				var loss = price - ask;

				// Close long trades once either virtual target or stop is reached.
				if ((takeProfitOffset > 0m && profit >= takeProfitOffset) ||
					(stopLossOffset > 0m && loss >= stopLossOffset))
				{
					SellMarket(volume);
					_buyPositions.RemoveAt(i);
					return true;
				}
			}
		}

		if (_sellPositions.Count > 0 && hasBid && hasAsk)
		{
			for (var i = 0; i < _sellPositions.Count; i++)
			{
				var (price, volume) = _sellPositions[i];
				var profit = price - ask;
				var loss = bid - price;

				// Close short trades when their synthetic take-profit or stop-loss triggers.
				if ((takeProfitOffset > 0m && profit >= takeProfitOffset) ||
					(stopLossOffset > 0m && loss >= stopLossOffset))
				{
					BuyMarket(volume);
					_sellPositions.RemoveAt(i);
					return true;
				}
			}
		}

		return false;
	}

	private void TryOpenPositions(decimal reentryOffset)
	{
		if (Volume <= 0m)
			return;

		if (_currentAsk is decimal ask)
		{
			var shouldOpenBuy = _buyPositions.Count == 0;

			if (!shouldOpenBuy && reentryOffset > 0m)
			{
				var lastBuyPrice = _buyPositions[^1].price;
				if (lastBuyPrice - ask >= reentryOffset)
					shouldOpenBuy = true;
			}

			if (shouldOpenBuy)
			{
				// MetaTrader executes buy orders at the ask price.
				BuyMarket(Volume);
				_buyPositions.Add((ask, Volume));
			}
		}

		if (_currentBid is decimal bid)
		{
			var shouldOpenSell = _sellPositions.Count == 0;

			if (!shouldOpenSell && reentryOffset > 0m)
			{
				var lastSellPrice = _sellPositions[^1].price;
				if (bid - lastSellPrice >= reentryOffset)
					shouldOpenSell = true;
			}

			if (shouldOpenSell)
			{
				// MetaTrader executes sell orders at the bid price.
				SellMarket(Volume);
				_sellPositions.Add((bid, Volume));
			}
		}
	}
}
