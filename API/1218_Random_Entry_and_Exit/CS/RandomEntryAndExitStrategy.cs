using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that randomly enters and exits positions using predefined probability thresholds.
/// </summary>
public class RandomEntryAndExitStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<int> _randomSeed;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private Random _entryRandom;
	private Random _exitRandom;

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Seed for random generator.
	/// </summary>
	public int RandomSeed
	{
		get => _randomSeed.Value;
		set => _randomSeed.Value = value;
	}

	/// <summary>
	/// Probability threshold for entries.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Probability threshold for exits.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomEntryAndExitStrategy"/> class.
	/// </summary>
	public RandomEntryAndExitStrategy()
	{
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), false)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_randomSeed = Param(nameof(RandomSeed), 1)
			.SetDisplay("Random Seed", "Seed for random generator", "Parameters");

		_entryThreshold = Param(nameof(EntryThreshold), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Probability threshold for entries", "Parameters");

		_exitThreshold = Param(nameof(ExitThreshold), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Threshold", "Probability threshold for exits", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryRandom = null;
		_exitRandom = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_entryRandom = new Random(RandomSeed);
		_exitRandom = new Random(RandomSeed + 1);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var entryProb = (decimal)_entryRandom.NextDouble();
		var exitProb = (decimal)_exitRandom.NextDouble();

		var enter = entryProb < EntryThreshold;
		var exit = exitProb < ExitThreshold;

		if (EnableLong)
		{
			if (enter && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			if (exit && Position > 0)
				SellMarket(Math.Abs(Position));
		}

		if (EnableShort)
		{
			if (enter && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			if (exit && Position < 0)
				BuyMarket(Math.Abs(Position));
		}
	}
}
