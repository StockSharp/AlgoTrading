using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA rainbow with MACD and ADX filters.
/// Opens long when MACD signal is positive, EMAs are ascending and ADX is above threshold.
/// Opens short when MACD signal is negative, EMAs are descending and ADX is above threshold.
/// </summary>
public class MagnaRapaxCopperStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MagnaRapaxCopperStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "Minimum ADX value", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		var ema5 = new ExponentialMovingAverage { Length = 5 };
		var ema13 = new ExponentialMovingAverage { Length = 13 };
		var ema34 = new ExponentialMovingAverage { Length = 34 };
		var ema89 = new ExponentialMovingAverage { Length = 89 };

		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = 5;
		macd.Macd.LongMa.Length = 35;
		macd.SignalMa.Length = 5;

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(new IIndicator[] { ema5, ema13, ema34, ema89, macd, adx }, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var e5 = values[0].IsEmpty ? (decimal?)null : values[0].GetValue<decimal>();
		var e13 = values[1].IsEmpty ? (decimal?)null : values[1].GetValue<decimal>();
		var e34 = values[2].IsEmpty ? (decimal?)null : values[2].GetValue<decimal>();
		var e89 = values[3].IsEmpty ? (decimal?)null : values[3].GetValue<decimal>();

		if (e5 is null || e13 is null || e34 is null || e89 is null)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)values[4];
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		var adxTyped = (AverageDirectionalIndexValue)values[5];
		if (adxTyped.MovingAverage is not decimal adxVal)
			return;

		var price = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		var ascending = e5.Value > e13.Value && e13.Value > e34.Value && e34.Value > e89.Value;
		var descending = e5.Value < e13.Value && e13.Value < e34.Value && e34.Value < e89.Value;

		// Entry
		if (Position == 0 && adxVal > AdxThreshold)
		{
			if (ascending && macdLine > signalLine)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (descending && macdLine < signalLine)
			{
				SellMarket();
				_entryPrice = price;
			}
		}
	}
}
