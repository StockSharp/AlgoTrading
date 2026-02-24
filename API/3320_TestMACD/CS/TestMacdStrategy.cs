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

	private decimal? _prevHistogram;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TestMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHistogram = null;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = 12 }, LongMa = { Length = 26 } },
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (macdVal.IsEmpty)
			return;

		var v = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		var histogram = v.Macd - v.Signal;

		if (_prevHistogram.HasValue)
		{
			if (_prevHistogram.Value <= 0m && histogram > 0m && Position <= 0)
			{
				BuyMarket();
			}
			else if (_prevHistogram.Value >= 0m && histogram < 0m && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevHistogram = histogram;
	}
}
