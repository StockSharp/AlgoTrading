using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cycle Lines strategy: MACD crossover with EMA trend filter.
/// Buys on MACD histogram turning positive above EMA, sells on opposite.
/// </summary>
public class CycleLinesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal? _prevHistogram;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public CycleLinesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Trend EMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHistogram = null;

		var macd = new MovingAverageConvergenceDivergenceSignal();
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		var histogram = macdLine - signalLine;
		var emaVal = emaValue.ToDecimal();
		var close = candle.ClosePrice;

		if (_prevHistogram.HasValue)
		{
			// Buy: histogram crosses above zero and price above EMA
			if (_prevHistogram.Value <= 0 && histogram > 0 && close > emaVal && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: histogram crosses below zero and price below EMA
			else if (_prevHistogram.Value >= 0 && histogram < 0 && close < emaVal && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevHistogram = histogram;
	}
}
