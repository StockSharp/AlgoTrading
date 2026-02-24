using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that randomly opens long or short positions based on a random threshold.
/// Mirrors the "At random" MetaTrader expert.
/// </summary>
public class AtRandomStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _randomSeed;

	private Random _random;
	private int _barCount;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RandomSeed
	{
		get => _randomSeed.Value;
		set => _randomSeed.Value = value;
	}

	public AtRandomStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that triggers random decisions", "Data");

		_randomSeed = Param(nameof(RandomSeed), 42)
			.SetDisplay("Random Seed", "Seed for the pseudo random generator (0 = system clock)", "Diagnostics");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_random = RandomSeed == 0 ? new Random() : new Random(RandomSeed);
		_barCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		// Only trade every 10th bar on average (10% chance per bar)
		if (_random.Next(0, 10) != 0)
			return;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Random direction
		var goLong = _random.Next(0, 2) == 0;

		if (goLong)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}
	}
}
