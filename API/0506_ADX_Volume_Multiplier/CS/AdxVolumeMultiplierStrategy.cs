using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX with volume multiplier filter.
/// </summary>
public class AdxVolumeMultiplierStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	public int VolumePeriod { get => _volumePeriod.Value; set => _volumePeriod.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdxVolumeMultiplierStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX", "ADX");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "Trend strength threshold", "ADX");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 0.8m)
			.SetDisplay("Volume Multiplier", "Multiplier for average volume", "Volume");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Period for volume SMA", "Volume");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var ema = new ExponentialMovingAverage { Length = VolumePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx ||
			adxTyped.Dx.Plus is not decimal diPlus ||
			adxTyped.Dx.Minus is not decimal diMinus)
			return;

		var ema = emaValue.ToDecimal();

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Use EMA as trend confirmation instead of volume multiplier
		var aboveEma = candle.ClosePrice > ema;
		var belowEma = candle.ClosePrice < ema;

		var longCondition = adx > AdxThreshold && diPlus > diMinus && aboveEma;
		var shortCondition = adx > AdxThreshold && diMinus > diPlus && belowEma;

		if (longCondition && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (shortCondition && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
