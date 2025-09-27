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
/// Multi Martin reversal strategy converted from the MQL5 expert advisor "MultiMartin".
/// Alternates long and short trades with martingale-style position sizing and optional trailing exit modes.
/// </summary>
public class MultiMartinStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _hourStart;
	private readonly StrategyParam<int> _hourEnd;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _limit;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<StartDirectionOptions> _startDirection;
	private readonly StrategyParam<BadTimeIntervals> _skipBadTime;
	private readonly StrategyParam<TrailModes> _trailMode;
	private readonly StrategyParam<DataType> _candleType;

	private Sides? _activeSide;
	private Sides _nextEntrySide;
	private decimal _lastEntryVolume;
	private decimal _maxVolume;
	private decimal? _entryPrice;
	private decimal? _trailingStopPrice;
	private bool _entryRequested;
	private bool _exitRequested;
	private DateTimeOffset? _blockedUntil;
	private bool _blockedForever;

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiMartinStrategy"/> class.
	/// </summary>
	public MultiMartinStrategy()
	{
		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Use Time Filter", "Restrict trading to the selected intraday window", "Timing");

		_hourStart = Param(nameof(HourStart), 2)
		.SetDisplay("Hour Start", "Inclusive start hour of the trading window", "Timing")
		.SetCanOptimize(true)
		.SetOptimize(0, 23, 1);

		_hourEnd = Param(nameof(HourEnd), 22)
		.SetDisplay("Hour End", "Exclusive end hour of the trading window", "Timing")
		.SetCanOptimize(true)
		.SetOptimize(1, 24, 1);


		_factor = Param(nameof(Factor), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Martingale Factor", "Multiplier applied after losing trades", "Trading")
		.SetCanOptimize(true);

		_limit = Param(nameof(Limit), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Multiplications", "Maximum number of consecutive volume multiplications", "Risk")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Target profit distance expressed in price points", "Risk")
		.SetCanOptimize(true);

		_startDirection = Param(nameof(StartDirection), StartDirectionOptions.Buy)
		.SetDisplay("Start Direction", "Direction of the very first position", "Trading");

		_skipBadTime = Param(nameof(SkipBadTime), BadTimeIntervals.Second)
		.SetDisplay("Skip Bad Time", "Cooldown interval applied after a rejected market order", "Timing");

		_trailMode = Param(nameof(TrailModes), TrailModes.None)
		.SetDisplay("Trailing Mode", "Optional trailing stop behaviour", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Data series used to manage exits", "Data");
	}

	/// <summary>
	/// Enables or disables the time filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Inclusive start hour (0-23) of the intraday trading window.
	/// </summary>
	public int HourStart
	{
		get => _hourStart.Value;
		set => _hourStart.Value = value;
	}

	/// <summary>
	/// Exclusive end hour (1-24) of the intraday trading window.
	/// </summary>
	public int HourEnd
	{
		get => _hourEnd.Value;
		set => _hourEnd.Value = value;
	}


	/// <summary>
	/// Multiplier applied to the next order after a losing trade.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive multiplications before the volume resets.
	/// </summary>
	public int Limit
	{
		get => _limit.Value;
		set => _limit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Direction of the very first position.
	/// </summary>
	public StartDirectionOptions StartDirection
	{
		get => _startDirection.Value;
		set => _startDirection.Value = value;
	}

	/// <summary>
	/// Cooldown interval applied after a rejected market order.
	/// </summary>
	public BadTimeIntervals SkipBadTime
	{
		get => _skipBadTime.Value;
		set => _skipBadTime.Value = value;
	}

	/// <summary>
	/// Trailing stop behaviour applied while a position is open.
	/// </summary>
	public TrailModes TrailModes
	{
		get => _trailMode.Value;
		set => _trailMode.Value = value;
	}

	/// <summary>
	/// Candle series used for exit management and time filtering.
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

		_activeSide = null;
		_nextEntrySide = StartDirection == StartDirectionOptions.Buy ? Sides.Buy : Sides.Sell;
		_lastEntryVolume = Volume;
		_entryPrice = null;
		_trailingStopPrice = null;
		_entryRequested = false;
		_exitRequested = false;
		_blockedUntil = null;
		_blockedForever = false;
		_maxVolume = CalculateMaxVolume();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maxVolume = CalculateMaxVolume();
		_lastEntryVolume = Volume;
		_nextEntrySide = StartDirection == StartDirectionOptions.Buy ? Sides.Buy : Sides.Sell;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_blockedForever)
			return;

		var time = candle.CloseTime != default ? candle.CloseTime : CurrentTime;

		if (_blockedUntil != null && time <= _blockedUntil.Value)
			return; // Cooldown after rejected order.

		if (UseTimeFilter && !IsWithinTradingWindow(time.TimeOfDay))
			return;

		if (Position == 0)
		{
			_trailingStopPrice = null;

			if (_entryRequested)
				return; // Awaiting fill of a previously submitted market order.

			TryEnterPosition();
			return;
		}

		if (_activeSide == Sides.Buy)
		{
			ManageLongPosition(candle);
		}
		else if (_activeSide == Sides.Sell)
		{
			ManageShortPosition(candle);
		}
	}

	private void TryEnterPosition()
	{
		var volume = _lastEntryVolume;
		if (volume <= 0m)
			return;

		Order order = _nextEntrySide == Sides.Buy
		? BuyMarket(volume)
		: SellMarket(volume);

		if (order == null)
			return;

		_entryRequested = true; // Prevent duplicate submissions until the order is filled or rejected.
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		UpdateTrailingForLong(candle.ClosePrice, stopDistance);

		var stopPrice = _trailingStopPrice ?? (_entryPrice ?? candle.ClosePrice) - stopDistance;
		var takePrice = (_entryPrice ?? candle.ClosePrice) + takeDistance;

		var stopHit = stopDistance > 0m && candle.LowPrice <= stopPrice;
		var takeHit = takeDistance > 0m && candle.HighPrice >= takePrice;

		if (!stopHit && !takeHit)
			return;

		if (_exitRequested)
			return;

		_exitRequested = true; // Avoid duplicated exit orders while waiting for execution.
		ClosePosition();
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;

		UpdateTrailingForShort(candle.ClosePrice, stopDistance);

		var stopPrice = _trailingStopPrice ?? (_entryPrice ?? candle.ClosePrice) + stopDistance;
		var takePrice = (_entryPrice ?? candle.ClosePrice) - takeDistance;

		var stopHit = stopDistance > 0m && candle.HighPrice >= stopPrice;
		var takeHit = takeDistance > 0m && candle.LowPrice <= takePrice;

		if (!stopHit && !takeHit)
			return;

		if (_exitRequested)
			return;

		_exitRequested = true;
		ClosePosition();
	}

	private void UpdateTrailingForLong(decimal currentClose, decimal stopDistance)
	{
		if (TrailModes == TrailModes.None || stopDistance <= 0m || _entryPrice == null)
		{
			if (stopDistance > 0m && _entryPrice != null)
				_trailingStopPrice = _entryPrice.Value - stopDistance;
			return;
		}

		var entry = _entryPrice.Value;
		var baseStop = entry - stopDistance;

		if (TrailModes == TrailModes.Breakeven)
		{
			var profit = currentClose - entry;
			var newStop = profit >= stopDistance ? entry : baseStop;
			_trailingStopPrice = _trailingStopPrice == null ? newStop : Math.Max(_trailingStopPrice.Value, newStop);
			return;
		}

		if (TrailModes == TrailModes.Straight)
		{
			var candidate = Math.Max(baseStop, currentClose - stopDistance);
			_trailingStopPrice = _trailingStopPrice == null ? candidate : Math.Max(_trailingStopPrice.Value, candidate);
		}
	}

	private void UpdateTrailingForShort(decimal currentClose, decimal stopDistance)
	{
		if (TrailModes == TrailModes.None || stopDistance <= 0m || _entryPrice == null)
		{
			if (stopDistance > 0m && _entryPrice != null)
				_trailingStopPrice = _entryPrice.Value + stopDistance;
			return;
		}

		var entry = _entryPrice.Value;
		var baseStop = entry + stopDistance;

		if (TrailModes == TrailModes.Breakeven)
		{
			var profit = entry - currentClose;
			var newStop = profit >= stopDistance ? entry : baseStop;
			_trailingStopPrice = _trailingStopPrice == null ? newStop : Math.Min(_trailingStopPrice.Value, newStop);
			return;
		}

		if (TrailModes == TrailModes.Straight)
		{
			var candidate = Math.Min(baseStop, currentClose + stopDistance);
			_trailingStopPrice = _trailingStopPrice == null ? candidate : Math.Min(_trailingStopPrice.Value, candidate);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			_activeSide = Sides.Buy;
			_entryRequested = false;
			_exitRequested = false;
			return;
		}

		if (Position < 0)
		{
			_activeSide = Sides.Sell;
			_entryRequested = false;
			_exitRequested = false;
			return;
		}

		// Position flattened.
		_activeSide = null;
		_entryRequested = false;
		_exitRequested = false;
		_trailingStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var tradeVolume = trade.Volume ?? 0m;
		if (tradeVolume <= 0m)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			HandleBuyTrade(trade.Price, tradeVolume);
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			HandleSellTrade(trade.Price, tradeVolume);
		}
	}

	private void HandleBuyTrade(decimal price, decimal volume)
	{
		if (Position > 0 && !_entryPrice.HasValue)
		{
			_entryPrice = price; // Store filled price for long entries.
			_lastEntryVolume = volume;
			return;
		}

		if (Position == 0 && _activeSide == Sides.Sell && _entryPrice.HasValue)
		{
			// Short position closed by buying back.
			ProcessExit(price, _activeSide.Value);
		}
	}

	private void HandleSellTrade(decimal price, decimal volume)
	{
		if (Position < 0 && !_entryPrice.HasValue)
		{
			_entryPrice = price; // Store filled price for short entries.
			_lastEntryVolume = volume;
			return;
		}

		if (Position == 0 && _activeSide == Sides.Buy && _entryPrice.HasValue)
		{
			// Long position closed by selling.
			ProcessExit(price, _activeSide.Value);
		}
	}

	private void ProcessExit(decimal exitPrice, Sides closedSide)
	{
		if (_entryPrice == null)
			return;

		var entry = _entryPrice.Value;
		var direction = closedSide == Sides.Buy ? 1m : -1m;
		var diff = (exitPrice - entry) * direction;
		var wasProfit = diff >= 0m;

		if (wasProfit)
		{
			_lastEntryVolume = Volume; // Reset to base volume after profitable trade.
			_nextEntrySide = closedSide; // Repeat the same direction after profits.
		}
		else
		{
			var nextVolume = _lastEntryVolume * Factor;
			if (nextVolume > _maxVolume)
				nextVolume = Volume; // Reset after exceeding the configured limit.
			_lastEntryVolume = nextVolume;
			_nextEntrySide = closedSide == Sides.Buy ? Sides.Sell : Sides.Buy; // Reverse direction after losses.
		}

		_entryPrice = null;
		_trailingStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		if (_entryRequested && order.Type == OrderTypes.Market)
		{
			_entryRequested = false;
			ApplyCooldown(fail);
		}

		if (_exitRequested && order.Type == OrderTypes.Market)
		{
			_exitRequested = false;
		}
	}

	private void ApplyCooldown(OrderFail fail)
	{
		var interval = SkipBadTime;
		if (interval == BadTimeIntervals.None)
			return;

		var time = fail.ServerTime != default ? fail.ServerTime : CurrentTime;
		var duration = GetCooldown(interval);

		if (duration == null)
		{
			_blockedForever = true; // FOREVER setting disables further entries.
			return;
		}

		_blockedUntil = time + duration.Value;
	}

	private static TimeSpan? GetCooldown(BadTimeIntervals interval)
	{
		return interval switch
		{
			BadTimeIntervals.None => TimeSpan.Zero,
			BadTimeIntervals.Second => TimeSpan.FromSeconds(1),
			BadTimeIntervals.Minute => TimeSpan.FromMinutes(1),
			BadTimeIntervals.Hour => TimeSpan.FromHours(1),
			BadTimeIntervals.Session => TimeSpan.FromHours(4),
			BadTimeIntervals.Day => TimeSpan.FromDays(1),
			BadTimeIntervals.Month => TimeSpan.FromDays(30),
			BadTimeIntervals.Year => TimeSpan.FromDays(365),
			BadTimeIntervals.Forever => null,
			_ => TimeSpan.Zero,
		};
	}

	private bool IsWithinTradingWindow(TimeSpan time)
	{
		var start = Math.Max(0, Math.Min(23, HourStart));
		var end = Math.Max(1, Math.Min(24, HourEnd));

		if (start == end)
			return true; // Window covers the entire day.

		if (start < end)
		{
			return time.Hours >= start && time.Hours < end;
		}

		// Overnight window (e.g., 22 -> 2).
		return time.Hours >= start || time.Hours < end;
	}

	private decimal CalculateMaxVolume()
	{
		var maxVolume = Volume;
		for (var i = 0; i < Limit; i++)
		{
			maxVolume *= Factor;
		}

		return maxVolume;
	}

	/// <summary>
	/// Available start directions.
	/// </summary>
	public enum StartDirectionOptions
	{
		/// <summary>
		/// Begin the cycle with a long position.
		/// </summary>
		Buy,

		/// <summary>
		/// Begin the cycle with a short position.
		/// </summary>
		Sell
	}

	/// <summary>
	/// Cooldown intervals used after rejected orders.
	/// </summary>
	public enum BadTimeIntervals
	{
		/// <summary>
		/// Do not apply any cooldown.
		/// </summary>
		None,

		/// <summary>
		/// Wait for one second before retrying.
		/// </summary>
		Second,

		/// <summary>
		/// Wait for one minute before retrying.
		/// </summary>
		Minute,

		/// <summary>
		/// Wait for one hour before retrying.
		/// </summary>
		Hour,

		/// <summary>
		/// Wait for four hours (typical session length).
		/// </summary>
		Session,

		/// <summary>
		/// Wait for one calendar day.
		/// </summary>
		Day,

		/// <summary>
		/// Wait for thirty calendar days.
		/// </summary>
		Month,

		/// <summary>
		/// Wait for one calendar year.
		/// </summary>
		Year,

		/// <summary>
		/// Disable trading for the rest of the run.
		/// </summary>
		Forever
	}

	/// <summary>
	/// Trailing stop configuration.
	/// </summary>
	public enum TrailModes
	{
		/// <summary>
		/// Trailing is disabled; a fixed stop-loss is used.
		/// </summary>
		None,

		/// <summary>
		/// Move the stop-loss to breakeven after price advances by the stop distance.
		/// </summary>
		Breakeven,

		/// <summary>
		/// Trail the stop-loss in a straight fashion, maintaining the configured distance.
		/// </summary>
		Straight
	}
}

