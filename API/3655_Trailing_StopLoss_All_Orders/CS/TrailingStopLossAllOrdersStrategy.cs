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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop controller that mirrors the global stop management script from MetaTrader.
/// The strategy does not open new positions. It only advances a protective stop for any
/// existing long or short position once the price has travelled far enough in the profitable direction.
/// </summary>
public class TrailingStopLossAllOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailStartPips;
	private readonly StrategyParam<decimal> _trailDistancePips;

	private decimal _pipSize = 1m;
	private decimal _activationDistance;
	private decimal _trailDistance;

	private decimal? _longBestPrice;
	private decimal? _shortBestPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Profit threshold (in pips) required before the trailing stop becomes active.
	/// </summary>
	public decimal TrailStartPips
	{
		get => _trailStartPips.Value;
		set => _trailStartPips.Value = value;
	}

	/// <summary>
	/// Distance (in pips) maintained between the best price and the trailing stop.
	/// </summary>
	public decimal TrailDistancePips
	{
		get => _trailDistancePips.Value;
		set => _trailDistancePips.Value = value;
	}

	/// <summary>
	/// Initializes the trailing stop manager.
	/// </summary>
	public TrailingStopLossAllOrdersStrategy()
	{
		_trailStartPips = Param(nameof(TrailStartPips), 20m)
			.SetNotNegative()
			.SetDisplay("Trail Start (pips)", "Profit required before trailing activates.", "Risk management")
			.SetCanOptimize(true);

		_trailDistancePips = Param(nameof(TrailDistancePips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Distance (pips)", "Gap between price and trailing stop.", "Risk management")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 1m;
		if (_pipSize <= 0m)
			_pipSize = 1m;

			RecalculateDistances();

		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
			ResetState();
	}

		private void ProcessTrade(ITickTradeMessage trade)
		{
			var price = trade.Price;

			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			var lastPrice = price;

			RecalculateDistances();

			if (Position > 0 && PositionPrice is decimal entryPrice)
		{
			UpdateLongTrailing(entryPrice, lastPrice);
		}
			else if (Position < 0 && PositionPrice is decimal entryPriceShort)
		{
			UpdateShortTrailing(entryPriceShort, lastPrice);
		}
			else
			{
				ResetState();
		}
	}

	private void UpdateLongTrailing(decimal entryPrice, decimal lastPrice)
	{
		if (_trailDistance <= 0m)
			return;

		var best = _longBestPrice is decimal value ? Math.Max(value, lastPrice) : lastPrice;
		_longBestPrice = best;

		if (best - entryPrice < _activationDistance)
			return;

		var desiredStop = best - _trailDistance;
		if (desiredStop < entryPrice)
			desiredStop = entryPrice;

		if (_longStopPrice is not decimal current || desiredStop > current)
			_longStopPrice = desiredStop;

		if (lastPrice <= _longStopPrice)
		{
			SellMarket(Position);
			ResetState();
		}
	}

	private void UpdateShortTrailing(decimal entryPrice, decimal lastPrice)
	{
		if (_trailDistance <= 0m)
			return;

		var best = _shortBestPrice is decimal value ? Math.Min(value, lastPrice) : lastPrice;
		_shortBestPrice = best;

		if (entryPrice - best < _activationDistance)
			return;

		var desiredStop = best + _trailDistance;
		if (desiredStop > entryPrice)
			desiredStop = entryPrice;

		if (_shortStopPrice is not decimal current || desiredStop < current)
			_shortStopPrice = desiredStop;

		if (lastPrice >= _shortStopPrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetState();
		}
	}

	private void RecalculateDistances()
	{
		_activationDistance = TrailStartPips * _pipSize;
		_trailDistance = TrailDistancePips * _pipSize;
	}

	private void ResetState()
	{
		_longBestPrice = null;
		_shortBestPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}
}

