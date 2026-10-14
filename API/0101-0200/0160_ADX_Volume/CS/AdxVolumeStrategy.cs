using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX Volume strategy.
/// While ADX is above AdxThreshold, a candle with volume above the average of the previous VolumeAvgPeriod candles goes long when +DI
/// is above -DI and short when -DI is above +DI, reversing an opposite position. The position closes once ADX falls below AdxThreshold.
/// The stop lies StopLossAtr ATR from the entry close and is checked on candle closes.
/// </summary>
public class AdxVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];
	private decimal _stopPrice;

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
	/// Previous candles the volume is averaged over.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal StopLossAtr
	{
		get => _stopLossAtr.Value;
		set => _stopLossAtr.Value = value;
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
	public AdxVolumeStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level of a strong trend", "Indicators");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Average Period", "Previous candles the volume is averaged over", "Indicators");

		_stopLossAtr = Param(nameof(StopLossAtr), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk");

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
		_volumes.Clear();
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();
		_stopPrice = default;

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Volume is compared with the candles before this one.
		var average = _volumes.Count == VolumeAvgPeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumeAvgPeriod)
			_volumes.RemoveAt(0);

		if (!adxValue.IsFormed || !atrValue.IsFormed || average is not decimal avgVolume)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength } typedAdx
			|| typedAdx.Dx.Plus is not decimal plusDi || typedAdx.Dx.Minus is not decimal minusDi)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var active = strength > AdxThreshold && candle.TotalVolume > avgVolume;

		if (active && plusDi > minusDi && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - StopLossAtr * atr;
		}
		else if (active && minusDi > plusDi && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + StopLossAtr * atr;
		}
		else if (Position > 0 && (strength < AdxThreshold || (StopLossAtr > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (strength < AdxThreshold || (StopLossAtr > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
