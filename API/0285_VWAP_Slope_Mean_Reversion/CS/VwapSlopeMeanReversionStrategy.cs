using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP slope mean reversion strategy.
/// Trades reversions from extreme VWAP slopes and exits when the slope returns to its recent average.
/// </summary>
public class VwapSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _slopeLookback;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private VolumeWeightedMovingAverage _vwap;
	private decimal _previousVwapValue;
	private decimal[] _slopeHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// Period for slope statistics.
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
	/// Cooldown bars between orders.
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
	/// Initializes a new instance of <see cref="VwapSlopeMeanReversionStrategy"/>.
	/// </summary>
	public VwapSlopeMeanReversionStrategy()
	{
		_slopeLookback = Param(nameof(SlopeLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Lookback", "Period for slope statistics", "Slope Settings")
			.SetOptimize(10, 50, 5);

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entry threshold", "Slope Settings")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management");

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
		_vwap = null;
		_previousVwapValue = default;
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

		_vwap = new VolumeWeightedMovingAverage();
		_slopeHistory = new decimal[SlopeLookback];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_vwap, ProcessCandle)
			.Start();

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_vwap.IsFormed)
			return;

		if (!_isInitialized)
		{
			_previousVwapValue = vwapValue;
			_isInitialized = true;
			return;
		}

		if (_previousVwapValue == 0)
			return;

		var slope = vwapValue - _previousVwapValue;
		_previousVwapValue = vwapValue;

		_slopeHistory[_currentIndex] = slope;
		_currentIndex = (_currentIndex + 1) % SlopeLookback;

		if (_filledCount < SlopeLookback)
			_filledCount++;

		if (_filledCount < SlopeLookback)
			return;

		var averageSlope = 0m;
		var sumSq = 0m;

		for (var i = 0; i < SlopeLookback; i++)
			averageSlope += _slopeHistory[i];

		averageSlope /= SlopeLookback;

		for (var i = 0; i < SlopeLookback; i++)
		{
			var diff = _slopeHistory[i] - averageSlope;
			sumSq += diff * diff;
		}

		var slopeStdDev = (decimal)Math.Sqrt((double)(sumSq / SlopeLookback));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var lowerThreshold = averageSlope - ThresholdMultiplier * slopeStdDev;
		var upperThreshold = averageSlope + ThresholdMultiplier * slopeStdDev;

		if (Position == 0)
		{
			if (slope < lowerThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (slope > upperThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && slope >= averageSlope)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && slope <= averageSlope)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
