using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens positions after detecting N identical candles in a row.
/// Enters in the direction of the candle streak.
/// </summary>
public class NCandlesSequenceStrategy : Strategy
{
	private readonly StrategyParam<int> _consecutiveCandles;
	private readonly StrategyParam<DataType> _candleType;

	private int _consecutiveDirection;
	private int _consecutiveCount;

	/// <summary>
	/// Number of identical candles required before entering a trade.
	/// </summary>
	public int ConsecutiveCandles
	{
		get => _consecutiveCandles.Value;
		set => _consecutiveCandles.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public NCandlesSequenceStrategy()
	{
		_consecutiveCandles = Param(nameof(ConsecutiveCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("Consecutive Candles", "Number of identical candles in a row", "Entry")
			.SetOptimize(2, 6, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_consecutiveDirection = 0;
		_consecutiveCount = 0;

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

		var direction = GetCandleDirection(candle);

		if (direction == 0)
		{
			_consecutiveDirection = 0;
			_consecutiveCount = 0;
			return;
		}

		if (direction == _consecutiveDirection)
		{
			_consecutiveCount++;
		}
		else
		{
			_consecutiveDirection = direction;
			_consecutiveCount = 1;
		}

		if (_consecutiveCount < ConsecutiveCandles)
			return;

		if (direction > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (direction < 0 && Position >= 0)
		{
			SellMarket();
		}
	}

	private static int GetCandleDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
			return 1;
		if (candle.ClosePrice < candle.OpenPrice)
			return -1;
		return 0;
	}
}
