using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range breakout strategy that uses Highest/Lowest channel.
/// Enters on breakouts above/below the channel and exits on reversion.
/// </summary>
public class PipsoNightBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _breakoutPeriod;

	private decimal _entryPrice;
	private decimal _prevHighest;
	private decimal _prevLowest;
	private bool _hasPrev;

	public PipsoNightBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_breakoutPeriod = Param(nameof(BreakoutPeriod), 36)
			.SetDisplay("Breakout Period", "Period for Highest/Lowest channel.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BreakoutPeriod
	{
		get => _breakoutPeriod.Value;
		set => _breakoutPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevHighest = 0;
		_prevLowest = 0;
		_hasPrev = false;

		var highest = new Highest { Length = BreakoutPeriod };
		var lowest = new Lowest { Length = BreakoutPeriod };
		var ema = new ExponentialMovingAverage { Length = BreakoutPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var mid = (highestValue + lowestValue) / 2m;

		// Exit conditions
		if (Position > 0)
		{
			// Exit when price reverts to middle or stop-loss
			if (close < mid || (_entryPrice > 0 && close < _entryPrice * 0.98m))
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (close > mid || (_entryPrice > 0 && close > _entryPrice * 1.02m))
			{
				BuyMarket();
			}
		}

		// Entry conditions: breakout above previous highest or below previous lowest
		if (Position == 0 && _hasPrev)
		{
			if (close > _prevHighest && close > emaValue)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close < _prevLowest && close < emaValue)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
		_hasPrev = true;
	}
}
