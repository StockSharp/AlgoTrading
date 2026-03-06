using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Williams %R slope breakout.
/// Opens positions when Williams %R slope deviates from its recent average by a multiple of standard deviation.
/// </summary>
public class WilliamsRSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _centerLevel;

	private WilliamsR _williamsR;
	private decimal _prevWilliamsRValue;
	private decimal _currentSlope;
	private decimal _avgSlope;
	private decimal _stdDevSlope;
	private decimal[] _slopes;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
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
	/// Center level separating bullish and bearish zones.
	/// </summary>
	public decimal CenterLevel
	{
		get => _centerLevel.Value;
		set => _centerLevel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="WilliamsRSlopeBreakoutStrategy"/>.
	/// </summary>
	public WilliamsRSlopeBreakoutStrategy()
	{
		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicator Parameters")
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

		_centerLevel = Param(nameof(CenterLevel), -50m)
			.SetRange(-100m, 0m)
			.SetDisplay("Center Level", "Zone separator for bullish and bearish entries", "Signal Filters");

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
		_williamsR = null;
		_prevWilliamsRValue = default;
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

		_williamsR = new WilliamsR { Length = WilliamsRPeriod };
		_slopes = new decimal[SlopePeriod];
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_williamsR, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williamsR);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal williamsRValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_williamsR.IsFormed)
			return;

		if (!_isInitialized)
		{
			_prevWilliamsRValue = williamsRValue;
			_isInitialized = true;
			return;
		}

		_currentSlope = williamsRValue - _prevWilliamsRValue;
		_prevWilliamsRValue = williamsRValue;

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
			if (_currentSlope > upperThreshold && williamsRValue > CenterLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (_currentSlope < lowerThreshold && williamsRValue < CenterLevel)
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
