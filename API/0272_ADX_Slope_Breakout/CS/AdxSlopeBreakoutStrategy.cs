using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX slope breakout.
/// Opens positions when ADX slope deviates from its recent average and the dominant DI confirms direction.
/// </summary>
public class AdxSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _minAdx;

	private AverageDirectionalIndex _adx;
	private decimal _prevAdxValue;
	private decimal _currentSlope;
	private decimal _avgSlope;
	private decimal _stdDevSlope;
	private decimal[] _slopes;
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
	/// Minimum ADX level required for entries.
	/// </summary>
	public decimal MinAdx
	{
		get => _minAdx.Value;
		set => _minAdx.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AdxSlopeBreakoutStrategy"/>.
	/// </summary>
	public AdxSlopeBreakoutStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicator Parameters")
			.SetOptimize(10, 20, 2);

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

		_minAdx = Param(nameof(MinAdx), 25m)
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
		_prevAdxValue = default;
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

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_slopes = new decimal[SlopePeriod];
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
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
		if (dx.Plus is not decimal diPlus ||
			dx.Minus is not decimal diMinus)
			return;

		if (!_isInitialized)
		{
			_prevAdxValue = adx;
			_isInitialized = true;
			return;
		}

		_currentSlope = adx - _prevAdxValue;
		_prevAdxValue = adx;

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
		var isBullish = diPlus > diMinus;
		var isBearish = diMinus > diPlus;

		if (Position == 0)
		{
			if (_currentSlope > upperThreshold && adx >= MinAdx)
			{
				if (isBullish)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
				else if (isBearish)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
			}
		}
		else if (Position > 0)
		{
			if (_currentSlope <= _avgSlope || !isBullish)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (_currentSlope <= _avgSlope || !isBearish)
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
