using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
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

	private SimpleMovingAverage _volumeSma;

	/// <summary>
	/// Candle type used for strategy calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }

	/// <summary>
	/// ADX threshold to confirm trend strength.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Volume multiplier applied to moving average.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// Period for volume moving average.
	/// </summary>
	public int VolumePeriod { get => _volumePeriod.Value; set => _volumePeriod.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="AdxVolumeMultiplierStrategy"/>.
	/// </summary>
	public AdxVolumeMultiplierStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX", "ADX");

		_adxThreshold = Param(nameof(AdxThreshold), 26m)
			.SetDisplay("ADX Threshold", "Trend strength threshold", "ADX");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.8m)
			.SetDisplay("Volume Multiplier", "Multiplier for average volume", "Volume");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Period for volume SMA", "Volume");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_volumeSma = new SimpleMovingAverage { Length = VolumePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volumeValue = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true);
		if (!volumeValue.IsFinal)
			return;
		var avgVolume = volumeValue.ToDecimal();

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx ||
			adxTyped.Dx.Plus is not decimal diPlus ||
			adxTyped.Dx.Minus is not decimal diMinus)
			return;

		var volumeRequirement = avgVolume * VolumeMultiplier;
		var longCondition = adx > AdxThreshold && diPlus > diMinus && candle.TotalVolume > volumeRequirement;
		var shortCondition = adx > AdxThreshold && diMinus > diPlus && candle.TotalVolume > volumeRequirement;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}
}
