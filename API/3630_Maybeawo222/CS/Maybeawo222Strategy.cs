using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with price crossing the SMA line.
/// Buys when candle opens below MA and closes above it, sells vice versa.
/// </summary>
public class Maybeawo222Strategy : Strategy
{
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public Maybeawo222Strategy()
	{
		_movingPeriod = Param(nameof(MovingPeriod), 14)
			.SetDisplay("MA Period", "Simple moving average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MovingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		// Buy signal: candle crosses MA from below to above
		var buySignal = open < maValue && close > maValue;
		// Sell signal: candle crosses MA from above to below
		var sellSignal = open > maValue && close < maValue;

		if (buySignal && Position <= 0)
		{
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket();
		}
	}
}
