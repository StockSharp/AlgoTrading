using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull moving average slope mean reversion strategy.
/// Trades reversions from extreme Hull MA slopes and exits when the slope returns to its recent average.
/// </summary>
public class HullMaSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hullMa;
	private decimal _prevHullValue;
	private decimal[] _slopeHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HullPeriod
	{
		get => _hullPeriod.Value;
		set => _hullPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for slope statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for mean reversion detection.
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HullMaSlopeMeanReversionStrategy"/>.
	/// </summary>
	public HullMaSlopeMeanReversionStrategy()
	{
		_hullPeriod = Param(nameof(HullPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Hull MA Period", "Hull Moving Average period", "Hull MA")
			.SetOptimize(5, 20, 1);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Lookback period for slope statistics", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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
		_hullMa = null;
		_prevHullValue = default;
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

		_hullMa = new HullMovingAverage { Length = HullPeriod };
		_slopeHistory = new decimal[LookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_hullMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hullMa);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal hullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hullMa.IsFormed)
			return;

		if (!_isInitialized)
		{
			_prevHullValue = hullValue;
			_isInitialized = true;
			return;
		}

		if (_prevHullValue == 0)
			return;

		var slope = (hullValue - _prevHullValue) / _prevHullValue * 100m;
		_prevHullValue = hullValue;

		_slopeHistory[_currentIndex] = slope;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
			return;

		var avgSlope = 0m;
		var sumSq = 0m;

		for (var i = 0; i < LookbackPeriod; i++)
			avgSlope += _slopeHistory[i];

		avgSlope /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _slopeHistory[i] - avgSlope;
			sumSq += diff * diff;
		}

		var stdSlope = (decimal)Math.Sqrt((double)(sumSq / LookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var highThreshold = avgSlope + stdSlope * DeviationMultiplier;
		var lowThreshold = avgSlope - stdSlope * DeviationMultiplier;

		if (Position == 0)
		{
			if (slope < lowThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (slope > highThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && slope >= avgSlope)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && slope <= avgSlope)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
