using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger ADX strategy.
/// A close below the lower band while ADX is above AdxThreshold goes long and a close above the upper band goes short, reversing
/// an opposite position. The position closes when price reverts to the middle band. The stop lies AtrMultiplier ATR from the entry close
/// and is checked on candle closes.
/// </summary>
public class BollingerAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// Period of the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier of the bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Period of ADX.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX level of a strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the stop ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
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
	/// Constructor.
	/// </summary>
	public BollingerAdxStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier of the bands", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level of a strong trend", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the stop ATR", "Risk");

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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = default;

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, adx, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue adxValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !adxValue.IsFormed || !atrValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower || bands.MovingAverage is not decimal middle)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var strong = strength > AdxThreshold;

		if (close < lower && strong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (close > upper && strong && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + AtrMultiplier * atr;
		}
		else if (Position > 0 && (close >= middle || (AtrMultiplier > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close <= middle || (AtrMultiplier > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
