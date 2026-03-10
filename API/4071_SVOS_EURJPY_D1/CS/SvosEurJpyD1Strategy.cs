using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Regime-switching strategy that uses ADX to detect trend vs range.
/// In trends, follows EMA direction; in ranges, trades mean reversion.
/// </summary>
public class SvosEurJpyD1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _adxThreshold;

	private decimal _entryPrice;

	public SvosEurJpyD1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetDisplay("ADX Length", "Period for ADX.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "Period for trend EMA.", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level to distinguish trend from range.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		var atr = new AverageTrueRange { Length = AdxLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adxValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var isTrending = adxValue > AdxThreshold;

		// Position management
		if (Position > 0)
		{
			if (isTrending && close < emaValue)
			{
				SellMarket(); // Trend reversed
			}
			else if (!isTrending && close >= emaValue)
			{
				SellMarket(); // Mean reversion target hit
			}
		}
		else if (Position < 0)
		{
			if (isTrending && close > emaValue)
			{
				BuyMarket();
			}
			else if (!isTrending && close <= emaValue)
			{
				BuyMarket();
			}
		}

		// Entry
		if (Position == 0)
		{
			if (isTrending)
			{
				// Trend following
				if (close > emaValue)
				{
					_entryPrice = close;
					BuyMarket();
				}
				else if (close < emaValue)
				{
					_entryPrice = close;
					SellMarket();
				}
			}
			else
			{
				// Mean reversion
				var deviation = Math.Abs(close - emaValue);
				if (deviation > 0 && close < emaValue)
				{
					_entryPrice = close;
					BuyMarket();
				}
				else if (deviation > 0 && close > emaValue)
				{
					_entryPrice = close;
					SellMarket();
				}
			}
		}
	}
}
