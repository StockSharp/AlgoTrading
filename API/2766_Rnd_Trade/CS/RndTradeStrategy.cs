using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random direction trading strategy converted from the MetaTrader RndTrade EA.
/// The strategy closes any open position on each interval and immediately opens a new random position.
/// </summary>
public class RndTradeStrategy : Strategy
{
	private readonly StrategyParam<int> _intervalMinutes;
	private readonly Random _random = new();

	/// <summary>
	/// Interval in minutes between closing the current position and opening a new random one.
	/// </summary>
	public int IntervalMinutes
	{
		get => _intervalMinutes.Value;
		set => _intervalMinutes.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RndTradeStrategy"/> class.
	/// </summary>
	public RndTradeStrategy()
	{
		_intervalMinutes = Param(nameof(IntervalMinutes), 60)
			.SetGreaterThanZero()
			.SetDisplay("Interval Minutes", "Minutes between closing and opening positions", "General");

		Volume = 1;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, TimeSpan.FromMinutes(IntervalMinutes).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Use time-based candles as a deterministic timer replacement.
		var timeFrame = TimeSpan.FromMinutes(IntervalMinutes).TimeFrame();

		var subscription = SubscribeCandles(timeFrame);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Ensure position protection is initialized (no stop/target by default).
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only final candles to execute logic exactly once per interval.
		if (candle.State != CandleStates.Finished)
			return;

		// Skip trading until the strategy is fully ready.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Always close the existing position before selecting a new random direction.
		if (Position != 0)
			ClosePosition();

		// Determine the next position direction using the RNG.
		if (_random.Next(0, 2) == 0)
		{
			// Enter long after flattening the previous position.
			if (Position <= 0)
				BuyMarket(Volume);
		}
		else
		{
			// Enter short after flattening the previous position.
			if (Position >= 0)
				SellMarket(Volume);
		}
	}
}
