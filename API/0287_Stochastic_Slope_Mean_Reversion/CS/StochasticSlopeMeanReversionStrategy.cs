using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic slope mean reversion strategy.
/// Trades reversions from extreme smoothed stochastic slopes and exits when the slope returns to its recent average.
/// </summary>
public class StochasticSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _slopeLookback;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _longStochLevel;
	private readonly StrategyParam<decimal> _shortStochLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highs;
	private decimal[] _lows;
	private int _priceIndex;
	private int _priceFilled;
	private decimal[] _kValues;
	private int _kIndex;
	private int _kFilled;
	private decimal _previousStochK;
	private decimal[] _slopeHistory;
	private int _slopeIndex;
	private int _slopeFilled;
	private int _cooldown;
	private bool _isInitialized;

	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	public int SlopeLookback
	{
		get => _slopeLookback.Value;
		set => _slopeLookback.Value = value;
	}

	public decimal ThresholdMultiplier
	{
		get => _thresholdMultiplier.Value;
		set => _thresholdMultiplier.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public decimal LongStochLevel
	{
		get => _longStochLevel.Value;
		set => _longStochLevel.Value = value;
	}

	public decimal ShortStochLevel
	{
		get => _shortStochLevel.Value;
		set => _shortStochLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public StochasticSlopeMeanReversionStrategy()
	{
		_stochKPeriod = Param(nameof(StochKPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %K Period", "Stochastic lookback period", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %D Period", "Smoothing period for stochastic %K", "Stochastic");

		_slopeLookback = Param(nameof(SlopeLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Lookback", "Period for slope statistics", "Slope");

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold Multiplier", "Std dev multiplier for entry", "Slope");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_longStochLevel = Param(nameof(LongStochLevel), 30m)
			.SetRange(1m, 100m)
			.SetDisplay("Long Stoch Level", "Maximum stochastic level for long entries", "Signal Filters");

		_shortStochLevel = Param(nameof(ShortStochLevel), 70m)
			.SetRange(1m, 100m)
			.SetDisplay("Short Stoch Level", "Minimum stochastic level for short entries", "Signal Filters");

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
		_highs = new decimal[StochKPeriod];
		_lows = new decimal[StochKPeriod];
		_priceIndex = default;
		_priceFilled = default;
		_kValues = new decimal[StochDPeriod];
		_kIndex = default;
		_kFilled = default;
		_previousStochK = default;
		_slopeHistory = new decimal[SlopeLookback];
		_slopeIndex = default;
		_slopeFilled = default;
		_cooldown = default;
		_isInitialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs = new decimal[StochKPeriod];
		_lows = new decimal[StochKPeriod];
		_kValues = new decimal[StochDPeriod];
		_slopeHistory = new decimal[SlopeLookback];
		_priceIndex = 0;
		_priceFilled = 0;
		_kIndex = 0;
		_kFilled = 0;
		_slopeIndex = 0;
		_slopeFilled = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs[_priceIndex] = candle.HighPrice;
		_lows[_priceIndex] = candle.LowPrice;
		_priceIndex = (_priceIndex + 1) % StochKPeriod;

		if (_priceFilled < StochKPeriod)
			_priceFilled++;

		if (_priceFilled < StochKPeriod)
			return;

		var highest = decimal.MinValue;
		var lowest = decimal.MaxValue;

		for (var i = 0; i < StochKPeriod; i++)
		{
			highest = Math.Max(highest, _highs[i]);
			lowest = Math.Min(lowest, _lows[i]);
		}

		var range = highest - lowest;
		if (range <= 0)
			return;

		var rawK = (candle.ClosePrice - lowest) / range * 100m;

		_kValues[_kIndex] = rawK;
		_kIndex = (_kIndex + 1) % StochDPeriod;

		if (_kFilled < StochDPeriod)
			_kFilled++;

		if (_kFilled < StochDPeriod)
			return;

		var stochK = 0m;
		for (var i = 0; i < StochDPeriod; i++)
			stochK += _kValues[i];

		stochK /= StochDPeriod;

		if (!_isInitialized)
		{
			_previousStochK = stochK;
			_isInitialized = true;
			return;
		}

		var slope = stochK - _previousStochK;
		_previousStochK = stochK;

		_slopeHistory[_slopeIndex] = slope;
		_slopeIndex = (_slopeIndex + 1) % SlopeLookback;

		if (_slopeFilled < SlopeLookback)
			_slopeFilled++;

		if (_slopeFilled < SlopeLookback)
			return;

		var avgSlope = 0m;
		var sumSq = 0m;

		for (var i = 0; i < SlopeLookback; i++)
			avgSlope += _slopeHistory[i];

		avgSlope /= SlopeLookback;

		for (var i = 0; i < SlopeLookback; i++)
		{
			var diff = _slopeHistory[i] - avgSlope;
			sumSq += diff * diff;
		}

		var stdDev = (decimal)Math.Sqrt((double)(sumSq / SlopeLookback));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var lowerThreshold = avgSlope - ThresholdMultiplier * stdDev;
		var upperThreshold = avgSlope + ThresholdMultiplier * stdDev;

		if (Position == 0)
		{
			if (slope < lowerThreshold && stochK <= LongStochLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (slope > upperThreshold && stochK >= ShortStochLevel)
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
