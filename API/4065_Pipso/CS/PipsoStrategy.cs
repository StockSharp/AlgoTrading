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

using StockSharp.Localization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range breakout strategy converted from the MetaTrader "Pipso" expert advisor.
/// The algorithm looks for price breakouts outside of the previous range while trading only inside a configurable night session.
/// </summary>
public class PipsoStrategy : Strategy
{
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<int> _sessionLengthHours;
	private readonly StrategyParam<int> _breakoutPeriod;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _minStopDistance;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private decimal? _stopDistance;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private DateTimeOffset? _sessionStartTime;
	private DateTimeOffset? _sessionEndTime;
	private bool _isWithinTradingWindow;

	/// <summary>
	/// Initializes a new instance of <see cref="PipsoStrategy"/>.
	/// </summary>
	public PipsoStrategy()
	{
		_sessionStartHour = Param(nameof(SessionStartHour), 21)
			.SetDisplay("Session Start Hour", "Hour of the day when the breakout window starts", "Schedule")
			.SetNotNegative();

		_sessionLengthHours = Param(nameof(SessionLengthHours), 9)
			.SetDisplay("Session Length (hours)", "Number of hours after the start when trading remains enabled", "Schedule")
			.SetGreaterThanZero();

		_breakoutPeriod = Param(nameof(BreakoutPeriod), 36)
			.SetDisplay("Breakout Period", "Number of completed candles used to build the breakout channel", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 3m)
			.SetDisplay("Stop Multiplier", "Multiplier applied to the price range to calculate the protective stop", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_minStopDistance = Param(nameof(MinStopDistance), 0m)
			.SetDisplay("Minimum Stop Distance", "Absolute minimum distance between entry and stop in price units", "Risk")
			.SetNotNegative();

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Position size used for every breakout order", "General")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay(LocalizedStrings.CandleType, "Timeframe used to evaluate the breakout range", "General");
	}

	/// <summary>
	/// Hour (0-23) when the trading window opens.
	/// </summary>
	public int SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	/// <summary>
	/// Duration of the trading window measured in hours.
	/// </summary>
	public int SessionLengthHours
	{
		get => _sessionLengthHours.Value;
		set => _sessionLengthHours.Value = value;
	}

	/// <summary>
	/// Number of completed candles that define the breakout high and low.
	/// </summary>
	public int BreakoutPeriod
	{
		get => _breakoutPeriod.Value;
		set => _breakoutPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the range width to derive the stop-loss distance.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum stop distance expressed in price units.
	/// </summary>
	public decimal MinStopDistance
	{
		get => _minStopDistance.Value;
		set => _minStopDistance.Value = value;
	}

	/// <summary>
	/// Fixed volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			if (value > 0)
				Volume = value;
		}
	}

	/// <summary>
	/// Candle type used to build the breakout channel.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		// Reset cached channel values and protection levels.
		_rangeHigh = null;
		_rangeLow = null;
		_stopDistance = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_sessionStartTime = null;
		_sessionEndTime = null;
		_isWithinTradingWindow = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Keep the strategy volume in sync with the configured order size.
		Volume = OrderVolume;

		// Initialize breakout indicators for the high and low channel bounds.
		_highest = new Highest
		{
			Length = BreakoutPeriod,
			CandlePrice = CandlePrice.High
		};

		_lowest = new Lowest
		{
			Length = BreakoutPeriod,
			CandlePrice = CandlePrice.Low
		};

		// Subscribe to the main candle stream and process breakout signals.
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		// Visualize candles, indicators, and trades when a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		// Work only with finished candles to mimic the original EA behaviour.
		if (candle.State != CandleStates.Finished)
			return;

		// Determine the timestamp used for the trading schedule.
		var currentTime = candle.CloseTime;
		if (currentTime == default)
			currentTime = candle.OpenTime;

		// Update the trading window and manage protective stops before generating new signals.
		UpdateTradingWindow(currentTime);
		ManageStopLosses(candle);

		var hasActiveOrders = HasActiveOrders();
		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (!hasActiveOrders && _rangeHigh is decimal previousHigh && _rangeLow is decimal previousLow)
		{
			var breakoutUp = candle.HighPrice >= previousHigh;
			var breakoutDown = candle.LowPrice <= previousLow;
			var stopDistanceValue = _stopDistance ?? 0m;

			if (breakoutUp)
			{
				HandleBreakoutUp(candle, stopDistanceValue, canTrade);
			}
			else if (breakoutDown)
			{
				HandleBreakoutDown(candle, stopDistanceValue, canTrade);
			}
		}

