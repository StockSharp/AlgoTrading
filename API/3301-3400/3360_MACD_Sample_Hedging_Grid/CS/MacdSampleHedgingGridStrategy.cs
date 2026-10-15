namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MACD Sample Hedging Grid: MACD crossover with grid-like position management.
/// </summary>
public class MacdSampleHedgingGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MacdSampleHedgingGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergence();

		decimal? prevMacd = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, (candle, macdLine) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevMacd.HasValue)
				{
					if (prevMacd.Value <= 0 && macdLine > 0 && Position <= 0)
						BuyMarket();
					else if (prevMacd.Value >= 0 && macdLine < 0 && Position >= 0)
						SellMarket();
				}

				prevMacd = macdLine;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
