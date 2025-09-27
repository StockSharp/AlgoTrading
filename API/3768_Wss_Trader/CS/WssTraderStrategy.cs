
using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Wss_trader" MetaTrader strategy built around Camarilla and classic pivot levels.
/// Reproduces the time-filtered breakout entries, fixed targets, and optional trailing stop.
/// </summary>
public class WssTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _metricPoints;
	private readonly StrategyParam<int> _trailingPoints;
	private readonly StrategyParam<decimal> _orderVolume;

	private ICandleMessage _previousDailyCandle;
	private decimal _priceStep;

	private decimal _longEntryLevel;
	private decimal _shortEntryLevel;
	private decimal _longStopLevel;
	private decimal _shortStopLevel;
	private decimal _longTargetLevel;
	private decimal _shortTargetLevel;

	private decimal _previousClose;
	private bool _hasPreviousClose;
	private bool _levelsReady;
	private bool _canTrade;
	private DateTimeOffset? _lastCandleOpenTime;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal _longTarget;
	private decimal _shortTarget;

	/// <summary>
	/// Initializes a new instance of the <see cref="WssTraderStrategy"/> class.
	/// </summary>
	public WssTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Working Candle", "Primary candle type for trading logic.", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle", "Daily candle type used for pivot calculation.", "General");

		_startHour = Param(nameof(StartHour), 8)
			.SetDisplay("Start Hour", "Hour of day when trading becomes active (0-23).", "Session");

		_endHour = Param(nameof(EndHour), 16)
			.SetDisplay("End Hour", "Hour of day after which trading is disabled (0-23).", "Session");

		_metricPoints = Param(nameof(MetricPoints), 20)
			.SetGreaterThanZero()
			.SetDisplay("Metric Points", "Distance from the pivot to entry levels expressed in price steps.", "Levels");

		_trailingPoints = Param(nameof(TrailingPoints), 20)
			.SetDisplay("Trailing Points", "Trailing stop offset in price steps (0 disables trailing).", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Order volume replicated from the original lots parameter.", "Orders");
	}

	/// <summary>
	/// Candle type used for the trading calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Daily candle type responsible for pivot extraction.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Hour of day when the strategy begins accepting signals.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour of day when the strategy stops opening trades.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Distance from the pivot to breakout levels in price steps.
	/// </summary>
	public int MetricPoints
	{
		get => _metricPoints.Value;
		set => _metricPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, CandleType);

		if (DailyCandleType != null && !Equals(DailyCandleType, CandleType))
			yield return (Security, DailyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousDailyCandle = null;
		_priceStep = 0m;

		_longEntryLevel = 0m;
		_shortEntryLevel = 0m;
		_longStopLevel = 0m;
		_shortStopLevel = 0m;
		_longTargetLevel = 0m;
		_shortTargetLevel = 0m;

		_previousClose = 0m;
		_hasPreviousClose = false;
		_levelsReady = false;
		_canTrade = true;
		_lastCandleOpenTime = null;

		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStop = 0m;
		_shortStop = 0m;
		_longTarget = 0m;
		_shortTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 0.0001m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription.Bind(ProcessDailyCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_previousDailyCandle != null)
		{
			CalculatePivotLevels(_previousDailyCandle);
			_levelsReady = true;
		}

		_previousDailyCandle = candle;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_lastCandleOpenTime != candle.OpenTime)
		{
			_canTrade = true;
			_lastCandleOpenTime = candle.OpenTime;
		}

		var withinHours = IsWithinTradingHours(candle.CloseTime);
		if (!withinHours)
		{
			if (Position != 0m)
			{
				ClosePosition();
				ResetPositionState();
			}

			_previousClose = candle.ClosePrice;
			_hasPreviousClose = true;
			return;
		}

		ManagePositions(candle);

		if (!_levelsReady)
		{
			_previousClose = candle.ClosePrice;
			_hasPreviousClose = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousClose = candle.ClosePrice;
			_hasPreviousClose = true;
			return;
		}

		if (!_hasPreviousClose)
		{
			_previousClose = candle.ClosePrice;
			_hasPreviousClose = true;
			return;
		}

		if (!_canTrade || Position != 0m)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var close = candle.ClosePrice;
		var volume = AdjustVolume(OrderVolume);

		if (volume > 0m && _previousClose < _longEntryLevel && close >= _longEntryLevel)
		{
			BuyMarket(volume);
			_canTrade = false;
			_longEntryPrice = close;
			_longStop = RoundPrice(_longStopLevel);
			_longTarget = RoundPrice(_longTargetLevel);
			_previousClose = close;
			return;
		}

		if (volume > 0m && _previousClose > _shortEntryLevel && close <= _shortEntryLevel)
		{
			SellMarket(volume);
			_canTrade = false;
			_shortEntryPrice = close;
			_shortStop = RoundPrice(_shortStopLevel);
			_shortTarget = RoundPrice(_shortTargetLevel);
			_previousClose = close;
			return;
		}

		_previousClose = close;
	}

	private void ManagePositions(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle);
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var stop = _longStop;
		var target = _longTarget;
		var volume = Position;

		if (volume <= 0m)
			return;

		if (stop > 0m && candle.LowPrice <= stop)
		{
			SellMarket(volume);
			ResetLongState();
			return;
		}

		if (target > 0m && candle.HighPrice >= target)
		{
			SellMarket(volume);
			ResetLongState();
			return;
		}

		var trailingDistance = ConvertPointsToPrice(TrailingPoints);
		if (trailingDistance <= 0m || _longEntryPrice <= 0m)
			return;

		if (candle.ClosePrice - _longEntryPrice >= trailingDistance)
		{
			var newStop = RoundPrice(candle.ClosePrice - trailingDistance);
			if (newStop > _longStop)
			{
				_longStop = newStop;

				if (candle.LowPrice <= _longStop)
				{
					SellMarket(volume);
					ResetLongState();
				}
			}
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var stop = _shortStop;
		var target = _shortTarget;
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		if (stop > 0m && candle.HighPrice >= stop)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		if (target > 0m && candle.LowPrice <= target)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		var trailingDistance = ConvertPointsToPrice(TrailingPoints);
		if (trailingDistance <= 0m || _shortEntryPrice <= 0m)
			return;

		if (_shortEntryPrice - candle.ClosePrice >= trailingDistance)
		{
			var newStop = RoundPrice(candle.ClosePrice + trailingDistance);
			if (_shortStop == 0m || newStop < _shortStop)
			{
				_shortStop = newStop;

				if (candle.HighPrice >= _shortStop)
				{
					BuyMarket(volume);
					ResetShortState();
				}
			}
		}
	}

	private void ResetPositionState()
	{
		ResetLongState();
		ResetShortState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = 0m;
		_longStop = 0m;
		_longTarget = 0m;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = 0m;
		_shortStop = 0m;
		_shortTarget = 0m;
	}

	private void CalculatePivotLevels(ICandleMessage dailyCandle)
	{
		var high = dailyCandle.HighPrice;
		var low = dailyCandle.LowPrice;
		var close = dailyCandle.ClosePrice;

		var pivot = (high + low + close) / 3m;
		var metricDistance = MetricPoints * _priceStep;
		var doubleMetric = 2m * metricDistance;
		var twentyPoints = 20m * _priceStep;
		var range = (high - low) * 1.1m / 2m;

		var lwb = RoundPrice(pivot + metricDistance);
		var lwr = RoundPrice(pivot - metricDistance);
		var lrr = RoundPrice(pivot - doubleMetric);

		var rtl = RoundPrice(Math.Max(close + range, lrr - twentyPoints));
		var rts = RoundPrice(Math.Min(close - range, lrr - twentyPoints));

		_longEntryLevel = lwb;
		_shortEntryLevel = lwr;
		_longStopLevel = lwr;
		_shortStopLevel = lwb;
		_longTargetLevel = rtl;
		_shortTargetLevel = rts;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security == null)
			return volume;

		var step = Security.VolumeStep;
		if (step is decimal volumeStep && volumeStep > 0m)
		{
			var steps = Math.Ceiling(volume / volumeStep);
			if (steps < 1m)
				steps = 1m;
			volume = steps * volumeStep;
		}

		var minVolume = Security.MinVolume;
		if (minVolume is decimal min && volume < min)
			volume = min;

		var maxVolume = Security.MaxVolume;
		if (maxVolume is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private decimal RoundPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private decimal ConvertPointsToPrice(int points)
	{
		if (points <= 0)
			return 0m;

		return points * _priceStep;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		var start = Math.Clamp(StartHour, 0, 23);
		var end = Math.Clamp(EndHour, 0, 23);

		if (start <= end)
			return hour >= start && hour <= end;

		return hour >= start || hour <= end;
	}
}

