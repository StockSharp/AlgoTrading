using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Test MACD strategy: MACD histogram zero-cross.
/// Buys when MACD histogram crosses above zero.
/// Sells when MACD histogram crosses below zero.
/// </summary>
public class TestMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TestMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = 12 }, LongMa = { Length = 26 } },
			SignalMa = { Length = 9 }
		};

		decimal? prevHistogram = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, (candle, macdVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (macdVal.IsEmpty)
					return;

				var v = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
				if (v.Macd is not decimal macdDec || v.Signal is not decimal signalDec)
					return;

				var histogram = macdDec - signalDec;

				if (prevHistogram.HasValue)
				{
					if (prevHistogram.Value <= 0m && histogram > 0m && Position <= 0)
						BuyMarket();
					else if (prevHistogram.Value >= 0m && histogram < 0m && Position >= 0)
						SellMarket();
				}

				prevHistogram = histogram;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}
}
