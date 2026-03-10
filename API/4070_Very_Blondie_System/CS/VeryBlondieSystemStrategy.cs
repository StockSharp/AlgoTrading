using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion strategy that trades when price deviates from a range channel.
/// Buys at lower channel, sells at upper channel, exits at channel midpoint.
/// </summary>
public class VeryBlondieSystemStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _periodLength;

	public VeryBlondieSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_periodLength = Param(nameof(PeriodLength), 30)
			.SetDisplay("Period Length", "Period for Highest/Lowest channel.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int PeriodLength
	{
		get => _periodLength.Value;
		set => _periodLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = PeriodLength };
		var lowest = new Lowest { Length = PeriodLength };
		var sma = new SimpleMovingAverage { Length = PeriodLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var range = highestValue - lowestValue;
		if (range <= 0)
			return;

		// Percentage position within channel
		var position = (close - lowestValue) / range;

		// Exit conditions: revert to mean
		if (Position > 0 && close >= smaValue)
		{
			SellMarket();
		}
		else if (Position < 0 && close <= smaValue)
		{
			BuyMarket();
		}

		// Entry: at channel extremes
		if (Position == 0)
		{
			if (position < 0.15m)
			{
				// Near lower channel - buy for mean reversion
				BuyMarket();
			}
			else if (position > 0.85m)
			{
				// Near upper channel - sell for mean reversion
				SellMarket();
			}
		}
	}
}
