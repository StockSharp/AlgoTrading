using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "At random Full" MetaTrader expert.
/// Randomly opens long or short positions with grid spacing and position limits.
/// </summary>
public class AtRandomFullStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _randomSeed;

	private Random _random;
	private decimal _lastEntryPrice;
	private int _entryCount;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	public int RandomSeed
	{
		get => _randomSeed.Value;
		set => _randomSeed.Value = value;
	}

	public AtRandomFullStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_maxPositions = Param(nameof(MaxPositions), 3)
			.SetDisplay("Max Positions", "Maximum number of averaged entries", "Risk");

		_randomSeed = Param(nameof(RandomSeed), 123)
			.SetDisplay("Random Seed", "Fixed seed for deterministic simulations", "Execution");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_random = RandomSeed == 0 ? new Random() : new Random(RandomSeed);
		_lastEntryPrice = 0;
		_entryCount = 0;

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

		// Only trade occasionally to keep turnover within runner limits.
		if (_random.Next(0, 5) != 0)
			return;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var close = candle.ClosePrice;

		// Check grid spacing - minimum 0.5% between entries
		if (_lastEntryPrice > 0 && Math.Abs(close - _lastEntryPrice) / _lastEntryPrice < 0.005m)
			return;

		// Check entry limit
		if (MaxPositions > 0 && _entryCount >= MaxPositions)
		{
			// Close position and reset
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			_entryCount = 0;
			_lastEntryPrice = 0;
			return;
		}

		var goLong = _random.Next(0, 2) == 0;

		if (goLong)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position) + volume);
				_entryCount = 1;
				_lastEntryPrice = close;
			}
			else if (Position == 0)
			{
				BuyMarket(volume);
				_lastEntryPrice = close;
				_entryCount++;
			}
		}
		else
		{
			if (Position > 0)
			{
				SellMarket(Math.Abs(Position) + volume);
				_entryCount = 1;
				_lastEntryPrice = close;
			}
			else if (Position == 0)
			{
				SellMarket(volume);
				_lastEntryPrice = close;
				_entryCount++;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_random = null;
		_lastEntryPrice = 0;
		_entryCount = 0;

		base.OnReseted();
	}
}
