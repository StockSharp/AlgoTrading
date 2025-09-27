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
/// Noah 10 Pips 2006 breakout and reversal strategy converted from MetaTrader 4.
/// </summary>
public class Noah10Pips2006Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _timeZoneOffset;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _fridayEndHour;
	private readonly StrategyParam<bool> _tradeFriday;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _secureProfitPips;
	private readonly StrategyParam<int> _trailSecureProfitPips;
	private readonly StrategyParam<int> _minimumRangePips;
	private readonly StrategyParam<int> _startYear;
	private readonly StrategyParam<int> _startMonth;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _maximumRiskPercent;
	private readonly StrategyParam<bool> _fixedVolume;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _protectiveStopOrder;
	private Order _protectiveTakeProfitOrder;

	private DateTime? _currentSessionDate;
	private DateTime? _previousSessionDate;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private decimal _previousSessionHigh;
	private decimal _previousSessionLow;

	private decimal? _midPoint;
	private decimal? _hiPass;
	private decimal? _loPass;
	private decimal? _entryHigh;
	private decimal? _entryLow;
	private decimal _entryRangePips;

	private decimal _pipSize;
	private decimal? _lastClose;
	private decimal? _entryPrice;

	private bool _levelsReady;
	private bool _reverseEnabled;
	private Sides? _reverseDirection;
	private bool _secureActivated;
	private bool _dailyShutdown;

/// <summary>
/// Initializes a new instance of <see cref="Noah10Pips2006Strategy"/>.
/// </summary>
	public Noah10Pips2006Strategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle subscription", "General");

		_timeZoneOffset = Param(nameof(TimeZoneOffset), 0)
		.SetDisplay("Time Zone Offset", "Hours to shift server time", "Schedule");

		_startHour = Param(nameof(StartHour), 2)
		.SetDisplay("Start Hour", "Trading window start hour", "Schedule");

		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start Minute", "Trading window start minute", "Schedule");

		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Trading window end hour", "Schedule");

		_endMinute = Param(nameof(EndMinute), 0)
		.SetDisplay("End Minute", "Trading window end minute", "Schedule");

		_fridayEndHour = Param(nameof(FridayEndHour), 21)
		.SetDisplay("Friday End Hour", "Latest hour to hold positions on Friday", "Schedule");

		_tradeFriday = Param(nameof(TradeFriday), true)
		.SetDisplay("Trade Friday", "Allow new entries on Fridays", "Schedule");

		_stopLossPips = Param(nameof(StopLossPips), 55)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Initial stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Initial target distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetDisplay("Trailing Stop (pips)", "Step distance for trailing after lock", "Risk");

		_secureProfitPips = Param(nameof(SecureProfitPips), 10)
		.SetDisplay("Secure Profit (pips)", "Profit locked after trigger", "Risk");

		_trailSecureProfitPips = Param(nameof(TrailSecureProfitPips), 16)
		.SetDisplay("Secure Trigger (pips)", "Profit required before locking", "Risk");

		_minimumRangePips = Param(nameof(MinimumRangePips), 40)
		.SetDisplay("Minimum Range (pips)", "Required size of entry channel", "Rules");

		_startYear = Param(nameof(StartYear), 2005)
		.SetDisplay("Start Year", "Ignore data before this year", "Schedule");

		_startMonth = Param(nameof(StartMonth), 6)
		.SetDisplay("Start Month", "Ignore data before this month", "Schedule");

		_minVolume = Param(nameof(MinVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Minimum Volume", "Lower bound for calculated volume", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Maximum Volume", "Upper bound for calculated volume", "Risk");

		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 7m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Percent", "Percentage of capital risked per trade", "Risk");

		_fixedVolume = Param(nameof(FixedVolume), true)
		.SetDisplay("Fixed Volume", "Use the strategy volume instead of risk model", "Risk");
	}

/// <summary>
/// Candle type used for the primary subscription.
/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

