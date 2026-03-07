namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on ADX with a volume breakout confirmation.
/// </summary>
public class AdxWithVolumeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _volumeThresholdFactor;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private SimpleMovingAverage _volumeSma;
	private StandardDeviation _volumeStdDev;
	private int _cooldownRemaining;

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	public decimal VolumeThresholdFactor
	{
		get => _volumeThresholdFactor.Value;
		set => _volumeThresholdFactor.Value = value;
	}

	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AdxWithVolumeBreakoutStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 10m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators");

		_volumeThresholdFactor = Param(nameof(VolumeThresholdFactor), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Threshold Factor", "Factor for volume breakout detection", "Indicators");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 48)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between signals", "Trading");

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

		_adx = null;
		_volumeSma = null;
		_volumeStdDev = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_volumeSma = new SMA { Length = VolumeAvgPeriod };
		_volumeStdDev = new StandardDeviation { Length = VolumeAvgPeriod };
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var adxValue = _adx.Process(candle);
		var volumeAvgValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.ServerTime));
		var volumeStdValue = _volumeStdDev.Process(new DecimalIndicatorValue(_volumeStdDev, candle.TotalVolume, candle.ServerTime));

		if (!adxValue.IsFormed || !volumeAvgValue.IsFormed || !volumeStdValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;

		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var volumeAverage = volumeAvgValue.ToDecimal();
		var volumeStdDev = volumeStdValue.ToDecimal();
		var volumeThreshold = Math.Max(volumeAverage * VolumeThresholdFactor, volumeAverage + volumeStdDev * 0.5m);
		var isStrongTrend = adx > AdxThreshold;
		var isVolumeBreakout = volumeAverage <= 0m || candle.TotalVolume >= volumeThreshold;
		var isBullishBreakout = candle.ClosePrice > candle.OpenPrice;
		var isBearishBreakout = candle.ClosePrice < candle.OpenPrice;

		if (Position > 0 && (adx < 8m || isBearishBreakout))
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
			return;
		}

		if (Position < 0 && (adx < 8m || isBullishBreakout))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
			return;
		}

		if (_cooldownRemaining > 0 || !isStrongTrend || !isVolumeBreakout)
			return;

		if (isBullishBreakout && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (isBearishBreakout && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldownRemaining = SignalCooldownBars;
		}
	}
}
