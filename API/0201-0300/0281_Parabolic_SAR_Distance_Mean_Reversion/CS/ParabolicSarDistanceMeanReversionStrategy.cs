using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR distance mean reversion strategy.
/// Trades large deviations of price from a locally calculated Parabolic SAR level and exits when the distance returns to its recent average.
/// </summary>
public class ParabolicSarDistanceMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accelerationFactor;
	private readonly StrategyParam<decimal> _accelerationLimit;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _distanceHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;
	private bool _isBullishTrend;
	private decimal _sarValue;
	private decimal _extremePoint;
	private decimal _acceleration;
	private decimal _previousHigh;
	private decimal _previousLow;

	/// <summary>
	/// Acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal AccelerationFactor
	{
		get => _accelerationFactor.Value;
		set => _accelerationFactor.Value = value;
	}

	/// <summary>
	/// Acceleration limit for Parabolic SAR.
	/// </summary>
	public decimal AccelerationLimit
	{
		get => _accelerationLimit.Value;
		set => _accelerationLimit.Value = value;
	}

	/// <summary>
	/// Lookback period for distance statistics.
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
	/// Initializes a new instance of <see cref="ParabolicSarDistanceMeanReversionStrategy"/>.
	/// </summary>
	public ParabolicSarDistanceMeanReversionStrategy()
	{
		_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Acceleration Factor", "Acceleration factor for Parabolic SAR", "Parabolic SAR")
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_accelerationLimit = Param(nameof(AccelerationLimit), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Acceleration Limit", "Acceleration limit for Parabolic SAR", "Parabolic SAR")
			.SetOptimize(0.1m, 0.3m, 0.05m);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Lookback period for distance statistics", "Strategy Parameters")
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
		_distanceHistory = new decimal[LookbackPeriod];
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_isInitialized = default;
		_isBullishTrend = default;
		_sarValue = default;
		_extremePoint = default;
		_acceleration = default;
		_previousHigh = default;
		_previousLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_distanceHistory = new decimal[LookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
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

		if (!_isInitialized)
		{
			InitializeState(candle);
			return;
		}

		UpdateSar(candle);

		var distance = Math.Abs(candle.ClosePrice - _sarValue);

		_distanceHistory[_currentIndex] = distance;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var avgDistance = 0m;
		var sumSq = 0m;

		for (var i = 0; i < LookbackPeriod; i++)
			avgDistance += _distanceHistory[i];

		avgDistance /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _distanceHistory[i] - avgDistance;
			sumSq += diff * diff;
		}

		var stdDistance = (decimal)Math.Sqrt((double)(sumSq / LookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var extendedThreshold = avgDistance + stdDistance * DeviationMultiplier;
		var priceAboveSar = candle.ClosePrice > _sarValue;
		var priceBelowSar = candle.ClosePrice < _sarValue;

		if (Position == 0)
		{
			if (distance > extendedThreshold)
			{
				if (priceAboveSar)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
				else if (priceBelowSar)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
			}
		}
		else if (Position > 0 && (distance <= avgDistance || priceAboveSar))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (distance <= avgDistance || priceBelowSar))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void InitializeState(ICandleMessage candle)
	{
		_isBullishTrend = candle.ClosePrice >= candle.OpenPrice;
		_sarValue = _isBullishTrend ? candle.LowPrice : candle.HighPrice;
		_extremePoint = _isBullishTrend ? candle.HighPrice : candle.LowPrice;
		_acceleration = AccelerationFactor;
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		_isInitialized = true;
	}

	private void UpdateSar(ICandleMessage candle)
	{
		_sarValue += _acceleration * (_extremePoint - _sarValue);

		if (_isBullishTrend)
		{
			_sarValue = Math.Min(_sarValue, _previousLow);

			if (candle.LowPrice <= _sarValue)
			{
				_isBullishTrend = false;
				_sarValue = _extremePoint;
				_extremePoint = candle.LowPrice;
				_acceleration = AccelerationFactor;
			}
			else if (candle.HighPrice > _extremePoint)
			{
				_extremePoint = candle.HighPrice;
				_acceleration = Math.Min(_acceleration + AccelerationFactor, AccelerationLimit);
			}
		}
		else
		{
			_sarValue = Math.Max(_sarValue, _previousHigh);

			if (candle.HighPrice >= _sarValue)
			{
				_isBullishTrend = true;
				_sarValue = _extremePoint;
				_extremePoint = candle.HighPrice;
				_acceleration = AccelerationFactor;
			}
			else if (candle.LowPrice < _extremePoint)
			{
				_extremePoint = candle.LowPrice;
				_acceleration = Math.Min(_acceleration + AccelerationFactor, AccelerationLimit);
			}
		}
	}
}
