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
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
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
		_adxPeriod = Param(nameof(AdxPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 15)
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

		_volumeSma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_volumeSma = new SimpleMovingAverage { Length = VolumeAvgPeriod };
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(adx, ProcessCandle).Start();

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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		// Process volume average
		var volumeAvgValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.ServerTime));

		var adxTyped = (AverageDirectionalIndexValue)adxValue;

		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var dx = adxTyped.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
			return;

		var volumeAverage = volumeAvgValue.IsFormed ? volumeAvgValue.ToDecimal() : 0m;
		var isStrongTrend = adx > AdxThreshold;
		var isVolumeBreakout = volumeAverage <= 0m || candle.TotalVolume >= volumeAverage;
		var isBullish = plusDi > minusDi;
		var isBearish = minusDi > plusDi;

		if (_cooldownRemaining > 0)
			return;

		if (!isStrongTrend || !isVolumeBreakout)
			return;

		if (Position == 0)
		{
			if (isBullish)
			{
				BuyMarket();
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (isBearish)
			{
				SellMarket();
				_cooldownRemaining = SignalCooldownBars;
			}
		}
	}
}
