using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic %K slope breakout.
/// Opens positions when Stochastic slope deviates from its recent average by a multiple of standard deviation.
/// </summary>
public class StochasticSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private StochasticOscillator _stochastic;
	private decimal _prevStochKValue;
	private decimal _currentSlope;
	private decimal _avgSlope;
	private decimal _stdDevSlope;
	private decimal[] _slopes;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// %K smoothing period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for slope statistics calculation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for breakout detection.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
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
	/// Initializes a new instance of <see cref="StochasticSlopeBreakoutStrategy"/>.
	/// </summary>
	public StochasticSlopeBreakoutStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Indicator Parameters")
			.SetOptimize(7, 21, 7);

		_kPeriod = Param(nameof(KPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Smoothing period for %K line", "Indicator Parameters")
			.SetOptimize(1, 5, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Period for %D line", "Indicator Parameters")
			.SetOptimize(1, 5, 1);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 2400)
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
		_stochastic = null;
		_prevStochKValue = default;
		_currentSlope = default;
		_avgSlope = default;
		_stdDevSlope = default;
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_isInitialized = default;
		_slopes = new decimal[LookbackPeriod];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		_slopes = new decimal[LookbackPeriod];
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_stochastic.IsFormed)
			return;

		var typedValue = (StochasticOscillatorValue)stochValue;
		if (typedValue.K is not decimal kValue)
			return;

		if (!_isInitialized)
		{
			_prevStochKValue = kValue;
			_isInitialized = true;
			return;
		}

		_currentSlope = kValue - _prevStochKValue;
		_prevStochKValue = kValue;

		_slopes[_currentIndex] = _currentSlope;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
			return;

		CalculateStatistics();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_stdDevSlope <= 0)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var upperThreshold = _avgSlope + DeviationMultiplier * _stdDevSlope;
		var lowerThreshold = _avgSlope - DeviationMultiplier * _stdDevSlope;

		if (Position == 0)
		{
			if (_currentSlope > upperThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (_currentSlope < lowerThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (_currentSlope <= _avgSlope)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (_currentSlope >= _avgSlope)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
	}

	private void CalculateStatistics()
	{
		_avgSlope = 0;
		var sumSquaredDiffs = 0m;

		for (var i = 0; i < LookbackPeriod; i++)
			_avgSlope += _slopes[i];

		_avgSlope /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _slopes[i] - _avgSlope;
			sumSquaredDiffs += diff * diff;
		}

		_stdDevSlope = (decimal)Math.Sqrt((double)(sumSquaredDiffs / LookbackPeriod));
	}
}
