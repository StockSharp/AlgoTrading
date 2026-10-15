using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smoothed MA directional strategy. Goes long when price is above the MA, short when below.
/// </summary>
public class SmoothedMaDirectionalStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	public SmoothedMaDirectionalStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 12)
			.SetDisplay("MA Period", "Number of bars for the moving average.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for price analysis.", "General");
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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

		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		if (closePrice > maValue && Position <= 0)
		{
			// Price above MA - go long
			if (Position < 0)
				BuyMarket(); // Close short
			BuyMarket(); // Open long
		}
		else if (closePrice < maValue && Position >= 0)
		{
			// Price below MA - go short
			if (Position > 0)
				SellMarket(); // Close long
			SellMarket(); // Open short
		}
	}
}
