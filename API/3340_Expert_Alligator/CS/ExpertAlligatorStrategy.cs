using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Expert Alligator strategy: Triple SMA (Alligator concept).
/// Buys when lips > teeth > jaw (bullish alignment).
/// Sells when lips < teeth < jaw (bearish alignment).
/// </summary>
public class ExpertAlligatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ExpertAlligatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Alligator: Jaw=13, Teeth=8, Lips=5
		var lips = new SimpleMovingAverage { Length = 5 };
		var teeth = new SimpleMovingAverage { Length = 8 };
		var jaw = new SimpleMovingAverage { Length = 13 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lips, teeth, jaw, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lips);
			DrawIndicator(area, teeth);
			DrawIndicator(area, jaw);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lips, decimal teeth, decimal jaw)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (lips > teeth && teeth > jaw && Position <= 0)
			BuyMarket();
		else if (lips < teeth && teeth < jaw && Position >= 0)
			SellMarket();
	}
}
