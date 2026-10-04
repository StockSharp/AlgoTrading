using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Average Directional Index (ADX) trend.
/// While ADX is above 25 it holds a long position when the close is above the moving average and a short one when it is below.
/// The position closes when ADX falls below the exit threshold or the close crosses the ATR stop set at entry,
/// and reverses when the opposite setup appears.
/// </summary>
public class AdxTrendStrategy : Strategy
{
	/// <summary>
	/// ADX level above which the trend counts as established.
	/// </summary>
	public const decimal AdxEntryThreshold = 25m;

	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _adxExitThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// ADX period, also used for the ATR of the stop.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Moving Average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop distance in ATR multiples, fixed at entry.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ADX level below which the position is closed.
	/// </summary>
	public int AdxExitThreshold
	{
		get => _adxExitThreshold.Value;
		set => _adxExitThreshold.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the ADX Trend strategy.
	/// </summary>
	public AdxTrendStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for the ADX and the ATR of the stop", "Indicators")
			.SetOptimize(10, 30, 2);

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators")
			.SetOptimize(20, 100, 10);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Stop distance in entry ATR multiples", "Risk parameters")
			.SetOptimize(1, 3, 0.5m);

		_adxExitThreshold = Param(nameof(AdxExitThreshold), 20)
			.SetRange(0, 100)
			.SetDisplay("ADX Exit Threshold", "ADX level below which to exit position", "Exit parameters")
			.SetOptimize(15, 25, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = AdxPeriod };
		var ma = new SMA { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, atr, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue atrValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished || !adxValue.IsFormed || !atrValue.IsFormed || !maValue.IsFormed)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var ma = maValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();

		var trending = adx > AdxEntryThreshold;
		var longSetup = trending && close > ma;
		var shortSetup = trending && close < ma;

		if (Position > 0)
		{
			if (shortSetup)
			{
				SellMarket(Volume + Position);
				_stopPrice = close + AtrMultiplier * atr;
			}
			else if (adx < AdxExitThreshold || close <= _stopPrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (longSetup)
			{
				BuyMarket(Volume - Position);
				_stopPrice = close - AtrMultiplier * atr;
			}
			else if (adx < AdxExitThreshold || close >= _stopPrice)
			{
				BuyMarket(-Position);
				_stopPrice = 0m;
			}
		}
		else if (longSetup)
		{
			BuyMarket(Volume);
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (shortSetup)
		{
			SellMarket(Volume);
			_stopPrice = close + AtrMultiplier * atr;
		}
	}
}