/// <summary>
/// Hours to shift platform time to the desired trading session.
/// </summary>
	public int TimeZoneOffset
	{
		get => _timeZoneOffset.Value;
		set => _timeZoneOffset.Value = value;
	}

/// <summary>
/// Trading window start hour in the shifted time zone.
/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

/// <summary>
/// Trading window start minute in the shifted time zone.
/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

/// <summary>
/// Trading window end hour in the shifted time zone.
/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

/// <summary>
/// Trading window end minute in the shifted time zone.
/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

/// <summary>
/// Hour when all trades must be closed on Friday.
/// </summary>
	public int FridayEndHour
	{
		get => _fridayEndHour.Value;
		set => _fridayEndHour.Value = value;
	}

/// <summary>
/// Whether the strategy is allowed to open new trades on Fridays.
/// </summary>
	public bool TradeFriday
	{
		get => _tradeFriday.Value;
		set => _tradeFriday.Value = value;
	}

/// <summary>
/// Stop loss distance expressed in pips.
/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

/// <summary>
/// Take profit distance expressed in pips.
/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

/// <summary>
/// Trailing stop distance used after the secure profit step.
/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

/// <summary>
/// Locked profit after the secure trigger activates.
/// </summary>
	public int SecureProfitPips
	{
		get => _secureProfitPips.Value;
		set => _secureProfitPips.Value = value;
	}

/// <summary>
/// Profit threshold that activates the secure stop adjustment.
/// </summary>
	public int TrailSecureProfitPips
	{
		get => _trailSecureProfitPips.Value;
		set => _trailSecureProfitPips.Value = value;
	}

/// <summary>
/// Minimum entry range width expressed in pips.
/// </summary>
	public int MinimumRangePips
	{
		get => _minimumRangePips.Value;
		set => _minimumRangePips.Value = value;
	}

/// <summary>
/// First year of operation for the strategy.
/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

/// <summary>
/// First month of operation for the strategy.
/// </summary>
	public int StartMonth
	{
		get => _startMonth.Value;
		set => _startMonth.Value = value;
	}

/// <summary>
/// Minimum allowed trading volume.
/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

/// <summary>
/// Maximum allowed trading volume.
/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

/// <summary>
/// Risk percentage used when calculating dynamic volume.
/// </summary>
	public decimal MaximumRiskPercent
	{
		get => _maximumRiskPercent.Value;
		set => _maximumRiskPercent.Value = value;
	}

/// <summary>
/// Use a fixed volume instead of the risk-based sizing.
/// </summary>
	public bool FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_protectiveStopOrder = null;
		_protectiveTakeProfitOrder = null;

		_currentSessionDate = null;
		_previousSessionDate = null;
		_sessionHigh = 0m;
		_sessionLow = 0m;
		_previousSessionHigh = 0m;
		_previousSessionLow = 0m;

		_midPoint = null;
		_hiPass = null;
		_loPass = null;
		_entryHigh = null;
		_entryLow = null;
		_entryRangePips = 0m;

		_pipSize = 0m;
		_lastClose = null;
		_entryPrice = null;

		_levelsReady = false;
		_reverseEnabled = false;
		_reverseDirection = null;
		_secureActivated = false;
		_dailyShutdown = false;
	}

