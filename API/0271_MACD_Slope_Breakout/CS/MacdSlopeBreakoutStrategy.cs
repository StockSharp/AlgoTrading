using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD histogram slope breakout.
/// Opens positions when MACD histogram slope deviates from its recent average by a multiple of standard deviation.
/// </summary>
public class MacdSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEma;
	private readonly StrategyParam<int> _slowEma;
	private readonly StrategyParam<int> _signalMa;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private decimal _prevHistogramValue;
	private decimal _currentSlope;
	private decimal _avgSlope;
	private decimal _stdDevSlope;
	private decimal[] _slopes;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEma
	{
		get => _fastEma.Value;
		set => _fastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEma
	{
		get => _slowEma.Value;
		set => _slowEma.Value = value;
	}

	/// <summary>
	/// Signal MA period.
	/// </summary>
	public int SignalMa
	{
		get => _signalMa.Value;
		set => _signalMa.Value = value;
	}

	/// <summary>
	/// Lookback period for slope statistics calculation.
	/// </summary>
	public int SlopePeriod
	{
		get => _slopePeriod.Value;
		set => _slopePeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for breakout detection.
	/// </summary>
	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Initializes a new instance of <see cref="MacdSlopeBreakoutStrategy"/>.
	/// </summary>
	public MacdSlopeBreakoutStrategy()
	{
		_fastEma = Param(nameof(FastEma), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicator Parameters")
			.SetOptimize(8, 16, 2);

		_slowEma = Param(nameof(SlowEma), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicator Parameters")
			.SetOptimize(20, 30, 2);

		_signalMa = Param(nameof(SignalMa), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA", "Signal MA period", "Indicator Parameters")
			.SetOptimize(7, 12, 1);

		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Period for slope statistics calculation", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters")
			.SetOptimize(1.5m, 4m, 0.5m);

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
		_macd = null;
		_prevHistogramValue = default;
		_currentSlope = default;
		_avgSlope = default;
		_stdDevSlope = default;
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_isInitialized = default;
		_slopes = new decimal[SlopePeriod];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEma },
				LongMa = { Length = SlowEma },
			},
			SignalMa = { Length = SignalMa },
		};

		_slopes = new decimal[SlopePeriod];
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed)
			return;

		var typedValue = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typedValue.Macd is not decimal macd ||
			typedValue.Signal is not decimal signal)
			return;

		var histogram = macd - signal;

		if (!_isInitialized)
		{
			_prevHistogramValue = histogram;
			_isInitialized = true;
			return;
		}

		_currentSlope = histogram - _prevHistogramValue;
		_prevHistogramValue = histogram;

		_slopes[_currentIndex] = _currentSlope;
		_currentIndex = (_currentIndex + 1) % SlopePeriod;

		if (_filledCount < SlopePeriod)
			_filledCount++;

		if (_filledCount < SlopePeriod)
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

		var upperThreshold = _avgSlope + BreakoutMultiplier * _stdDevSlope;
		var lowerThreshold = _avgSlope - BreakoutMultiplier * _stdDevSlope;

		if (Position == 0)
		{
			if (_currentSlope > upperThreshold && histogram > 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (_currentSlope < lowerThreshold && histogram < 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (_currentSlope <= _avgSlope || histogram <= 0)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (_currentSlope >= _avgSlope || histogram >= 0)
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

		for (var i = 0; i < SlopePeriod; i++)
			_avgSlope += _slopes[i];

		_avgSlope /= SlopePeriod;

		for (var i = 0; i < SlopePeriod; i++)
		{
			var diff = _slopes[i] - _avgSlope;
			sumSquaredDiffs += diff * diff;
		}

		_stdDevSlope = (decimal)Math.Sqrt((double)(sumSquaredDiffs / SlopePeriod));
	}
}
