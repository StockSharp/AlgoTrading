namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// ADX trend strength with a volume breakout confirmation.
/// Enters in the direction of the dominant directional index when ADX is above <see cref="AdxThreshold"/>
/// and volume exceeds its average by <see cref="VolumeThresholdFactor"/>. The opposite signal reverses the position.
/// </summary>
public class AdxWithVolumeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _volumeThresholdFactor;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX value for a strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Period of the volume average.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Volume must exceed its average multiplied by this factor.
	/// </summary>
	public decimal VolumeThresholdFactor
	{
		get => _volumeThresholdFactor.Value;
		set => _volumeThresholdFactor.Value = value;
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
	/// Initialize <see cref="AdxWithVolumeBreakoutStrategy"/>.
	/// </summary>
	public AdxWithVolumeBreakoutStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators");

		_volumeThresholdFactor = Param(nameof(VolumeThresholdFactor), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Factor", "Volume must exceed its average by this factor", "Indicators");

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
		_volumeSma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_volumeSma = new SimpleMovingAverage { Length = VolumeAvgPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
				DrawIndicator(adxArea, adx);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The average is taken over previous bars so the current bar's volume is compared with history.
		var volumeAverage = _volumeSma.GetCurrentValue();
		var volumeFormed = _volumeSma.IsFormed;
		_volumeSma.Process(candle.TotalVolume, candle.ServerTime, true);

		if (!volumeFormed || !adxValue.IsFormed)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;

		if (adxTyped.MovingAverage is not decimal adx)
			return;

		if (adxTyped.Dx.Plus is not decimal plusDi || adxTyped.Dx.Minus is not decimal minusDi)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (adx <= AdxThreshold || candle.TotalVolume <= volumeAverage * VolumeThresholdFactor)
			return;

		if (plusDi > minusDi && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (minusDi > plusDi && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
