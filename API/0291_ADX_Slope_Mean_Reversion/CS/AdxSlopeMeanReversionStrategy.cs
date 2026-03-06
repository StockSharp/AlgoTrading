using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX slope mean reversion strategy.
/// Trades reversion of extreme ADX slope moves once the recent slope distribution is formed.
/// </summary>
public class AdxSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _slopeLookback;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _minAdx;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private decimal _previousAdxValue;
	private decimal[] _slopeHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
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
	/// Minimum ADX level required for entries.
	/// </summary>
	public decimal MinAdx
	{
		get => _minAdx.Value;
		set => _minAdx.Value = value;
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
	/// Initializes a new instance of <see cref="AdxSlopeMeanReversionStrategy"/>.
	/// </summary>
	public AdxSlopeMeanReversionStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicator Parameters")
			.SetOptimize(10, 20, 2);

		_slopeLookback = Param(nameof(SlopeLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Lookback", "Period for slope statistics", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entries", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_minAdx = Param(nameof(MinAdx), 18m)
			.SetGreaterThanZero()
			.SetDisplay("Min ADX", "Minimum ADX level required for entries", "Signal Filters");

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
		_previousAdxValue = default;
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

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_slopeHistory = new decimal[SlopeLookback];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_adx.IsFormed)
			return;

		var typedValue = (AverageDirectionalIndexValue)adxValue;

		if (typedValue.MovingAverage is not decimal adx)
			return;

		var dx = typedValue.Dx;

		if (dx.Plus is not decimal diPlus || dx.Minus is not decimal diMinus)
			return;

		if (!_isInitialized)
		{
			_previousAdxValue = adx;
			_isInitialized = true;
			return;
		}

		var slope = adx - _previousAdxValue;
		_previousAdxValue = adx;

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
		var isBullish = diPlus >= diMinus;
		var isBearish = diMinus > diPlus;

		if (Position == 0)
		{
			if (adx >= MinAdx && slope <= lowerThreshold && isBullish)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (adx >= MinAdx && slope >= upperThreshold && isBearish)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (slope >= averageSlope || !isBullish)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (slope <= averageSlope || !isBearish)
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