		// Update the breakout channel after the trading decisions.
		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		_rangeHigh = highestValue;
		_rangeLow = lowestValue;

		var range = highestValue - lowestValue;
		if (range <= 0m)
		{
			_stopDistance = 0m;
			return;
		}

		var stopDistance = range * StopLossMultiplier;
		if (stopDistance < MinStopDistance)
			stopDistance = MinStopDistance;

		_stopDistance = stopDistance;
	}

	private void HandleBreakoutUp(ICandleMessage candle, decimal stopDistance, bool canTrade)
	{
		// Existing short positions stay untouched, matching the original logic that only closed buys here.
		if (Position < 0m)
			return;

		// Outside the trading window the strategy only liquidates existing longs.
		if (!canTrade || !_isWithinTradingWindow)
		{
			if (Position > 0m)
			{
				SellMarket(Position);
				_longStopPrice = null;
			}

			return;
		}

		if (HasActiveOrders())
			return;

		// Combine position flattening and short entry into a single market order.
		var volumeToSell = OrderVolume + Math.Max(Position, 0m);
		if (volumeToSell <= 0m)
			return;

		SellMarket(volumeToSell);
		_longStopPrice = null;

		if (stopDistance > 0m)
		{
			// Approximate the protective stop relative to the current closing price.
			_shortStopPrice = candle.ClosePrice + stopDistance;
		}
	}

	private void HandleBreakoutDown(ICandleMessage candle, decimal stopDistance, bool canTrade)
	{
		// Existing long positions are preserved because the original EA only closed shorts here.
		if (Position > 0m)
			return;

		// Outside the trading window the strategy only liquidates existing shorts.
		if (!canTrade || !_isWithinTradingWindow)
		{
			if (Position < 0m)
			{
				BuyMarket(-Position);
				_shortStopPrice = null;
			}

			return;
		}

		if (HasActiveOrders())
			return;

		// Combine position flattening and long entry into a single market order.
		var volumeToBuy = OrderVolume + Math.Max(-Position, 0m);
		if (volumeToBuy <= 0m)
			return;

		BuyMarket(volumeToBuy);
		_shortStopPrice = null;

		if (stopDistance > 0m)
		{
			// Approximate the protective stop relative to the current closing price.
			_longStopPrice = candle.ClosePrice - stopDistance;
		}
	}

	private void ManageStopLosses(ICandleMessage candle)
	{
		// Long positions are protected by a stop placed below the entry range.
		if (Position > 0m && _longStopPrice is decimal longStop)
		{
			if (candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				_longStopPrice = null;
			}
		}
		// Short positions are protected by a stop placed above the entry range.
		else if (Position < 0m && _shortStopPrice is decimal shortStop)
		{
			if (candle.HighPrice >= shortStop)
			{
				BuyMarket(-Position);
				_shortStopPrice = null;
			}
		}

		// Clear cached stop levels once the position is flat.
		if (Position <= 0m)
			_longStopPrice = null;

		if (Position >= 0m)
			_shortStopPrice = null;
	}

	private void UpdateTradingWindow(DateTimeOffset currentTime)
	{
		// Rebuild the session borders when the previous window elapsed.
		if (_sessionStartTime == null || _sessionEndTime == null || currentTime >= _sessionEndTime.Value)
		{
			var dayStart = new DateTimeOffset(currentTime.Date, currentTime.Offset);
			var startTime = dayStart + TimeSpan.FromHours(SessionStartHour);
			var endTime = startTime + TimeSpan.FromHours(SessionLengthHours);

			// When the session crosses midnight on Friday, extend the window across the weekend just like the EA.
			if (SessionStartHour + SessionLengthHours > 24 && currentTime.DayOfWeek == DayOfWeek.Friday)
				endTime = endTime.AddDays(2);

			_sessionStartTime = startTime;
			_sessionEndTime = endTime;
		}

		if (_sessionStartTime != null && _sessionEndTime != null)
			_isWithinTradingWindow = currentTime >= _sessionStartTime.Value && currentTime <= _sessionEndTime.Value;
		else
			_isWithinTradingWindow = false;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}
}
