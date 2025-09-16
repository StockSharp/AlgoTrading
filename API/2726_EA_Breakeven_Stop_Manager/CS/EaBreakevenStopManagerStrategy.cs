// -----------------------------------------------------------------------------
// EaBreakevenStopManagerStrategy.cs
// -----------------------------------------------------------------------------
// Utility strategy that mirrors the MetaTrader 5 "eaBreakeven" expert.
// • Monitors the best bid/ask stream and existing position.
// • Moves the protective stop to breakeven plus a configurable buffer.
// • Closes the position when price retraces to the new stop level.
// -----------------------------------------------------------------------------
// Date: 9 Nov 2023
// Converted by: StockSharp strategy conversion toolkit
// -----------------------------------------------------------------------------

using System;

using StockSharp.Algo;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Break-even stop manager that replicates the MetaTrader 5 <c>eaBreakeven</c> expert.
/// The strategy does not open trades on its own; it only protects an existing position.
/// </summary>
public class EaBreakevenStopManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _breakevenPoints;
	private readonly StrategyParam<decimal> _distancePoints;
	private readonly StrategyParam<bool> _enableNotifications;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Minimum profit in points required before moving the stop to breakeven.
	/// </summary>
	public decimal BreakevenPoints
	{
		get => _breakevenPoints.Value;
		set => _breakevenPoints.Value = value;
	}

	/// <summary>
	/// Distance in points between the entry price and the breakeven stop.
	/// </summary>
	public decimal DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Whether to log a notification when the stop is moved.
	/// </summary>
	public bool EnableNotifications
	{
		get => _enableNotifications.Value;
		set => _enableNotifications.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with sensible defaults.
	/// </summary>
	public EaBreakevenStopManagerStrategy()
	{
		_breakevenPoints = Param(nameof(BreakevenPoints), 15m)
			.SetDisplay("Breakeven Points", "Profit in points required before stop adjustment", "Risk Management");

		_distancePoints = Param(nameof(DistancePoints), 5m)
			.SetDisplay("Lock-In Distance", "Points added beyond the entry price when moving the stop", "Risk Management");

		_enableNotifications = Param(nameof(EnableNotifications), true)
			.SetDisplay("Enable Notifications", "Log when the stop switches to breakeven", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_lastBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_lastAsk = (decimal)ask;

		ManageBreakeven();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
			return;
		}

		if (Position > 0)
		{
			_shortStopPrice = null;
			_longStopPrice = null;
		}
		else
		{
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}

	private void ManageBreakeven()
	{
		if (BreakevenPoints < 0m)
			return;

		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0m)
			step = 0.0001m;

		if (Position > 0)
		{
			if (_lastBid is null)
				return;

			var entryPrice = Position.AveragePrice;
			if (entryPrice <= 0m)
				return;

			var profitThreshold = BreakevenPoints * step;
			if (BreakevenPoints == 0m || _lastBid.Value - entryPrice >= profitThreshold)
			{
				var desiredStop = entryPrice + DistancePoints * step;

				if (_longStopPrice is null || desiredStop - _longStopPrice.Value >= step)
				{
					_longStopPrice = desiredStop;

					if (EnableNotifications)
						LogInfo($"Breakeven stop for long position set to {desiredStop:F4}");
				}
			}

			if (_longStopPrice.HasValue && _lastBid.Value <= _longStopPrice.Value)
			{
				SellMarket(Position);
				_longStopPrice = null;
				_shortStopPrice = null;
			}
		}
		else if (Position < 0)
		{
			if (_lastAsk is null)
				return;

			var entryPrice = Position.AveragePrice;
			if (entryPrice <= 0m)
				return;

			var profitThreshold = BreakevenPoints * step;
			if (BreakevenPoints == 0m || entryPrice - _lastAsk.Value >= profitThreshold)
			{
				var desiredStop = entryPrice - DistancePoints * step;

				if (_shortStopPrice is null || _shortStopPrice.Value - desiredStop >= step)
				{
					_shortStopPrice = desiredStop;

					if (EnableNotifications)
						LogInfo($"Breakeven stop for short position set to {desiredStop:F4}");
				}
			}

			if (_shortStopPrice.HasValue && _lastAsk.Value >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_longStopPrice = null;
				_shortStopPrice = null;
			}
		}
		else
		{
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}
}
