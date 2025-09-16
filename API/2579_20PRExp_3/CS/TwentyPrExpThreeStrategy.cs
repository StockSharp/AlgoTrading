using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 20PRExp-3 breakout strategy ported from MetaTrader 5.
/// Tracks the current day's range, waits for volume expansion, and trades breakouts beyond the high or low.
/// </summary>
public class TwentyPrExpThreeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _gapPoints;
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _volumeCandleType;

	// Daily levels that are recalculated every trading day.
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private decimal _dailyMid;
	private decimal _dailyRange;
	private DateTime _currentDay;

	// Previous candle close needed for Parabolic SAR exit condition.
	private decimal _previousClose;
	private bool _hasPreviousClose;

	// Last two 30-minute volumes for expansion filter.
	private decimal _currentVolumeBar;
	private decimal _previousVolumeBar;

	// Position management state.
	private decimal _longEntryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortEntryPrice;
	private decimal _shortStop;
	private decimal _shortTake;

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum progress in points before the trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio equity to risk per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Minimum daily channel width in points before breakouts are allowed.
	/// </summary>
	public decimal GapPoints
	{
		get => _gapPoints.Value;
		set => _gapPoints.Value = value;
	}

	/// <summary>
	/// Hour (0-23) after which new positions are allowed.
	/// </summary>
	public int SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	/// <summary>
	/// Primary candle type used for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type used for the volume filter.
	/// </summary>
	public DataType VolumeCandleType
	{
		get => _volumeCandleType.Value;
		set => _volumeCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TwentyPrExpThreeStrategy"/>.
	/// </summary>
	public TwentyPrExpThreeStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetDisplay("Take Profit (pts)", "Target distance in points", "Risk Management")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
			.SetDisplay("Trailing Stop (pts)", "Trailing stop distance", "Risk Management")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 10m)
			.SetDisplay("Trailing Step (pts)", "Minimum advance before moving trailing stop", "Risk Management")
			.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetDisplay("Risk %", "Portfolio percentage to risk per trade", "Position Sizing")
			.SetCanOptimize(true);

		_gapPoints = Param(nameof(GapPoints), 50m)
			.SetDisplay("Range Filter (pts)", "Minimum daily range in points", "Filters")
			.SetCanOptimize(true);

		_sessionStartHour = Param(nameof(SessionStartHour), 7)
			.SetDisplay("Session Start Hour", "Hour after which breakout trades are enabled", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");

		_volumeCandleType = Param(nameof(VolumeCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Volume Candle Type", "Higher timeframe for tick volume filter", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, VolumeCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_dailyHigh = 0m;
		_dailyLow = 0m;
		_dailyMid = 0m;
		_dailyRange = 0m;
		_currentDay = default;
		_previousClose = 0m;
		_hasPreviousClose = false;
		_currentVolumeBar = 0m;
		_previousVolumeBar = 0m;

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Parabolic SAR parameters mirror the original expert advisor values.
		var parabolicSar = new ParabolicSar
		{
			Acceleration = 0.005m,
			AccelerationMax = 0.01m
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(parabolicSar, ProcessMainCandle)
			.Start();

		var volumeSubscription = SubscribeCandles(VolumeCandleType);
		volumeSubscription
			.Bind(ProcessVolumeCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessVolumeCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Shift the last two finished 30-minute volumes to approximate tick volume expansion.
		_previousVolumeBar = _currentVolumeBar;
		_currentVolumeBar = candle.TotalVolume;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyLevels(candle);

		ManageOpenPosition(candle, sarValue);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousClose(candle);
			return;
		}

		if (candle.OpenTime.Hour < SessionStartHour)
		{
			UpdatePreviousClose(candle);
			return;
		}

		if (Position == 0)
		{
			var signal = GetTradeSignal(candle);

			if (signal > 0)
			{
				TryEnterLong(candle.ClosePrice);
			}
			else if (signal < 0)
			{
				TryEnterShort(candle.ClosePrice);
			}
		}

		UpdatePreviousClose(candle);
	}

	private void UpdateDailyLevels(ICandleMessage candle)
	{
		var candleDay = candle.OpenTime.Date;

		if (_currentDay != candleDay)
		{
			_currentDay = candleDay;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
		}
		else
		{
			if (candle.HighPrice > _dailyHigh)
				_dailyHigh = candle.HighPrice;

			if (_dailyLow == 0m || candle.LowPrice < _dailyLow)
				_dailyLow = candle.LowPrice;
		}

		_dailyMid = (_dailyHigh + _dailyLow) / 2m;
		_dailyRange = _dailyHigh - _dailyLow;
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal sarValue)
	{
		if (Position > 0)
		{
			// Close longs when Parabolic SAR crosses above the previous close.
			if (_hasPreviousClose && sarValue > _previousClose)
			{
				ClosePosition();
				ResetLongState();
				ResetShortState();
				return;
			}

			UpdateLongTrailing(candle);
			CheckLongTargets(candle);
		}
		else if (Position < 0)
		{
			// Close shorts when Parabolic SAR crosses below the previous close.
			if (_hasPreviousClose && sarValue < _previousClose)
			{
				ClosePosition();
				ResetLongState();
				ResetShortState();
				return;
			}

			UpdateShortTrailing(candle);
			CheckShortTargets(candle);
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || _longEntryPrice <= 0m)
			return;

		var pointValue = GetPointValue();
		var trailingDistance = TrailingStopPoints * pointValue;

		if (trailingDistance <= 0m)
			return;

		var profit = candle.ClosePrice - _longEntryPrice;

		if (profit <= trailingDistance)
			return;

		var newStop = candle.ClosePrice - trailingDistance;
		var minStep = TrailingStepPoints > 0m ? TrailingStepPoints * pointValue : 0m;

		if (_longStop > 0m && minStep > 0m && newStop - _longStop < minStep)
			return;

		_longStop = newStop;
		_longTake = TrailingStopPoints > 0m ? candle.ClosePrice + trailingDistance : _longTake;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || _shortEntryPrice <= 0m)
			return;

		var pointValue = GetPointValue();
		var trailingDistance = TrailingStopPoints * pointValue;

		if (trailingDistance <= 0m)
			return;

		var profit = _shortEntryPrice - candle.ClosePrice;

		if (profit <= trailingDistance)
			return;

		var newStop = candle.ClosePrice + trailingDistance;
		var minStep = TrailingStepPoints > 0m ? TrailingStepPoints * pointValue : 0m;

		if (_shortStop > 0m && minStep > 0m && _shortStop - newStop < minStep)
			return;

		_shortStop = newStop;
		_shortTake = TrailingStopPoints > 0m ? candle.ClosePrice - trailingDistance : _shortTake;
	}

	private void CheckLongTargets(ICandleMessage candle)
	{
		var position = Position;

		if (position <= 0m)
			return;

		if (_longStop > 0m && candle.LowPrice <= _longStop)
		{
			SellMarket(position);
			ResetLongState();
			return;
		}

		if (_longTake > 0m && candle.HighPrice >= _longTake)
		{
			SellMarket(position);
			ResetLongState();
		}
	}

	private void CheckShortTargets(ICandleMessage candle)
	{
		var position = Position;

		if (position >= 0m)
			return;

		var volume = Math.Abs(position);

		if (_shortStop > 0m && candle.HighPrice >= _shortStop)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		if (_shortTake > 0m && candle.LowPrice <= _shortTake)
		{
			BuyMarket(volume);
			ResetShortState();
		}
	}

	private int GetTradeSignal(ICandleMessage candle)
	{
		var pointValue = GetPointValue();
		var rangeThreshold = GapPoints * pointValue;
		var hasRange = _dailyRange > 0m && _dailyRange > rangeThreshold;

		var hasVolumeHistory = _previousVolumeBar > 0m && _currentVolumeBar > 0m;
		var volumeRatio = hasVolumeHistory ? _currentVolumeBar / _previousVolumeBar : 0m;

		if (!hasRange || volumeRatio <= 1.5m)
			return 0;

		if (candle.ClosePrice >= _dailyHigh && _dailyHigh > 0m)
			return 1;

		if (candle.ClosePrice <= _dailyLow && _dailyLow > 0m)
			return -1;

		return 0;
	}

	private void TryEnterLong(decimal entryPrice)
	{
		if (_dailyLow <= 0m)
			return;

		var stopPrice = _dailyLow;
		var stopDistance = entryPrice - stopPrice;

		if (stopDistance <= 0m)
			return;

		var volume = CalculatePositionSize(stopDistance);

		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_longEntryPrice = entryPrice;
		_longStop = stopPrice;
		_longTake = TakeProfitPoints > 0m ? entryPrice + TakeProfitPoints * GetPointValue() : 0m;

		ResetShortState();
	}

	private void TryEnterShort(decimal entryPrice)
	{
		if (_dailyHigh <= 0m)
			return;

		var stopPrice = _dailyHigh;
		var stopDistance = stopPrice - entryPrice;

		if (stopDistance <= 0m)
			return;

		var volume = CalculatePositionSize(stopDistance);

		if (volume <= 0m)
			return;

		SellMarket(volume);

		_shortEntryPrice = entryPrice;
		_shortStop = stopPrice;
		_shortTake = TakeProfitPoints > 0m ? entryPrice - TakeProfitPoints * GetPointValue() : 0m;

		ResetLongState();
	}

	private decimal CalculatePositionSize(decimal stopDistance)
	{
		if (stopDistance <= 0m)
			return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var riskFraction = RiskPercent / 100m;

		if (riskFraction > 0m && portfolioValue > 0m)
		{
			var riskAmount = portfolioValue * riskFraction;
			var sized = riskAmount / stopDistance;

			if (sized > 0m)
				return sized;
		}

		var fallback = Volume + Math.Abs(Position);
		return fallback > 0m ? fallback : 1m;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep;
		return step.HasValue && step.Value > 0m ? step.Value : 1m;
	}

	private void UpdatePreviousClose(ICandleMessage candle)
	{
		_previousClose = candle.ClosePrice;
		_hasPreviousClose = true;
	}

	private void ResetLongState()
	{
		_longEntryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}
}
