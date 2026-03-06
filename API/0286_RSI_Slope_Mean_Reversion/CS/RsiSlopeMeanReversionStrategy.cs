using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI slope mean reversion strategy.
/// Trades reversions from extreme RSI slopes and exits when the slope returns to its recent average.
/// </summary>
public class RsiSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _slopeLookback;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _longRsiLevel;
	private readonly StrategyParam<decimal> _shortRsiLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal _previousRsiValue;
	private decimal[] _slopeHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

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
	/// Maximum RSI level for long entries.
	/// </summary>
	public decimal LongRsiLevel
	{
		get => _longRsiLevel.Value;
		set => _longRsiLevel.Value = value;
	}

	/// <summary>
	/// Minimum RSI level for short entries.
	/// </summary>
	public decimal ShortRsiLevel
	{
		get => _shortRsiLevel.Value;
		set => _shortRsiLevel.Value = value;
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
	/// Initializes a new instance of <see cref="RsiSlopeMeanReversionStrategy"/>.
	/// </summary>
	public RsiSlopeMeanReversionStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Relative Strength Index period", "RSI Settings")
			.SetOptimize(5, 30, 5);

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

		_longRsiLevel = Param(nameof(LongRsiLevel), 40m)
			.SetRange(1m, 100m)
			.SetDisplay("Long RSI Level", "Maximum RSI level for long entries", "Signal Filters");

		_shortRsiLevel = Param(nameof(ShortRsiLevel), 60m)
			.SetRange(1m, 100m)
			.SetDisplay("Short RSI Level", "Minimum RSI level for short entries", "Signal Filters");

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
		_rsi = null;
		_previousRsiValue = default;
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

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_slopeHistory = new decimal[SlopeLookback];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
			return;

		if (!_isInitialized)
		{
			_previousRsiValue = rsiValue;
			_isInitialized = true;
			return;
		}

		var slope = rsiValue - _previousRsiValue;
		_previousRsiValue = rsiValue;

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
			if (slope < lowerThreshold && rsiValue <= LongRsiLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (slope > upperThreshold && rsiValue >= ShortRsiLevel)
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
