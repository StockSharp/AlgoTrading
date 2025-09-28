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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 "_Fibo_Pivot_multiVal" expert advisor.
/// Places limit orders around daily pivot and Fibonacci zones using StockSharp high level API.
/// </summary>
public class FiboPivotMultiValStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _finishTime;
	private readonly StrategyParam<TimeSpan> _closeAllTime;
	private readonly StrategyParam<bool> _useReversalTargets;
	private readonly StrategyParam<int> _limitPointIn;
	private readonly StrategyParam<int> _limitPointOut;
	private readonly StrategyParam<decimal> _levelPf1;
	private readonly StrategyParam<decimal> _levelF1F2;
	private readonly StrategyParam<decimal> _levelF2F3;
	private readonly StrategyParam<decimal> _levelF3Out;
	private readonly StrategyParam<string> _midZoneOrder;
	private readonly StrategyParam<int> _dailyProfitTarget;
	private readonly StrategyParam<int> _dailyTradeTarget;
	private readonly StrategyParam<int> _symbolProfitTarget;
	private readonly StrategyParam<int> _symbolTradeTarget;

	private readonly Dictionary<string, ZoneState> _zones = new();
	private readonly Dictionary<Order, string> _orderZoneMap = new();

	private DailyStats _currentDay;
	private DailyStats _previousDay;
	private PivotLevels? _activeLevels;
	private DateTime? _activeLevelsDate;

	private decimal _pipSize;
	private decimal _limitInDistance;
	private decimal _limitOutDistance;
	private bool _allowTrading = true;
	private decimal _dailyProfitPoints;
	private int _dailyTrades;
	private decimal _symbolProfitPoints;
	private int _symbolTrades;

	/// <summary>
	/// Initializes a new instance of the <see cref="FiboPivotMultiValStrategy"/> class.
	/// </summary>
	public FiboPivotMultiValStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Timeframe used to compute pivot statistics and detect signals.", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Order volume", "Volume used for each pending order registered by the strategy.", "Trading");

		_startTime = Param(nameof(StartTime), TimeSpan.FromMinutes(1))
			.SetDisplay("Start time", "Session time of day that enables trading.", "Schedule");

		_finishTime = Param(nameof(FinishTime), TimeSpan.FromHours(8))
			.SetDisplay("Finish time", "Session time of day that disables new entries.", "Schedule");

		_closeAllTime = Param(nameof(CloseAllTime), TimeSpan.FromHours(12))
			.SetDisplay("Close all time", "Session time of day when all positions and orders are flattened.", "Schedule");

		_useReversalTargets = Param(nameof(UseReversalTargets), true)
			.SetDisplay("Use reversal targets", "When true take-profit levels stay inside the Fibonacci zone; otherwise breakout and pivot targets are used.", "Execution");

		_limitPointIn = Param(nameof(LimitPointIn), 150)
			.SetNotNegative()
			.SetDisplay("Limit points in", "Range threshold (in points) that activates mean reversion targets.", "Execution");

		_limitPointOut = Param(nameof(LimitPointOut), 50)
			.SetNotNegative()
			.SetDisplay("Limit points out", "Range threshold (in points) that activates breakout targets.", "Execution");

		_levelPf1 = Param(nameof(LevelPf1), 33m)
			.SetDisplay("Level P-F1", "Percentage that splits the Pivot-R1 and Pivot-S1 zones.", "Levels");

		_levelF1F2 = Param(nameof(LevelF1F2), 50m)
			.SetDisplay("Level F1-F2", "Percentage that defines the intermediate R1-R2 and S1-S2 entries.", "Levels");

		_levelF2F3 = Param(nameof(LevelF2F3), 33m)
			.SetDisplay("Level F2-F3", "Percentage that defines the intermediate R2-R3 and S2-S3 entries.", "Levels");

		_levelF3Out = Param(nameof(LevelF3Out), 40m)
			.SetDisplay("Level F3-out", "Percentage that extends the R3 and S3 breakout levels.", "Levels");

		_midZoneOrder = Param(nameof(MidZoneOrderMode), "bs")
			.SetDisplay("Mid-zone order mode", "Allowed directions for trades inside the R1-R2 and S1-S2 ranges (b=buy, s=sell, bs=both).", "Execution");

		_dailyProfitTarget = Param(nameof(DailyProfitTarget), 50)
			.SetNotNegative()
			.SetDisplay("Daily profit target", "Global profit target in points after which trading pauses for the rest of the day.", "Risk");

		_dailyTradeTarget = Param(nameof(DailyTradeTarget), 35)
			.SetNotNegative()
			.SetDisplay("Daily trade limit", "Maximum number of completed trades per day before pausing entries.", "Risk");

		_symbolProfitTarget = Param(nameof(SymbolProfitTarget), 150)
			.SetNotNegative()
			.SetDisplay("Symbol profit target", "Per-symbol profit target in points.", "Risk");

		_symbolTradeTarget = Param(nameof(SymbolTradeTarget), 15)
			.SetNotNegative()
			.SetDisplay("Symbol trade limit", "Per-symbol trade count limit.", "Risk");
	}

	/// <summary>
	/// Trading timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume placed with each new pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Time of day when trading can start.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Time of day when new entries are disabled.
	/// </summary>
	public TimeSpan FinishTime
	{
		get => _finishTime.Value;
		set => _finishTime.Value = value;
	}

	/// <summary>
	/// Time of day when all positions and orders are closed.
	/// </summary>
	public TimeSpan CloseAllTime
	{
		get => _closeAllTime.Value;
		set => _closeAllTime.Value = value;
	}

	/// <summary>
	/// Enables intrazone reversal take-profit levels when true.
	/// </summary>
	public bool UseReversalTargets
	{
		get => _useReversalTargets.Value;
		set => _useReversalTargets.Value = value;
	}

	/// <summary>
	/// Range threshold in points that enables mean reversion targets.
	/// </summary>
	public int LimitPointIn
	{
		get => _limitPointIn.Value;
		set => _limitPointIn.Value = value;
	}

	/// <summary>
	/// Range threshold in points that enables breakout targets.
	/// </summary>
	public int LimitPointOut
	{
		get => _limitPointOut.Value;
		set => _limitPointOut.Value = value;
	}

	/// <summary>
	/// Fibonacci level between pivot and R1/S1.
	/// </summary>
	public decimal LevelPf1
	{
		get => _levelPf1.Value;
		set => _levelPf1.Value = value;
	}

	/// <summary>
	/// Fibonacci level between R1-R2 and S1-S2.
	/// </summary>
	public decimal LevelF1F2
	{
		get => _levelF1F2.Value;
		set => _levelF1F2.Value = value;
	}

	/// <summary>
	/// Fibonacci level between R2-R3 and S2-S3.
	/// </summary>
	public decimal LevelF2F3
	{
		get => _levelF2F3.Value;
		set => _levelF2F3.Value = value;
	}

	/// <summary>
	/// Fibonacci extension used beyond R3 and S3.
	/// </summary>
	public decimal LevelF3Out
	{
		get => _levelF3Out.Value;
		set => _levelF3Out.Value = value;
	}

	/// <summary>
	/// Allowed order direction inside the mid zones.
	/// </summary>
	public string MidZoneOrderMode
	{
		get => NormalizeMidZoneMode(_midZoneOrder.Value);
		set => _midZoneOrder.Value = NormalizeMidZoneMode(value);
	}

	/// <summary>
	/// Daily profit limit in points.
	/// </summary>
	public int DailyProfitTarget
	{
		get => _dailyProfitTarget.Value;
		set => _dailyProfitTarget.Value = value;
	}

	/// <summary>
	/// Daily trade count limit.
	/// </summary>
	public int DailyTradeTarget
	{
		get => _dailyTradeTarget.Value;
		set => _dailyTradeTarget.Value = value;
	}

	/// <summary>
	/// Per-symbol profit limit in points.
	/// </summary>
	public int SymbolProfitTarget
	{
		get => _symbolProfitTarget.Value;
		set => _symbolProfitTarget.Value = value;
	}

	/// <summary>
	/// Per-symbol trade count limit.
	/// </summary>
	public int SymbolTradeTarget
	{
		get => _symbolTradeTarget.Value;
		set => _symbolTradeTarget.Value = value;
	}

	private bool AllowMidZoneBuys => MidZoneOrderMode.Contains('b', StringComparison.InvariantCultureIgnoreCase);
	private bool AllowMidZoneSells => MidZoneOrderMode.Contains('s', StringComparison.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_zones.Clear();
		_orderZoneMap.Clear();
		_currentDay = null;
		_previousDay = null;
		_activeLevels = null;
		_activeLevelsDate = null;
		_dailyProfitPoints = 0m;
		_dailyTrades = 0;
		_symbolProfitPoints = 0m;
		_symbolTrades = 0;
		_allowTrading = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPriceStep();
		_limitInDistance = ConvertPoints(LimitPointIn);
		_limitOutDistance = ConvertPoints(LimitPointOut);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (!_orderZoneMap.TryGetValue(order, out var zoneId) || !_zones.TryGetValue(zoneId, out var zone))
			return;

		switch (order.State)
		{
			case OrderStates.Done when order.Balance == 0m:
			{
				if (zone.EntryOrder == order)
				{
					zone.EntryOrder = null;
					_orderZoneMap.Remove(order);
					zone.ActiveEntryPrice = order.Price;
					zone.ActiveVolume = order.Volume;
					RegisterExitOrders(zoneId, zone);
				}
				else if (zone.TargetOrder == order || zone.StopOrder == order)
				{
					_orderZoneMap.Remove(order);
					FinalizeZoneTrade(zoneId, zone, order.Price);
				}

				break;
			}
			case OrderStates.Done when order.Balance > 0m:
			{
				if (zone.EntryOrder == order)
					zone.EntryOrder = null;

				if (zone.TargetOrder == order)
					zone.TargetOrder = null;

				if (zone.StopOrder == order)
					zone.StopOrder = null;

				_orderZoneMap.Remove(order);
				break;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		// Keep statistics for daily trade counts.
		if (trade.Order == null)
			return;

		if (_orderZoneMap.ContainsKey(trade.Order))
			return;

		// Only count fills that are not linked to pending zone orders (handled in FinalizeZoneTrade).
		_dailyTrades++;
		_symbolTrades++;
		CheckDailyLimits();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyStatistics(candle);

		if (_activeLevels is not PivotLevels levels || _activeLevelsDate == null)
			return;

		var currentDate = candle.OpenTime.Date;
		if (_activeLevelsDate.Value.Date != currentDate)
			return;

		var timeOfDay = candle.CloseTime.TimeOfDay;

		if (timeOfDay < StartTime)
		{
			ResetSessionState();
			return;
		}

		if (timeOfDay >= CloseAllTime)
		{
			FlattenPosition();
			DisableAllZones();
			_allowTrading = true;
			return;
		}

		if (timeOfDay >= FinishTime)
		{
			_allowTrading = false;
			CancelEntryOrders();
			return;
		}

		if (!_allowTrading || OrderVolume <= 0m)
		{
			CancelEntryOrders();
			return;
		}

		EvaluateZones(levels, candle.ClosePrice, levels.Range);
	}

	private void EvaluateZones(PivotLevels levels, decimal price, decimal dailyRange)
	{
		var pivot = levels.Pivot;
		var limitOut = _limitOutDistance;
		var limitIn = _limitInDistance;

		bool priceIn(decimal lower, decimal upper) => price > lower && price < upper;

		if (priceIn(levels.R2, levels.R3))
		{
			EnsureZoneOrder("R2R3_Long", Sides.Buy, levels.R2R3Lower,
			SelectTarget(levels.R2R3Upper, levels.R3Extension, pivot, dailyRange, limitOut, limitIn), null);

			EnsureZoneOrder("R2R3_Short", Sides.Sell, levels.R2R3Upper,
			SelectTarget(levels.R2R3Lower, pivot, pivot, dailyRange, limitOut, limitIn), null);
		}
		else
		{
			DisableZone("R2R3_Long");
			DisableZone("R2R3_Short");
		}

		if (priceIn(levels.R1, levels.R2))
		{
			if (AllowMidZoneBuys)
			{
				EnsureZoneOrder("R1R2_Long", Sides.Buy, levels.R1R2Intermediate,
				SelectTarget(levels.R2, levels.R3Extension, pivot, dailyRange, limitOut, limitIn), null);
			}
			else
			{
				DisableZone("R1R2_Long");
			}

			if (AllowMidZoneSells)
			{
				EnsureZoneOrder("R1R2_Short", Sides.Sell, levels.R1R2Intermediate,
				SelectTarget(levels.R1, pivot, pivot, dailyRange, limitOut, limitIn), null);
			}
			else
			{
				DisableZone("R1R2_Short");
			}
		}
		else
		{
			DisableZone("R1R2_Long");
			DisableZone("R1R2_Short");
		}

		if (priceIn(pivot, levels.R1))
		{
			EnsureZoneOrder("P_R1_Long", Sides.Buy, levels.PR1Lower,
			SelectTarget(levels.PR1Upper, levels.R3Extension, pivot, dailyRange, limitOut, limitIn), null);

			EnsureZoneOrder("P_R1_Short", Sides.Sell, levels.PR1Upper,
			SelectTarget(levels.PR1Lower, pivot, pivot, dailyRange, limitOut, limitIn), null);
		}
		else
		{
			DisableZone("P_R1_Long");
			DisableZone("P_R1_Short");
		}

		if (priceIn(levels.S1, pivot))
		{
			EnsureZoneOrder("P_S1_Long", Sides.Buy, levels.PS1Upper,
			SelectTarget(levels.PS1Lower, pivot, pivot, dailyRange, limitOut, limitIn), null);

			EnsureZoneOrder("P_S1_Short", Sides.Sell, levels.PS1Lower,
			SelectTarget(levels.PS1Upper, levels.S3Extension, pivot, dailyRange, limitOut, limitIn), null);
		}
		else
		{
			DisableZone("P_S1_Long");
			DisableZone("P_S1_Short");
		}

		if (priceIn(levels.S2, levels.S1))
		{
			if (AllowMidZoneSells)
			{
				EnsureZoneOrder("S1S2_Short", Sides.Sell, levels.S1S2Intermediate,
				SelectTarget(levels.S2, levels.S3Extension, pivot, dailyRange, limitOut, limitIn), null);
			}
			else
			{
				DisableZone("S1S2_Short");
			}

			if (AllowMidZoneBuys)
			{
				EnsureZoneOrder("S1S2_Long", Sides.Buy, levels.S1S2Intermediate,
				SelectTarget(levels.S1, pivot, pivot, dailyRange, limitOut, limitIn), null);
			}
			else
			{
				DisableZone("S1S2_Long");
			}
		}
		else
		{
			DisableZone("S1S2_Short");
			DisableZone("S1S2_Long");
		}

		if (priceIn(levels.S3, levels.S2))
		{
			EnsureZoneOrder("S2S3_Short", Sides.Sell, levels.S2S3Upper,
			SelectTarget(levels.S2S3Lower, pivot, pivot, dailyRange, limitOut, limitIn), null);

			EnsureZoneOrder("S2S3_Long", Sides.Buy, levels.S2S3Lower,
			SelectTarget(levels.S2S3Upper, pivot, pivot, dailyRange, limitOut, limitIn), null);
		}
		else
		{
			DisableZone("S2S3_Short");
			DisableZone("S2S3_Long");
		}

		if (price >= levels.R3)
		{
			EnsureZoneOrder("AboveR3", Sides.Buy, levels.R3Extension,
			SelectTarget(levels.R3Extension, levels.R3Extension, pivot, dailyRange, limitOut, limitIn), null);
			_allowTrading = false;
		}
		else
		{
			DisableZone("AboveR3");
		}

		if (price <= levels.S3)
		{
			EnsureZoneOrder("BelowS3", Sides.Sell, levels.S3Extension,
			SelectTarget(levels.S3Extension, levels.S3Extension, pivot, dailyRange, limitOut, limitIn), null);
			_allowTrading = false;
		}
		else
		{
			DisableZone("BelowS3");
		}
	}

	private void UpdateDailyStatistics(ICandleMessage candle)
	{
		var candleDate = candle.OpenTime.Date;

		if (_currentDay == null || _currentDay.Date != candleDate)
		{
			if (_currentDay != null)
				_previousDay = _currentDay;

			_currentDay = new DailyStats(candleDate);
		}

		_currentDay.Update(candle);

		if (_previousDay != null && _activeLevelsDate?.Date != candleDate)
		{
			_activeLevels = CalculatePivotLevels(_previousDay, LevelPf1, LevelF1F2, LevelF2F3, LevelF3Out);
			_activeLevelsDate = candleDate;
			_limitInDistance = ConvertPoints(LimitPointIn);
			_limitOutDistance = ConvertPoints(LimitPointOut);
			ResetSessionState();
			LogInfo(FormattableString.Invariant($"Pivot levels for {candleDate:yyyy-MM-dd}: P={FormatPrice(_activeLevels.Pivot)}, R1={FormatPrice(_activeLevels.R1)}, R2={FormatPrice(_activeLevels.R2)}, R3={FormatPrice(_activeLevels.R3)}, S1={FormatPrice(_activeLevels.S1)}, S2={FormatPrice(_activeLevels.S2)}, S3={FormatPrice(_activeLevels.S3)}"));
		}
	}

	private void ResetSessionState()
	{
		_allowTrading = true;
		_dailyProfitPoints = 0m;
		_dailyTrades = 0;
		_symbolProfitPoints = 0m;
		_symbolTrades = 0;
		DisableAllZones();
	}

	private void FlattenPosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (Position > 0m)
			SellMarket(volume);
		else
			BuyMarket(volume);
	}

	private void RegisterExitOrders(string zoneId, ZoneState zone)
	{
		CancelOrderIfActive(zone.TargetOrder);
		CancelOrderIfActive(zone.StopOrder);
		zone.TargetOrder = null;
		zone.StopOrder = null;

		if (zone.ActiveVolume <= 0m)
			zone.ActiveVolume = OrderVolume;

		if (zone.ActiveVolume <= 0m)
			return;

		if (zone.TargetPrice is decimal target)
		{
			var order = zone.Side == Sides.Buy
			? SellLimit(zone.ActiveVolume, target)
			: BuyLimit(zone.ActiveVolume, target);

			zone.TargetOrder = order;
			_orderZoneMap[order] = zoneId;
		}

		if (zone.StopPrice is decimal stop && stop > 0m)
		{
			var order = zone.Side == Sides.Buy
			? SellStop(zone.ActiveVolume, stop)
			: BuyStop(zone.ActiveVolume, stop);

			zone.StopOrder = order;
			_orderZoneMap[order] = zoneId;
		}
	}

	private void FinalizeZoneTrade(string zoneId, ZoneState zone, decimal exitPrice)
	{
		if (zone.ActiveEntryPrice is not decimal entryPrice || zone.ActiveVolume <= 0m)
		{
			DisableZone(zoneId);
			return;
		}

		var direction = zone.Side == Sides.Buy ? 1m : -1m;
		var profitPoints = (exitPrice - entryPrice) / _pipSize * direction;

		_dailyProfitPoints += profitPoints;
		_symbolProfitPoints += profitPoints;
		_dailyTrades++;
		_symbolTrades++;

		zone.ActiveEntryPrice = null;
		zone.ActiveVolume = 0m;

		DisableZone(zoneId);
		CheckDailyLimits();
	}

	private void CheckDailyLimits()
	{
		var dailyLimitHit = (_dailyProfitTarget.Value > 0 && _dailyProfitPoints >= _dailyProfitTarget.Value)
		|| (_dailyTradeTarget.Value > 0 && _dailyTrades >= _dailyTradeTarget.Value);

		var symbolLimitHit = (_symbolProfitTarget.Value > 0 && _symbolProfitPoints >= _symbolProfitTarget.Value)
		|| (_symbolTradeTarget.Value > 0 && _symbolTrades >= _symbolTradeTarget.Value);

		if (dailyLimitHit || symbolLimitHit)
		{
			_allowTrading = false;
			CancelEntryOrders();
		}
	}

	private void EnsureZoneOrder(string zoneId, Sides side, decimal entryPrice, decimal? targetPrice, decimal? stopPrice)
	{
		if (!_zones.TryGetValue(zoneId, out var zone))
		{
			zone = new ZoneState();
			_zones[zoneId] = zone;
		}

		zone.Side = side;
		zone.TargetPrice = targetPrice;
		zone.StopPrice = stopPrice;

		if (zone.EntryOrder is Order order && IsOrderAlive(order) && order.Price == entryPrice && zone.ActiveEntryPrice == null)
			return;

		CancelOrderIfActive(zone.EntryOrder);
		zone.EntryOrder = null;

		if (OrderVolume <= 0m)
			return;

		var newOrder = side == Sides.Buy
		? BuyLimit(OrderVolume, entryPrice)
		: SellLimit(OrderVolume, entryPrice);

		zone.EntryOrder = newOrder;
		zone.ActiveEntryPrice = null;
		zone.ActiveVolume = 0m;
		_orderZoneMap[newOrder] = zoneId;
	}

	private void DisableZone(string zoneId)
	{
		if (!_zones.TryGetValue(zoneId, out var zone))
			return;

		CancelZoneOrders(zoneId, zone);
	}

	private void DisableAllZones()
	{
		foreach (var (zoneId, zone) in _zones)
			CancelZoneOrders(zoneId, zone);
	}

	private void CancelEntryOrders()
	{
		foreach (var (zoneId, zone) in _zones)
		{
			if (zone.EntryOrder != null)
				CancelZoneOrders(zoneId, zone, cancelEntryOnly: true);
		}
	}

	private void CancelZoneOrders(string zoneId, ZoneState zone, bool cancelEntryOnly = false)
	{
		void Cancel(Order order)
		{
			if (order == null)
				return;

			_orderZoneMap.Remove(order);
			if (IsOrderAlive(order))
				CancelOrder(order);
		}

		Cancel(zone.EntryOrder);
		zone.EntryOrder = null;

		if (cancelEntryOnly)
			return;

		Cancel(zone.TargetOrder);
		zone.TargetOrder = null;

		Cancel(zone.StopOrder);
		zone.StopOrder = null;

		zone.ActiveEntryPrice = null;
		zone.ActiveVolume = 0m;
	}

	private decimal? SelectTarget(decimal defaultTarget, decimal breakoutTarget, decimal meanReversionTarget, decimal range, decimal limitOut, decimal limitIn)
	{
		if (UseReversalTargets)
			return defaultTarget;

		if (limitOut > 0m && range <= limitOut)
			return breakoutTarget;

		if (limitIn > 0m && range >= limitIn)
			return meanReversionTarget;

		return defaultTarget;
	}

	private void CancelOrderIfActive(Order order)
	{
		if (order == null)
			return;

		_orderZoneMap.Remove(order);
		if (IsOrderAlive(order))
			CancelOrder(order);
	}

	private static bool IsOrderAlive(Order order)
	{
		return order != null && order.State is OrderStates.Active or OrderStates.Pending;
	}

	private decimal ConvertPoints(int points)
	{
		if (points <= 0 || _pipSize <= 0m)
			return 0m;

		return points * _pipSize;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private static string NormalizeMidZoneMode(string value)
	{
		if (value.IsEmptyOrWhiteSpace())
			return "bs";

		var normalized = value.Trim().ToLowerInvariant();
		return normalized is "b" or "s" or "bs" ? normalized : "bs";
	}

	private string FormatPrice(decimal price)
	{
		var security = Security;
		if (security?.Decimals is int decimals && decimals > 0)
			return price.ToString("F" + decimals, CultureInfo.InvariantCulture);

		return price.ToString(CultureInfo.InvariantCulture);
	}

	private static PivotLevels CalculatePivotLevels(DailyStats stats, decimal levelPf1, decimal levelF1F2, decimal levelF2F3, decimal levelF3Out)
	{
		var high = stats.High;
		var low = stats.Low;
		var close = stats.Close;
		var range = high - low;
		var pivot = (high + low + close) / 3m;

		var r1 = pivot + range * 0.38m;
		var r2 = pivot + range * 0.62m;
		var r3 = pivot + range * 0.99m;
		var s1 = pivot - range * 0.38m;
		var s2 = pivot - range * 0.62m;
		var s3 = pivot - range * 0.99m;

		var pr1Lower = pivot + (r1 - pivot) * (levelPf1 / 100m);
		var pr1Upper = r1 - (r1 - pivot) * (levelPf1 / 100m);

		var r1r2Intermediate = r1 + (r2 - r1) * (levelF1F2 / 100m);
		var r2r3Lower = r2 + (r3 - r2) * (levelF2F3 / 100m);
		var r2r3Upper = r3 - (r3 - r2) * (levelF2F3 / 100m);

		var ps1Lower = pivot - (pivot - s1) * (levelPf1 / 100m);
		var ps1Upper = s1 + (pivot - s1) * (levelPf1 / 100m);
		var s1s2Intermediate = s1 - (s1 - s2) * (levelF1F2 / 100m);
		var s2s3Upper = s2 - (s2 - s3) * (levelF2F3 / 100m);
		var s2s3Lower = s3 + (s2 - s3) * (levelF2F3 / 100m);

		var r3Extension = r3 + (r3 - r2) * (levelF3Out / 100m);
		var s3Extension = s3 - (s2 - s3) * (levelF3Out / 100m);

		return new PivotLevels(pivot, r1, r2, r3, s1, s2, s3,
		pr1Lower, pr1Upper, r1r2Intermediate, r2r3Lower, r2r3Upper,
		ps1Lower, ps1Upper, s1s2Intermediate, s2s3Upper, s2s3Lower,
		r3Extension, s3Extension, range);
	}

	private sealed class ZoneState
	{
		public Sides Side { get; set; }
		public Order EntryOrder { get; set; }
		public Order TargetOrder { get; set; }
		public Order StopOrder { get; set; }
		public decimal? TargetPrice { get; set; }
		public decimal? StopPrice { get; set; }
		public decimal? ActiveEntryPrice { get; set; }
		public decimal ActiveVolume { get; set; }
	}

	private sealed class DailyStats
	{
		public DailyStats(DateTime date)
		{
			Date = date;
			High = decimal.MinValue;
			Low = decimal.MaxValue;
		}

		public DateTime Date { get; }
		public decimal High { get; private set; }
		public decimal Low { get; private set; }
		public decimal Close { get; private set; }

		public void Update(ICandleMessage candle)
		{
			if (candle.HighPrice > High)
				High = candle.HighPrice;

			if (candle.LowPrice < Low)
				Low = candle.LowPrice;

			Close = candle.ClosePrice;
		}
	}

	private readonly record struct PivotLevels(
	decimal Pivot,
	decimal R1,
	decimal R2,
	decimal R3,
	decimal S1,
	decimal S2,
	decimal S3,
	decimal PR1Lower,
	decimal PR1Upper,
	decimal R1R2Intermediate,
	decimal R2R3Lower,
	decimal R2R3Upper,
	decimal PS1Lower,
	decimal PS1Upper,
	decimal S1S2Intermediate,
	decimal S2S3Upper,
	decimal S2S3Lower,
	decimal R3Extension,
	decimal S3Extension,
	decimal Range);
}