/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var localTime = GetLocalTime(candle.OpenTime);
		if (localTime.Year < StartYear || (localTime.Year == StartYear && localTime.Month < StartMonth))
			return;

		UpdateSessionLevels(candle, localTime);
		_lastClose = candle.ClosePrice;

		ManagePosition(candle.ClosePrice);

		localTime = GetLocalTime(candle.CloseTime);

		if (ShouldCloseForDay(localTime))
			{
			if (!_dailyShutdown)
				{
				CloseForDay();
				_dailyShutdown = true;
			}

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TradeFriday && localTime.DayOfWeek == DayOfWeek.Friday)
			return;

		if (!IsWithinTradingWindow(localTime))
			return;

		if (!_levelsReady || _midPoint is not decimal midPoint)
			return;

		if (_entryRangePips < MinimumRangePips)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		if (!HasActiveEntryOrders() && Position == 0m && _lastClose is decimal lastClose)
			{
			if (_hiPass is decimal hiPass && lastClose > midPoint && lastClose < hiPass)
				{
				PlaceSellStop(midPoint, volume);
			}
			else if (_loPass is decimal loPass && lastClose < midPoint && lastClose > loPass)
				{
				PlaceBuyStop(midPoint, volume);
			}
		}
		else if (Position == 0m && _lastClose is decimal close)
			{
			if (IsOrderActive(_sellStopOrder) && !IsOrderActive(_buyStopOrder) && _hiPass is decimal hi && close > midPoint && close < hi)
				{
				PlaceBuyStop(hi, volume);
			}
			else if (IsOrderActive(_buyStopOrder) && !IsOrderActive(_sellStopOrder) && _loPass is decimal lo && close < midPoint && close > lo)
				{
				PlaceSellStop(lo, volume);
			}
		}
	}

	private void ManagePosition(decimal closePrice)
	{
		if (Position > 0m)
			{
			EnsureProtection(Sides.Buy);

			var entry = PositionPrice ?? _entryPrice ?? closePrice;
			var profit = closePrice - entry;

			if (!_secureActivated && SecureProfitPips > 0 && TrailSecureProfitPips > 0 && profit >= ConvertPips(TrailSecureProfitPips))
				{
				var secureStop = entry + ConvertPips(SecureProfitPips);
				MoveProtectiveStop(Sides.Buy, secureStop);
				_secureActivated = true;
				_reverseEnabled = false;
			}
			else if (_secureActivated && TrailingStopPips > 0)
				{
				var newStop = closePrice - ConvertPips(TrailingStopPips);
				if (!IsOrderActive(_protectiveStopOrder) || newStop - _protectiveStopOrder!.Price >= _pipSize)
					MoveProtectiveStop(Sides.Buy, newStop);
			}
		}
		else if (Position < 0m)
			{
			EnsureProtection(Sides.Sell);

			var entry = PositionPrice ?? _entryPrice ?? closePrice;
			var profit = entry - closePrice;

			if (!_secureActivated && SecureProfitPips > 0 && TrailSecureProfitPips > 0 && profit >= ConvertPips(TrailSecureProfitPips))
				{
				var secureStop = entry - ConvertPips(SecureProfitPips);
				MoveProtectiveStop(Sides.Sell, secureStop);
				_secureActivated = true;
				_reverseEnabled = false;
			}
			else if (_secureActivated && TrailingStopPips > 0)
				{
				var newStop = closePrice + ConvertPips(TrailingStopPips);
				if (!IsOrderActive(_protectiveStopOrder) || _protectiveStopOrder!.Price - newStop >= _pipSize)
					MoveProtectiveStop(Sides.Sell, newStop);
			}
		}
	}

	private bool IsWithinTradingWindow(DateTime localTime)
	{
		var start = new TimeSpan(NormalizeHour(StartHour), Math.Clamp(StartMinute, 0, 59), 0);
		var end = new TimeSpan(NormalizeHour(EndHour), Math.Clamp(EndMinute, 0, 59), 0);
		var current = localTime.TimeOfDay;

		if (end <= start)
			return current >= start || current < end;

		return current >= start && current < end;
	}

	private bool ShouldCloseForDay(DateTime localTime)
	{
		var endTime = new TimeSpan(NormalizeHour(EndHour), Math.Clamp(EndMinute, 0, 59), 0);
		var current = localTime.TimeOfDay;

		if (current >= endTime)
			return true;

		if (localTime.DayOfWeek == DayOfWeek.Friday && current >= new TimeSpan(NormalizeHour(FridayEndHour), Math.Clamp(EndMinute, 0, 59), 0))
			return true;

		return false;
	}

	private void CloseForDay()
	{
		CancelIfActive(ref _buyStopOrder);
		CancelIfActive(ref _sellStopOrder);
		CancelIfActive(ref _protectiveStopOrder);
		CancelIfActive(ref _protectiveTakeProfitOrder);

		if (Position > 0m)
			{
			SellMarket(Position);
		}
		else if (Position < 0m)
			{
			BuyMarket(-Position);
		}

		_reverseEnabled = false;
		_reverseDirection = null;
		_secureActivated = false;
	}

	private void EnsureProtection(Sides side)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var entry = PositionPrice ?? _entryPrice ?? _lastClose;
		if (entry is not decimal basePrice || basePrice <= 0m)
			return;

		if (StopLossPips > 0 && !IsOrderActive(_protectiveStopOrder))
			{
			var stopPrice = side == Sides.Buy
			? basePrice - ConvertPips(StopLossPips)
			: basePrice + ConvertPips(StopLossPips);

			_protectiveStopOrder = side == Sides.Buy
			? SellStop(volume, NormalizePrice(stopPrice))
			: BuyStop(volume, NormalizePrice(stopPrice));
		}

		if (TakeProfitPips > 0 && !IsOrderActive(_protectiveTakeProfitOrder))
			{
			var takePrice = side == Sides.Buy
			? basePrice + ConvertPips(TakeProfitPips)
			: basePrice - ConvertPips(TakeProfitPips);

			_protectiveTakeProfitOrder = side == Sides.Buy
			? SellLimit(volume, NormalizePrice(takePrice))
			: BuyLimit(volume, NormalizePrice(takePrice));
		}
	}

	private void MoveProtectiveStop(Sides side, decimal price)
	{
		CancelIfActive(ref _protectiveStopOrder);

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var normalized = NormalizePrice(price);

		_protectiveStopOrder = side == Sides.Buy
		? SellStop(volume, normalized)
		: BuyStop(volume, normalized);
	}

	private void PlaceBuyStop(decimal price, decimal volume)
	{
		CancelIfActive(ref _buyStopOrder);
		_buyStopOrder = BuyStop(volume, NormalizePrice(price));
	}

	private void PlaceSellStop(decimal price, decimal volume)
	{
		CancelIfActive(ref _sellStopOrder);
		_sellStopOrder = SellStop(volume, NormalizePrice(price));
	}

	private void UpdateSessionLevels(ICandleMessage candle, DateTime localTime)
	{
		if (_currentSessionDate == null || localTime.Date != _currentSessionDate.Value)
			{
			_previousSessionDate = _currentSessionDate;
			_previousSessionHigh = _sessionHigh;
			_previousSessionLow = _sessionLow;

			_currentSessionDate = localTime.Date;
			_sessionHigh = candle.HighPrice;
			_sessionLow = candle.LowPrice;

			_dailyShutdown = false;

			CalculateEntryLevels();
		}
		else
			{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = _sessionLow == 0m ? candle.LowPrice : Math.Min(_sessionLow, candle.LowPrice);
		}
	}

	private void CalculateEntryLevels()
	{
		_levelsReady = false;

		if (_previousSessionDate == null)
			return;

		var range = _previousSessionHigh - _previousSessionLow;
		if (range <= 0m)
			return;

		var twentyPips = ConvertPips(20);
		_hiPass = NormalizePrice(_previousSessionHigh + twentyPips);
		_loPass = NormalizePrice(_previousSessionLow - twentyPips);

		_midPoint = NormalizePrice(_previousSessionLow + range / 2m);

		var entryOffset = range <= ConvertPips(160)
		? ConvertPips(40)
		: range * 0.25m;

		_entryHigh = NormalizePrice(_previousSessionHigh - entryOffset);
		_entryLow = NormalizePrice(_previousSessionLow + entryOffset);

		if (_entryHigh is not decimal high || _entryLow is not decimal low || high <= low)
			{
			_entryRangePips = 0m;
			return;
		}

		_entryRangePips = _pipSize > 0m ? (high - low) / _pipSize : 0m;
		_levelsReady = true;
	}

	private decimal CalculateOrderVolume()
	{
		if (FixedVolume)
			return Math.Clamp(Volume, MinVolume, MaxVolume);

		if (StopLossPips <= 0 || _pipSize <= 0m)
			return Math.Clamp(Volume, MinVolume, MaxVolume);

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (portfolioValue is null || portfolioValue <= 0m || priceStep <= 0m || stepPrice <= 0m)
			return Math.Clamp(Volume, MinVolume, MaxVolume);

		var riskAmount = portfolioValue.Value * (MaximumRiskPercent / 100m);
		var stopDistance = ConvertPips(StopLossPips);
		var steps = stopDistance / priceStep;
		var lossPerContract = steps * stepPrice;

		if (lossPerContract <= 0m)
			return Math.Clamp(Volume, MinVolume, MaxVolume);

		var volume = riskAmount / lossPerContract;
		return Math.Clamp(volume, MinVolume, MaxVolume);
	}

	private decimal ConvertPips(int pips)
	{
		return pips * _pipSize;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step is null || step <= 0m)
			return price;

		var steps = Math.Round(price / step.Value);
		return steps * step.Value;
	}

	private static bool IsOrderActive(Order order)
	{
		return order is not null && order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private bool HasActiveEntryOrders()
	{
		return IsOrderActive(_buyStopOrder) || IsOrderActive(_sellStopOrder);
	}

	private void CancelIfActive(ref Order order)
	{
		if (!IsOrderActive(order))
			{
			order = null;
			return;
		}

		CancelOrder(order!);
		order = null;
	}

	private DateTime GetLocalTime(DateTimeOffset time)
	{
		return time.UtcDateTime + TimeSpan.FromHours(TimeZoneOffset);
	}

	private static int NormalizeHour(int hour)
	{
		var value = hour % 24;
		if (value < 0)
			value += 24;
		return value;
	}

/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		if (_buyStopOrder != null && order == _buyStopOrder)
			{
			_buyStopOrder = null;
			CancelIfActive(ref _sellStopOrder);
			OnEntryExecuted(Sides.Buy, trade.Trade.Price);
		}
		else if (_sellStopOrder != null && order == _sellStopOrder)
			{
			_sellStopOrder = null;
			CancelIfActive(ref _buyStopOrder);
			OnEntryExecuted(Sides.Sell, trade.Trade.Price);
		}
	}

	private void OnEntryExecuted(Sides side, decimal price)
	{
		_reverseDirection = side == Sides.Buy ? Sides.Sell : Sides.Buy;
		_reverseEnabled = true;
		_secureActivated = false;
		_entryPrice = PositionPrice ?? price;

		CancelIfActive(ref _protectiveStopOrder);
		CancelIfActive(ref _protectiveTakeProfitOrder);
		EnsureProtection(side);
	}

/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_protectiveStopOrder != null && order == _protectiveStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
			_protectiveStopOrder = null;

		if (_protectiveTakeProfitOrder != null && order == _protectiveTakeProfitOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
			_protectiveTakeProfitOrder = null;
	}

/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			{
			CancelIfActive(ref _protectiveStopOrder);
			CancelIfActive(ref _protectiveTakeProfitOrder);

			if (_reverseEnabled && _reverseDirection is Sides side && IsFormedAndOnlineAndAllowTrading())
				{
				var volume = CalculateOrderVolume();
				if (volume > 0m)
					{
					if (side == Sides.Buy)
						BuyMarket(volume);
					else
						SellMarket(volume);
				}
			}

			_reverseEnabled = false;
			_reverseDirection = null;
			_secureActivated = false;
			_entryPrice = null;
		}
	}
}

