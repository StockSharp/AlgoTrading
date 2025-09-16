using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades in the direction of consecutive candles of the same color.
/// </summary>
public class NCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _consecutiveCandles;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private int _currentDirection;
	private int _streakLength;

	/// <summary>
	/// Number of identical candles that must appear in a row to trigger an order.
	/// </summary>
	public int ConsecutiveCandles
	{
		get => _consecutiveCandles.Value;
		set => _consecutiveCandles.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// The type of candles used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public NCandlesStrategy()
	{
		_consecutiveCandles = Param(nameof(ConsecutiveCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("Consecutive Candles", "Number of identical candles required", "General")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles to analyze", "General");
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

		_currentDirection = 0;
		_streakLength = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		var direction = 0;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			direction = 1;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			direction = -1;
		}
		else
		{
			// Doji candle breaks the streak just like in the original expert.
			_currentDirection = 0;
			_streakLength = 0;
			return;
		}

		if (direction == _currentDirection)
		{
			_streakLength = Math.Min(_streakLength + 1, ConsecutiveCandles);
		}
		else
		{
			_currentDirection = direction;
			_streakLength = 1;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_streakLength < ConsecutiveCandles)
			return;

		if (direction > 0)
		{
			BuyMarket(Volume);
		}
		else
		{
			SellMarket(Volume);
		}
	}
}
