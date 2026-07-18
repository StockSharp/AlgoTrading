using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on OBV slope breakout with EMA direction filter.
/// Opens positions when OBV slope deviates from its recent average and price confirms the direction relative to EMA.
/// </summary>
public class ObvSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private OnBalanceVolume _obv;
	private ExponentialMovingAverage _ema;
	private decimal _prevObvValue;
	private decimal _currentSlope;
	private decimal _avgSlope;
	private decimal _stdDevSlope;
	private decimal[] _slopes;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

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
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss as a percentage of entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles to use in the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA period for trend confirmation.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
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
	/// Initializes a new instance of <see cref="ObvSlopeBreakoutStrategy"/>.
	/// </summary>
	public ObvSlopeBreakoutStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for calculating average and standard deviation of OBV slope", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_multiplier = Param(nameof(Multiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Std Dev Multiplier", "Multiplier for standard deviation to determine breakout threshold", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss as a percentage of entry price", "Risk Management");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA trend confirmation", "Indicator Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use in the strategy", "General");
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
		_prevObvValue = default;
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

		_obv = new OnBalanceVolume();
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_slopes = new decimal[LookbackPeriod];
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_obv, _ema, ProcessObv)
			.Start();

		StartProtection(new(), new Unit(StopLoss, UnitTypes.Percent));

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
			_prevObvValue = obvValue;
			_isInitialized = true;
			return;
		}

		_currentSlope = obvValue - _prevObvValue;
		_prevObvValue = obvValue;

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

		var upperThreshold = _avgSlope + Multiplier * _stdDevSlope;
		var lowerThreshold = _avgSlope - Multiplier * _stdDevSlope;
		var closePrice = candle.ClosePrice;
		var priceAboveEma = closePrice > emaValue;
		var priceBelowEma = closePrice < emaValue;

		if (Position == 0)
		{
			if (_currentSlope > upperThreshold && priceAboveEma)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (_currentSlope < lowerThreshold && priceBelowEma)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (_currentSlope <= _avgSlope || priceBelowEma)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (_currentSlope >= _avgSlope || priceAboveEma)
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
