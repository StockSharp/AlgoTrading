using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flat channel breakout strategy converted from the MetaTrader 5 version.
/// </summary>
public class FlatChannelStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<bool> _tradeTuesday;
	private readonly StrategyParam<bool> _tradeWednesday;
	private readonly StrategyParam<bool> _tradeThursday;
	private readonly StrategyParam<int> _mondayStartHour;
	private readonly StrategyParam<int> _fridayStopHour;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _orderLifetimeSeconds;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<int> _flatBars;
	private readonly StrategyParam<decimal> _channelMinPips;
	private readonly StrategyParam<decimal> _channelMaxPips;
	private readonly StrategyParam<bool> _useBreakeven;
	private readonly StrategyParam<decimal> _fiboTrail;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _stdDev = null!;
	private DonchianChannels _donchian = null!;

	private decimal _previousStdDev;
	private int _flatBarCount;
	private decimal _channelHigh;
	private decimal _channelLow;
	private bool _allowLongEntry;
	private bool _allowShortEntry;
	private decimal _lastClosedPnL;
	private int _lossCount;

	private Order? _buyStopEntryOrder;
	private Order? _sellStopEntryOrder;
	private Order? _longStopLossOrder;
	private Order? _longTakeProfitOrder;
	private Order? _shortStopLossOrder;
	private Order? _shortTakeProfitOrder;

	private DateTimeOffset? _buyEntryPlacedTime;
	private DateTimeOffset? _sellEntryPlacedTime;

	private decimal _plannedLongEntry;
	private decimal _plannedLongStop;
	private decimal _plannedLongTake;
	private decimal _plannedShortEntry;
	private decimal _plannedShortStop;
	private decimal _plannedShortTake;

	private bool _longBreakevenApplied;
	private bool _shortBreakevenApplied;

	/// <summary>
	/// Enable trading schedule filter.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Allow trading on Tuesday.
	/// </summary>
	public bool TradeTuesday
	{
		get => _tradeTuesday.Value;
		set => _tradeTuesday.Value = value;
	}

	/// <summary>
	/// Allow trading on Wednesday.
	/// </summary>
	public bool TradeWednesday
	{
		get => _tradeWednesday.Value;
		set => _tradeWednesday.Value = value;
	}

	/// <summary>
	/// Allow trading on Thursday.
	/// </summary>
	public bool TradeThursday
	{
		get => _tradeThursday.Value;
		set => _tradeThursday.Value = value;
	}

	/// <summary>
	/// Hour to start trading on Monday (0-23).
	/// </summary>
	public int MondayStartHour
	{
		get => _mondayStartHour.Value;
		set => _mondayStartHour.Value = value;
	}

	/// <summary>
	/// Hour to stop trading on Friday (0-23).
	/// </summary>
	public int FridayStopHour
	{
		get => _fridayStopHour.Value;
		set => _fridayStopHour.Value = value;
	}

	/// <summary>
	/// Use risk-based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Percentage of equity to risk per trade when money management is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed volume (lots) per trade when money management is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Lifetime of pending entry orders in seconds (0 disables expiration).
	/// </summary>
	public int OrderLifetimeSeconds
	{
		get => _orderLifetimeSeconds.Value;
		set => _orderLifetimeSeconds.Value = value;
	}

	/// <summary>
	/// Standard deviation indicator period.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Minimum number of bars with falling volatility required to form a flat channel.
	/// </summary>
	public int FlatBars
	{
		get => _flatBars.Value;
		set => _flatBars.Value = value;
	}

	/// <summary>
	/// Minimum channel width expressed in pips.
	/// </summary>
	public decimal ChannelMinPips
	{
		get => _channelMinPips.Value;
		set => _channelMinPips.Value = value;
	}

	/// <summary>
	/// Maximum channel width expressed in pips.
	/// </summary>
	public decimal ChannelMaxPips
	{
		get => _channelMaxPips.Value;
		set => _channelMaxPips.Value = value;
	}

	/// <summary>
	/// Enable stop-loss breakeven logic based on Fibonacci coefficient.
	/// </summary>
	public bool UseBreakeven
	{
		get => _useBreakeven.Value;
		set => _useBreakeven.Value = value;
	}

	/// <summary>
	/// Fraction of the distance to take-profit used to trigger breakeven.
	/// </summary>
	public decimal FiboTrail
	{
		get => _fiboTrail.Value;
		set => _fiboTrail.Value = value;
	}

	/// <summary>
	/// Candle type to analyse.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FlatChannelStrategy"/>.
	/// </summary>
	public FlatChannelStrategy()
	{
		_useTradingHours = Param(nameof(UseTradingHours), true)
		.SetDisplay("Use Trading Hours", "Enable trading window filter", "General");

		_tradeTuesday = Param(nameof(TradeTuesday), true)
		.SetDisplay("Trade Tuesday", "Allow trading on Tuesday", "General");

		_tradeWednesday = Param(nameof(TradeWednesday), true)
		.SetDisplay("Trade Wednesday", "Allow trading on Wednesday", "General");

		_tradeThursday = Param(nameof(TradeThursday), true)
		.SetDisplay("Trade Thursday", "Allow trading on Thursday", "General");

		_mondayStartHour = Param(nameof(MondayStartHour), 0)
		.SetDisplay("Monday Start Hour", "Trading start hour on Monday", "General");

		_fridayStopHour = Param(nameof(FridayStopHour), 19)
		.SetDisplay("Friday Stop Hour", "Trading stop hour on Friday", "General");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Use Money Management", "Enable risk-based sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 7m)
		.SetDisplay("Risk %", "Risk per trade percent", "Risk")
		.SetGreaterThanZero();

		_fixedVolume = Param(nameof(FixedVolume), 0.01m)
		.SetDisplay("Fixed Volume", "Fixed lot size for entries", "Risk")
		.SetGreaterThanZero();

		_orderLifetimeSeconds = Param(nameof(OrderLifetimeSeconds), 86400)
		.SetDisplay("Order Lifetime", "Pending order lifetime in seconds", "Orders")
		.SetGreaterThanOrEqualZero();

		_stdDevPeriod = Param(nameof(StdDevPeriod), 37)
		.SetDisplay("StdDev Period", "Standard deviation indicator period", "Indicators")
		.SetGreaterThanZero();

		_flatBars = Param(nameof(FlatBars), 2)
		.SetDisplay("Flat Bars", "Minimum bars in flat state", "Indicators")
		.SetGreaterThanZero();

		_channelMinPips = Param(nameof(ChannelMinPips), 610m)
		.SetDisplay("Channel Min Pips", "Minimum channel width in pips", "Indicators")
		.SetGreaterThanZero();

		_channelMaxPips = Param(nameof(ChannelMaxPips), 1860m)
		.SetDisplay("Channel Max Pips", "Maximum channel width in pips", "Indicators")
		.SetGreaterThanZero();

		_useBreakeven = Param(nameof(UseBreakeven), true)
		.SetDisplay("Use Breakeven", "Move stop-loss to entry using Fibonacci", "Risk");

		_fiboTrail = Param(nameof(FiboTrail), 0.873m)
		.SetDisplay("Fibo Trail", "Fibonacci coefficient for breakeven", "Risk")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle type", "General");
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

		_previousStdDev = 0m;
		_flatBarCount = 0;
		_channelHigh = 0m;
		_channelLow = 0m;
		_allowLongEntry = false;
		_allowShortEntry = false;
		_lastClosedPnL = 0m;
		_lossCount = 0;
		_buyStopEntryOrder = null;
		_sellStopEntryOrder = null;
		_longStopLossOrder = null;
		_longTakeProfitOrder = null;
		_shortStopLossOrder = null;
		_shortTakeProfitOrder = null;
		_buyEntryPlacedTime = null;
		_sellEntryPlacedTime = null;
		_plannedLongEntry = 0m;
		_plannedLongStop = 0m;
		_plannedLongTake = 0m;
		_plannedShortEntry = 0m;
		_plannedShortStop = 0m;
		_plannedShortTake = 0m;
		_longBreakevenApplied = false;
		_shortBreakevenApplied = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = FixedVolume;

		_stdDev = new StandardDeviation { Length = StdDevPeriod };
		_donchian = new DonchianChannels { Length = FlatBars };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(_donchian, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue channelValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var stdDevValue = _stdDev.Process(medianPrice, candle.CloseTime, true).ToDecimal();

		if (!_stdDev.IsFormed || channelValue is not DonchianChannelsValue donchianValue)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		if (donchianValue.Upper is not decimal upper || donchianValue.Lower is not decimal lower)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		UpdateStdDevState(stdDevValue, upper, lower, candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousStdDev = stdDevValue;
			return;
		}

		UpdateOrderReferences();
		CheckOrderExpiration(candle.CloseTime);

		if (!IsTradingAllowed(candle.CloseTime))
		{
			CancelEntryOrders();
			_previousStdDev = stdDevValue;
			return;
		}

		if (_flatBarCount < FlatBars)
		{
			CancelEntryOrders();
			_previousStdDev = stdDevValue;
			return;
		}

		if (_channelHigh <= _channelLow)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		var channelWidth = _channelHigh - _channelLow;
		var priceStep = Security?.PriceStep ?? 1m;
		var minWidth = ChannelMinPips * priceStep;
		var maxWidth = ChannelMaxPips * priceStep;

		if (channelWidth < minWidth || channelWidth > maxWidth)
		{
			CancelEntryOrders();
			_previousStdDev = stdDevValue;
			return;
		}

		var priceInsideChannel = candle.ClosePrice > _channelLow && candle.ClosePrice < _channelHigh;

		if (!priceInsideChannel || Position != 0)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		PlaceEntryOrders(channelWidth, candle.CloseTime);

		if (UseBreakeven)
		ApplyBreakeven(candle.ClosePrice);

		_previousStdDev = stdDevValue;
	}

	private void UpdateStdDevState(decimal stdDevValue, decimal upper, decimal lower, ICandleMessage candle)
	{
		if (_previousStdDev == 0m)
		{
			_previousStdDev = stdDevValue;
			return;
		}

		if (stdDevValue < _previousStdDev)
		{
			_flatBarCount++;
			_allowLongEntry = true;
			_allowShortEntry = true;

			if (_flatBarCount == FlatBars)
			{
				_channelHigh = upper;
				_channelLow = lower;
			}
			else if (_flatBarCount > FlatBars)
			{
				if (candle.HighPrice > _channelHigh)
				_channelHigh = candle.HighPrice;

				if (candle.LowPrice < _channelLow)
				_channelLow = candle.LowPrice;
			}
		}
		else if (stdDevValue > _previousStdDev)
		{
			ResetFlatState();
		}
		else if (_flatBarCount >= FlatBars && _channelHigh <= _channelLow)
		{
			_channelHigh = upper;
			_channelLow = lower;
		}
	}

	private void ResetFlatState()
	{
		_flatBarCount = 0;
		_allowLongEntry = false;
		_allowShortEntry = false;
		_channelHigh = 0m;
		_channelLow = 0m;
		CancelEntryOrders();
	}

	private void UpdateOrderReferences()
	{
		if (_buyStopEntryOrder != null && _buyStopEntryOrder.State != OrderStates.Active)
		{
			_buyStopEntryOrder = null;
			_buyEntryPlacedTime = null;
		}

		if (_sellStopEntryOrder != null && _sellStopEntryOrder.State != OrderStates.Active)
		{
			_sellStopEntryOrder = null;
			_sellEntryPlacedTime = null;
		}

		if (_longStopLossOrder != null && _longStopLossOrder.State != OrderStates.Active)
		_longStopLossOrder = null;

		if (_longTakeProfitOrder != null && _longTakeProfitOrder.State != OrderStates.Active)
		_longTakeProfitOrder = null;

		if (_shortStopLossOrder != null && _shortStopLossOrder.State != OrderStates.Active)
		_shortStopLossOrder = null;

		if (_shortTakeProfitOrder != null && _shortTakeProfitOrder.State != OrderStates.Active)
		_shortTakeProfitOrder = null;
	}

	private void CheckOrderExpiration(DateTimeOffset time)
	{
		if (OrderLifetimeSeconds <= 0)
		return;

		var lifetime = TimeSpan.FromSeconds(OrderLifetimeSeconds);

		if (_buyStopEntryOrder != null && _buyStopEntryOrder.State == OrderStates.Active && _buyEntryPlacedTime.HasValue)
		{
			if (time - _buyEntryPlacedTime.Value >= lifetime)
			{
				CancelOrder(_buyStopEntryOrder);
				_buyStopEntryOrder = null;
				_buyEntryPlacedTime = null;
				_allowLongEntry = true;
			}
		}

		if (_sellStopEntryOrder != null && _sellStopEntryOrder.State == OrderStates.Active && _sellEntryPlacedTime.HasValue)
		{
			if (time - _sellEntryPlacedTime.Value >= lifetime)
			{
				CancelOrder(_sellStopEntryOrder);
				_sellStopEntryOrder = null;
				_sellEntryPlacedTime = null;
				_allowShortEntry = true;
			}
		}
	}

	private bool IsTradingAllowed(DateTimeOffset time)
	{
		if (!UseTradingHours)
		return true;

		var hour = time.Hour;

		return time.DayOfWeek switch
		{
			DayOfWeek.Monday => hour >= MondayStartHour,
			DayOfWeek.Tuesday => TradeTuesday,
			DayOfWeek.Wednesday => TradeWednesday,
			DayOfWeek.Thursday => TradeThursday,
			DayOfWeek.Friday => hour <= FridayStopHour,
			_ => false,
		};
	}

	private void PlaceEntryOrders(decimal channelWidth, DateTimeOffset time)
	{
		if (_allowLongEntry)
		PlaceBuyStopOrder(channelWidth, time);

		if (_allowShortEntry)
		PlaceSellStopOrder(channelWidth, time);
	}

	private void PlaceBuyStopOrder(decimal channelWidth, DateTimeOffset time)
	{
		if (_buyStopEntryOrder != null && _buyStopEntryOrder.State == OrderStates.Active)
		return;

		var entryPrice = _channelHigh;
		var stopPrice = entryPrice - (channelWidth * 2m);
		var takePrice = entryPrice + channelWidth;

		if (stopPrice <= 0m || stopPrice >= entryPrice || takePrice <= entryPrice)
		return;

		var volume = GetEntryVolume(channelWidth);

		if (volume <= 0m)
		return;

		_buyStopEntryOrder = BuyStop(volume, entryPrice);

		if (_buyStopEntryOrder == null)
		return;

		_buyEntryPlacedTime = time;
		_plannedLongEntry = entryPrice;
		_plannedLongStop = stopPrice;
		_plannedLongTake = takePrice;
		_longBreakevenApplied = false;
		_allowLongEntry = false;
	}

	private void PlaceSellStopOrder(decimal channelWidth, DateTimeOffset time)
	{
		if (_sellStopEntryOrder != null && _sellStopEntryOrder.State == OrderStates.Active)
		return;

		var entryPrice = _channelLow;
		var stopPrice = entryPrice + (channelWidth * 2m);
		var takePrice = entryPrice - channelWidth;

		if (stopPrice <= entryPrice || takePrice >= entryPrice)
		return;

		var volume = GetEntryVolume(channelWidth);

		if (volume <= 0m)
		return;

		_sellStopEntryOrder = SellStop(volume, entryPrice);

		if (_sellStopEntryOrder == null)
		return;

		_sellEntryPlacedTime = time;
		_plannedShortEntry = entryPrice;
		_plannedShortStop = stopPrice;
		_plannedShortTake = takePrice;
		_shortBreakevenApplied = false;
		_allowShortEntry = false;
	}

	private decimal GetEntryVolume(decimal channelWidth)
	{
		var stopDistance = channelWidth * 2m;

		if (stopDistance <= 0m)
		return 0m;

		decimal volume;

		if (UseMoneyManagement)
		{
			volume = CalculateRiskVolume(stopDistance);
		}
		else
		{
			volume = FixedVolume;
		}

		if (_lossCount == 1)
		volume = FixedVolume * 4m;

		return volume;
	}

	private decimal CalculateRiskVolume(decimal stopDistance)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return FixedVolume;

		var ticksCount = stopDistance / priceStep;

		if (ticksCount <= 0m)
		return FixedVolume;

		var riskPerUnit = ticksCount * stepPrice;

		if (riskPerUnit <= 0m)
		return FixedVolume;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;

		if (portfolioValue <= 0m)
		return FixedVolume;

		var riskAmount = portfolioValue * (RiskPercent / 100m);

		if (riskAmount <= 0m)
		return FixedVolume;

		var volume = riskAmount / riskPerUnit;

		return volume <= 0m ? FixedVolume : volume;
	}

	private void ApplyBreakeven(decimal lastPrice)
	{
		if (Position > 0 && _longStopLossOrder != null && _longStopLossOrder.State == OrderStates.Active)
		{
			if (_plannedLongEntry > 0m && _plannedLongTake > _plannedLongEntry && _plannedLongStop < _plannedLongEntry)
			{
				var level = _plannedLongEntry + (_plannedLongTake - _plannedLongEntry) * FiboTrail;

				if (lastPrice > level && !_longBreakevenApplied)
				MoveLongStopToBreakeven();
			}
		}
		else if (Position < 0 && _shortStopLossOrder != null && _shortStopLossOrder.State == OrderStates.Active)
		{
			if (_plannedShortEntry > 0m && _plannedShortTake < _plannedShortEntry && _plannedShortStop > _plannedShortEntry)
			{
				var level = _plannedShortEntry - (_plannedShortEntry - _plannedShortTake) * FiboTrail;

				if (lastPrice < level && !_shortBreakevenApplied)
				MoveShortStopToBreakeven();
			}
		}
	}

	private void MoveLongStopToBreakeven()
	{
		if (_longStopLossOrder == null)
		return;

		var volume = Math.Abs(Position);

		if (volume <= 0m)
		return;

		CancelOrderSafe(ref _longStopLossOrder);
		_longStopLossOrder = SellStop(volume, _plannedLongEntry);
		_plannedLongStop = _plannedLongEntry;
		_longBreakevenApplied = true;
	}

	private void MoveShortStopToBreakeven()
	{
		if (_shortStopLossOrder == null)
		return;

		var volume = Math.Abs(Position);

		if (volume <= 0m)
		return;

		CancelOrderSafe(ref _shortStopLossOrder);
		_shortStopLossOrder = BuyStop(volume, _plannedShortEntry);
		_plannedShortStop = _plannedShortEntry;
		_shortBreakevenApplied = true;
	}

	private void CancelEntryOrders()
	{
		CancelOrderSafe(ref _buyStopEntryOrder);
		CancelOrderSafe(ref _sellStopEntryOrder);
		_buyEntryPlacedTime = null;
		_sellEntryPlacedTime = null;
		_plannedLongEntry = 0m;
		_plannedLongStop = 0m;
		_plannedLongTake = 0m;
		_plannedShortEntry = 0m;
		_plannedShortStop = 0m;
		_plannedShortTake = 0m;
	}

	private void CancelOrderSafe(ref Order? order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private void CancelProtectionOrders()
	{
		CancelOrderSafe(ref _longStopLossOrder);
		CancelOrderSafe(ref _longTakeProfitOrder);
		CancelOrderSafe(ref _shortStopLossOrder);
		CancelOrderSafe(ref _shortTakeProfitOrder);
		_longBreakevenApplied = false;
		_shortBreakevenApplied = false;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0 && delta > 0)
		{
			CancelOrderSafe(ref _buyStopEntryOrder);
			CancelOrderSafe(ref _sellStopEntryOrder);
			_buyEntryPlacedTime = null;
			_sellEntryPlacedTime = null;
			ActivateLongProtection();
		}
		else if (Position < 0 && delta < 0)
		{
			CancelOrderSafe(ref _sellStopEntryOrder);
			CancelOrderSafe(ref _buyStopEntryOrder);
			_sellEntryPlacedTime = null;
			_buyEntryPlacedTime = null;
			ActivateShortProtection();
		}
		else if (Position == 0)
		{
			CancelProtectionOrders();
			EvaluateLastTradeResult();
		}
	}

	private void ActivateLongProtection()
	{
		CancelProtectionOrders();

		var volume = Math.Abs(Position);

		if (volume <= 0m)
		return;

		if (_plannedLongStop > 0m && _plannedLongStop < _plannedLongEntry)
		_longStopLossOrder = SellStop(volume, _plannedLongStop);

		if (_plannedLongTake > _plannedLongEntry)
		_longTakeProfitOrder = SellLimit(volume, _plannedLongTake);

		_longBreakevenApplied = false;
	}

	private void ActivateShortProtection()
	{
		CancelProtectionOrders();

		var volume = Math.Abs(Position);

		if (volume <= 0m)
		return;

		if (_plannedShortStop > _plannedShortEntry)
		_shortStopLossOrder = BuyStop(volume, _plannedShortStop);

		if (_plannedShortTake < _plannedShortEntry)
		_shortTakeProfitOrder = BuyLimit(volume, _plannedShortTake);

		_shortBreakevenApplied = false;
	}

	private void EvaluateLastTradeResult()
	{
		var realizedPnL = PnL;
		var tradePnL = realizedPnL - _lastClosedPnL;
		_lossCount = tradePnL < 0m ? 1 : 0;
		_lastClosedPnL = realizedPnL;
	}
}
