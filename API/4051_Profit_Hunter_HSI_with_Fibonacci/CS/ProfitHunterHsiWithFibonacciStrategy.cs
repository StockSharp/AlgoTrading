using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Profit Hunter strategy with Fibonacci retracement levels.
/// Uses EMA trend filter and enters on pullbacks to Fibonacci levels.
/// </summary>
public class ProfitHunterHsiWithFibonacciStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _rangeHigh;
	private decimal _rangeLow;
	private int _barCount;

	public ProfitHunterHsiWithFibonacciStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "Period for trend filter EMA.", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 50)
			.SetDisplay("Lookback Period", "Bars to look back for range high/low.", "Fibonacci");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rangeHigh = 0;
		_rangeLow = decimal.MaxValue;
		_barCount = 0;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		if (_barCount < LookbackPeriod)
			return;

		var range = highestValue - lowestValue;
		if (range <= 0)
			return;

		// Fibonacci levels
		var fib382 = highestValue - range * 0.382m;
		var fib618 = highestValue - range * 0.618m;
		var close = candle.ClosePrice;

		// Manage position
		if (Position > 0)
		{
			// Exit long at 0 fib (range high) or if price drops below 61.8%
			if (close >= highestValue || close < fib618)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			// Exit short at 100% fib (range low) or if price rises above 38.2%
			if (close <= lowestValue || close > fib382)
			{
				BuyMarket();
			}
		}

		// Entry logic
		if (Position == 0)
		{
			if (close > emaValue && close <= fib382 && close > fib618)
			{
				// Uptrend + pullback to Fib zone -> buy
				BuyMarket();
			}
			else if (close < emaValue && close >= fib618 && close < fib382)
			{
				// Downtrend + pullback to Fib zone -> sell
				SellMarket();
			}
		}
	}
}
