using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBV slope mean reversion strategy.
/// Trades reversion of extreme OBV slope values with an EMA direction filter.
/// </summary>
public class ObvSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private OnBalanceVolume _obv;
	private ExponentialMovingAverage _ema;
	private decimal _previousObvValue;
	private decimal[] _slopeHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// Lookback used to estimate slope mean and standard deviation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
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
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
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
	/// Initializes a new instance of <see cref="ObvSlopeMeanReversionStrategy"/>.
	/// </summary>
	public ObvSlopeMeanReversionStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for OBV slope statistics", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entries", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA direction filter", "Indicator Parameters");

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

		_obv = null;
		_ema = null;
		_previousObvValue = default;
		_slopeHistory = new decimal[LookbackPeriod];
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_isInitialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_obv = new OnBalanceVolume();
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_slopeHistory = new decimal[LookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_obv, _ema, ProcessObv)
			.Start();

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _obv);
			DrawOwnTrades(area);
		}
	}

	private void ProcessObv(ICandleMessage candle, decimal obvValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_obv.IsFormed || !_ema.IsFormed)
			return;

		if (!_isInitialized)
		{
			_previousObvValue = obvValue;
			_isInitialized = true;
			return;
		}

		var slope = obvValue - _previousObvValue;
		_previousObvValue = obvValue;

		_slopeHistory[_currentIndex] = slope;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
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
		var priceAboveEma = candle.ClosePrice >= emaValue;
		var priceBelowEma = candle.ClosePrice <= emaValue;

		if (Position == 0)
		{
			if (slope <= lowerThreshold && priceAboveEma)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (slope >= upperThreshold && priceBelowEma)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (slope >= averageSlope || priceBelowEma)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (slope <= averageSlope || priceAboveEma)
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

		for (var i = 0; i < LookbackPeriod; i++)
			averageSlope += _slopeHistory[i];

		averageSlope /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _slopeHistory[i] - averageSlope;
			sumSquaredDiffs += diff * diff;
		}

		slopeStdDev = (decimal)Math.Sqrt((double)(sumSquaredDiffs / LookbackPeriod));
	}
}
