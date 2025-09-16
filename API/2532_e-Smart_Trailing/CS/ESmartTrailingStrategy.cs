using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop manager that mirrors the original e-Smart Trailing expert.
/// </summary>
public class ESmartTrailingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Minimum favourable move in pips before moving the stop.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ESmartTrailingStrategy"/> class.
	/// </summary>
	public ESmartTrailingStrategy()
	{
		_trailingStop = Param(nameof(TrailingStop), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Distance between price and trailing stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 5m);

		_trailingStep = Param(nameof(TrailingStep), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step (pips)", "Minimum movement before advancing the stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var tradePrice = trade.TradePrice;

		if (tradePrice is null)
			return;

		var price = tradePrice.Value;
		var pipSize = CalculateAdjustedPoint();
		var stopDistance = TrailingStop * pipSize;
		var stepDistance = TrailingStep * pipSize;

		if (stopDistance <= 0m)
			return;

		if (Position > 0)
		{
			ProcessLong(price, stopDistance, stepDistance);
		}
		else if (Position < 0)
		{
			ProcessShort(price, stopDistance, stepDistance);
		}
		else
		{
			// Reset trailing state when no position is open.
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}

	private void ProcessLong(decimal price, decimal stopDistance, decimal stepDistance)
	{
		var entryPrice = PositionAvgPrice;

		if (price >= entryPrice)
		{
			if (_longStopPrice is null)
			{
				// Initialize trailing level when profit turns non-negative.
				_longStopPrice = price - stopDistance;
			}
			else
			{
				var candidate = price - stopDistance;

				// Move the stop only when price advanced by the trailing step.
				if (candidate - _longStopPrice.Value > stepDistance)
					_longStopPrice = candidate;
			}
		}

		if (_longStopPrice.HasValue && price <= _longStopPrice.Value)
		{
			// Close the whole long position once the trailing stop is reached.
			SellMarket(Math.Abs(Position));
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}

	private void ProcessShort(decimal price, decimal stopDistance, decimal stepDistance)
	{
		var entryPrice = PositionAvgPrice;

		if (price <= entryPrice)
		{
			if (_shortStopPrice is null)
			{
				// Initialize trailing level when profit turns non-negative.
				_shortStopPrice = price + stopDistance;
			}
			else
			{
				var candidate = price + stopDistance;

				// Move the stop only when price advanced by the trailing step.
				if (_shortStopPrice.Value - candidate > stepDistance)
					_shortStopPrice = candidate;
			}
		}

		if (_shortStopPrice.HasValue && price >= _shortStopPrice.Value)
		{
			// Close the whole short position once the trailing stop is reached.
			BuyMarket(Math.Abs(Position));
			_shortStopPrice = null;
			_longStopPrice = null;
		}
	}

	private decimal CalculateAdjustedPoint()
	{
		var point = Security?.PriceStep ?? 0m;

		if (point <= 0m)
			return 1m;

		var temp = point;
		var decimals = 0;

		while (decimals < 10 && temp != Math.Floor(temp))
		{
			temp *= 10m;
			decimals++;
		}

		var adjust = decimals is 3 or 5 ? 10m : 1m;

		// Multiply the original point by 10 for 3- and 5-digit quotes to get pip size.
		return point * adjust;
	}
}
