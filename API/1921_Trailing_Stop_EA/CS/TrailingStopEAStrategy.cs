using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that applies a trailing stop to an existing position.
/// It does not open new trades and only manages the current position.
/// </summary>
public class TrailingStopEAStrategy : Strategy
{
	private readonly StrategyParam<int> _trailingPoints;

	private decimal _stopPrice;
	private decimal _trailDistance;

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TrailingStopEAStrategy()
	{
		_trailingPoints = Param(nameof(TrailingPoints), 200)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Points", "Distance for the trailing stop in price steps", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0m;
		_trailDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trailDistance = TrailingPoints * (Security?.PriceStep ?? 1m);

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;

		if (Position > 0)
		{
			// Initialize trailing stop for long position
			if (_stopPrice == 0m)
			{
				_stopPrice = price - _trailDistance;
				return;
			}

			// Move stop when price advances
			var newStop = price - _trailDistance;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			// Close position if price falls below stop
			if (price <= _stopPrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
				LogInfo($"Trailing stop hit for long position at {price}");
			}
		}
		else if (Position < 0)
		{
			// Initialize trailing stop for short position
			if (_stopPrice == 0m)
			{
				_stopPrice = price + _trailDistance;
				return;
			}

			// Move stop when price declines
			var newStop = price + _trailDistance;
			if (newStop < _stopPrice)
				_stopPrice = newStop;

			// Close position if price rises above stop
			if (price >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
				LogInfo($"Trailing stop hit for short position at {price}");
			}
		}
		else
		{
			// Reset trailing stop when no position
			_stopPrice = 0m;
		}
	}
}
