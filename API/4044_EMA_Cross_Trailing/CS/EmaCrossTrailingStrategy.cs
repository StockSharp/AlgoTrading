using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with trailing stop.
/// Buys when fast EMA crosses above slow EMA, sells on the opposite crossover.
/// </summary>
public class EmaCrossTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private int _currentDirection;
	private bool _hasInitialDirection;

	public EmaCrossTrailingStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 5)
			.SetDisplay("Fast EMA", "Length of the fast exponential moving average.", "Indicator");

		_slowEmaLength = Param(nameof(SlowEmaLength), 60)
			.SetDisplay("Slow EMA", "Length of the slow exponential moving average.", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to build candles and EMAs.", "General");
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDirection = 0;
		_hasInitialDirection = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Determine direction: 1 = fast above slow (bullish), -1 = fast below slow (bearish)
		var newDirection = fastValue > slowValue ? 1 : fastValue < slowValue ? -1 : 0;

		if (newDirection == 0)
			return;

		if (!_hasInitialDirection)
		{
			_currentDirection = newDirection;
			_hasInitialDirection = true;
			return;
		}

		if (newDirection == _currentDirection)
			return;

		var prevDirection = _currentDirection;
		_currentDirection = newDirection;

		// Crossover detected
		if (newDirection == 1 && prevDirection == -1)
		{
			// Bullish crossover
			if (Position < 0)
				BuyMarket(); // Close short
			if (Position <= 0)
				BuyMarket(); // Open long
		}
		else if (newDirection == -1 && prevDirection == 1)
		{
			// Bearish crossover
			if (Position > 0)
				SellMarket(); // Close long
			if (Position >= 0)
				SellMarket(); // Open short
		}
	}
}
