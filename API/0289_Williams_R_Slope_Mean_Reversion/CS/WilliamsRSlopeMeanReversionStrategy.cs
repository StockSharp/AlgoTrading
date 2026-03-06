using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R slope mean reversion strategy.
/// Trades reversions from extreme Williams %R slopes and exits when the slope returns to its recent average.
/// </summary>
public class WilliamsRSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _longWilliamsLevel;
	private readonly StrategyParam<decimal> _shortWilliamsLevel;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _williamsR;
	private decimal _previousWilliamsValue;
	private decimal[] _slopeHistory;
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
	/// Period for slope statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for standard deviation to determine entry threshold.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
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
	/// Maximum Williams %R level for long entries.
	/// </summary>
	public decimal LongWilliamsLevel
	{
		get => _longWilliamsLevel.Value;
		set => _longWilliamsLevel.Value = value;
	}

	/// <summary>
	/// Minimum Williams %R level for short entries.
	/// </summary>
	public decimal ShortWilliamsLevel
	{
		get => _shortWilliamsLevel.Value;
		set => _shortWilliamsLevel.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="WilliamsRSlopeMeanReversionStrategy"/>.
	/// </summary>
	public WilliamsRSlopeMeanReversionStrategy()
	{
		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicator Parameters")
			.SetOptimize(10, 30, 2);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_longWilliamsLevel = Param(nameof(LongWilliamsLevel), -80m)
			.SetRange(-100m, 0m)
			.SetDisplay("Long Williams Level", "Maximum Williams %R level for long entries", "Signal Filters");

		_shortWilliamsLevel = Param(nameof(ShortWilliamsLevel), -20m)
			.SetRange(-100m, 0m)
			.SetDisplay("Short Williams Level", "Minimum Williams %R level for short entries", "Signal Filters");

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
		_previousWilliamsValue = default;
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

		_williamsR = new WilliamsR { Length = WilliamsRPeriod };
		_slopeHistory = new decimal[LookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
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
			_previousWilliamsValue = williamsRValue;
			_isInitialized = true;
			return;
		}

		var slope = williamsRValue - _previousWilliamsValue;
		_previousWilliamsValue = williamsRValue;

		_slopeHistory[_currentIndex] = slope;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
			return;

		var averageSlope = 0m;
		var sumSq = 0m;

		for (var i = 0; i < LookbackPeriod; i++)
			averageSlope += _slopeHistory[i];

		averageSlope /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _slopeHistory[i] - averageSlope;
			sumSq += diff * diff;
		}

		var slopeStdDev = (decimal)Math.Sqrt((double)(sumSq / LookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var lowerThreshold = averageSlope - DeviationMultiplier * slopeStdDev;
		var upperThreshold = averageSlope + DeviationMultiplier * slopeStdDev;

		if (Position == 0)
		{
			if (slope < lowerThreshold && williamsRValue <= LongWilliamsLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (slope > upperThreshold && williamsRValue >= ShortWilliamsLevel)
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
