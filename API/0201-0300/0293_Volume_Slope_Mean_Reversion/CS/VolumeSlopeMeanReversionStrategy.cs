using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume slope mean reversion strategy.
/// Trades reversion of extreme volume-ratio slope values.
/// </summary>
public class VolumeSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeMaPeriod;
	private readonly StrategyParam<int> _slopeLookback;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeAverage;
	private decimal _previousVolumeRatio;
	private decimal[] _slopeHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// Volume moving average period.
	/// </summary>
	public int VolumeMaPeriod
	{
		get => _volumeMaPeriod.Value;
		set => _volumeMaPeriod.Value = value;
	}

	/// <summary>
	/// Lookback used to estimate slope mean and standard deviation.
	/// </summary>
	public int SlopeLookback
	{
		get => _slopeLookback.Value;
		set => _slopeLookback.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for entry threshold.
	/// </summary>
	public decimal ThresholdMultiplier
	{
		get => _thresholdMultiplier.Value;
		set => _thresholdMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes a new instance of <see cref="VolumeSlopeMeanReversionStrategy"/>.
	/// </summary>
	public VolumeSlopeMeanReversionStrategy()
	{
		_volumeMaPeriod = Param(nameof(VolumeMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Period", "Period for the volume moving average", "Indicator Parameters");

		_slopeLookback = Param(nameof(SlopeLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Lookback", "Period for slope statistics", "Strategy Parameters");

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entries", "Strategy Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

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

		_volumeAverage = null;
		_previousVolumeRatio = default;
		_slopeHistory = new decimal[SlopeLookback];
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_isInitialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumeAverage = new SimpleMovingAverage
		{
			Length = VolumeMaPeriod,
			Source = Level1Fields.Volume,
		};

		_slopeHistory = new decimal[SlopeLookback];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_volumeAverage, ProcessCandle)
			.Start();

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _volumeAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal averageVolume)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_volumeAverage.IsFormed || averageVolume <= 0)
			return;

		var volumeRatio = candle.TotalVolume / averageVolume;

		if (!_isInitialized)
		{
			_previousVolumeRatio = volumeRatio;
			_isInitialized = true;
			return;
		}

		var slope = volumeRatio - _previousVolumeRatio;
		_previousVolumeRatio = volumeRatio;

		_slopeHistory[_currentIndex] = slope;
		_currentIndex = (_currentIndex + 1) % SlopeLookback;

		if (_filledCount < SlopeLookback)
			_filledCount++;

		if (_filledCount < SlopeLookback)
			return;

		CalculateStatistics(out var averageSlope, out var slopeStdDev);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (slopeStdDev <= 0)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var lowerThreshold = averageSlope - ThresholdMultiplier * slopeStdDev;
		var upperThreshold = averageSlope + ThresholdMultiplier * slopeStdDev;
		var isBullishCandle = candle.ClosePrice >= candle.OpenPrice;
		var isBearishCandle = candle.ClosePrice <= candle.OpenPrice;

		if (Position == 0)
		{
			if (slope <= lowerThreshold && isBullishCandle)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (slope >= upperThreshold && isBearishCandle)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (slope >= averageSlope || isBearishCandle)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (slope <= averageSlope || isBullishCandle)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
	}

	private void CalculateStatistics(out decimal averageSlope, out decimal slopeStdDev)
	{
		averageSlope = 0m;
		var sumSquaredDiffs = 0m;

		for (var i = 0; i < SlopeLookback; i++)
			averageSlope += _slopeHistory[i];

		averageSlope /= SlopeLookback;

		for (var i = 0; i < SlopeLookback; i++)
		{
			var diff = _slopeHistory[i] - averageSlope;
			sumSquaredDiffs += diff * diff;
		}

		slopeStdDev = (decimal)Math.Sqrt((double)(sumSquaredDiffs / SlopeLookback));
	}
}
