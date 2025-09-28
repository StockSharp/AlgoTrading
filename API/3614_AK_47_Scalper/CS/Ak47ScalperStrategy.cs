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
/// Pending stop scalping strategy converted from the MetaTrader expert "AK-47 Scalper" (build 44883).
/// The algorithm keeps a single sell stop order active during the trading window, attaches protective
/// orders once the entry is filled and trails the stop loss together with the pending order price.
/// </summary>
public class Ak47ScalperStrategy : Strategy
{
	private readonly StrategyParam<bool> _useVolumePercent;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _userLot;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<DataType> _candleType;

	private Order _entryOrder;
	private Order _stopOrder;
	private Order _takeProfitOrder;

	private decimal _pipSize;
	private decimal _tickSize;
	private decimal _halfStopDistance;

	private decimal? _plannedStopPrice;
	private decimal? _plannedTakePrice;

	/// <summary>
	/// Initializes strategy parameters with defaults taken from the original expert advisor.
	/// </summary>
	public Ak47ScalperStrategy()
	{
		_useVolumePercent = Param(nameof(UseVolumePercent), true)
			.SetDisplay("Use Risk Percent", "Size orders using account percent instead of fixed lot", "Risk")
			.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 3m)
			.SetDisplay("Risk Percent", "Risk percentage applied to account equity when sizing orders", "Risk")
			.SetCanOptimize(true);

		_userLot = Param(nameof(UserLot), 0.01m)
			.SetDisplay("Base Lot", "Minimal lot used when sizing and as fallback volume", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 3.5m)
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 7m)
			.SetDisplay("Take Profit (pips)", "Distance of the profit target (0 disables)", "Risk")
			.SetCanOptimize(true);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 5m)
			.SetDisplay("Max Spread (points)", "Maximum allowed spread expressed in MetaTrader points", "Filters")
			.SetCanOptimize(true);

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Restrict trading to the configured time window", "Schedule")
			.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 2)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour when trading becomes active", "Schedule")
			.SetCanOptimize(true);

		_startMinute = Param(nameof(StartMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Minute when trading becomes active", "Schedule")
			.SetCanOptimize(true);

		_endHour = Param(nameof(EndHour), 21)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Hour when trading stops", "Schedule")
			.SetCanOptimize(true);

		_endMinute = Param(nameof(EndMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("End Minute", "Minute when trading stops", "Schedule")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for timing and price updates", "General");
	}

	/// <summary>
	/// Enable position sizing through the <see cref="RiskPercent"/> parameter.
	/// </summary>
	public bool UseVolumePercent
	{
		get => _useVolumePercent.Value;
		set => _useVolumePercent.Value = value;
	}

	/// <summary>
	/// Percent of account value converted into volume when <see cref="UseVolumePercent"/> is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed lot size used as fallback and as rounding step when sizing orders.
	/// </summary>
	public decimal UserLot
	{
		get => _userLot.Value;
		set => _userLot.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set
		{
			_stopLossPips.Value = value;
			UpdateDistances();
		}
	}

	/// <summary>
	/// Take-profit distance expressed in pips (zero disables the target).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set
		{
			_takeProfitPips.Value = value;
			UpdateDistances();
		}
	}

	/// <summary>
	/// Maximum allowed spread in MetaTrader points before new orders are placed.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Enable trading only during the configured time interval.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Hour when trading becomes active.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Minute when trading becomes active.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Hour when trading stops.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Minute when trading stops.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryOrder = null;
		_stopOrder = null;
		_takeProfitOrder = null;
		_plannedStopPrice = null;
		_plannedTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = CalculateTickSize();
		_pipSize = CalculatePipSize();
		UpdateDistances();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(OnProcessCandle).Start();
	}

	private void UpdateDistances()
	{
		var distance = StopLossPips * _pipSize;
		_halfStopDistance = distance / 2m;
	}

	private decimal CalculateTickSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step > 0m)
			return step;

		var decimals = security.Decimals ?? 4;
		return (decimal)Math.Pow(10, -decimals);
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals;

		if (decimals is 3 or 5)
			return step * 10m;

		return step;
	}

	private void OnProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bestBid = Security.BestBid?.Price ?? candle.ClosePrice;
		var bestAsk = Security.BestAsk?.Price ?? candle.ClosePrice;

		UpdatePendingOrder(bestBid, bestAsk);
		UpdateProtectiveOrders(bestBid);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!UseTimeFilter || IsWithinTradingWindow(candle.OpenTime))
		{
			if (!HasActiveEntryOrPosition() && IsSpreadAllowed(bestBid, bestAsk))
				TryPlaceEntry(bestBid, bestAsk);
		}
	}

	private void UpdatePendingOrder(decimal bestBid, decimal bestAsk)
	{
		if (_entryOrder == null)
			return;

		if (_entryOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_entryOrder = null;
			return;
		}

		if (_halfStopDistance <= 0m)
			return;

		var desiredPrice = NormalizePrice(bestBid - _halfStopDistance);
		if (desiredPrice <= 0m)
			return;

		var desiredStop = NormalizePrice(bestAsk + _halfStopDistance);
		var desiredTake = TakeProfitPips > 0m
			? NormalizePrice(desiredPrice - TakeProfitPips * _pipSize)
			: null;

		if (_entryOrder.Price != desiredPrice)
			ReRegisterOrder(_entryOrder, desiredPrice, _entryOrder.Volume);

		_plannedStopPrice = desiredStop;
		_plannedTakePrice = desiredTake;
	}

	private void UpdateProtectiveOrders(decimal bestBid)
	{
		if (Position >= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var volume = Math.Abs(Position);

		var stopDistance = StopLossPips * _pipSize;
		if (stopDistance > 0m)
		{
			var desiredStop = NormalizePrice(bestBid + stopDistance);
			UpdateStopOrder(desiredStop, volume);
		}
		else
		{
			CancelProtectiveOrder(ref _stopOrder);
		}

		if (TakeProfitPips > 0m)
		{
			var target = _plannedTakePrice ?? (PositionPrice != 0m
				? NormalizePrice(PositionPrice - TakeProfitPips * _pipSize)
				: (decimal?)null);

			if (target != null)
				UpdateTakeProfitOrder(target.Value, volume);
		}
		else
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
		}
	}

	private void TryPlaceEntry(decimal bestBid, decimal bestAsk)
	{
		if (_halfStopDistance <= 0m)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		OrderVolume = volume;

		var entryPrice = NormalizePrice(bestBid - _halfStopDistance);
		if (entryPrice <= 0m)
			return;

		var stopPrice = NormalizePrice(bestAsk + _halfStopDistance);
		var takePrice = TakeProfitPips > 0m
			? NormalizePrice(entryPrice - TakeProfitPips * _pipSize)
			: null;

		_entryOrder = SellStop(volume, entryPrice);
		_plannedStopPrice = stopPrice;
		_plannedTakePrice = takePrice;
	}

	private decimal CalculateOrderVolume()
	{
		var security = Security;
		if (security == null)
			return 0m;

		decimal volume;

		if (!UseVolumePercent)
		{
			volume = UserLot;
		}
		else
		{
			var portfolio = Portfolio;
			var balance = portfolio?.CurrentValue ?? portfolio?.CurrentBalance ?? 0m;
			if (balance <= 0m)
				return AlignVolume(UserLot);

			var raw = RiskPercent * balance / 100000m;

			if (UserLot > 0m)
			{
				var units = Math.Floor(raw / UserLot);
				raw = units * UserLot;
			}

			if (raw <= 0m)
				raw = UserLot;

			volume = raw;
		}

		return AlignVolume(volume);
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var min = security.VolumeMin ?? 0m;
			var max = security.VolumeMax ?? decimal.MaxValue;
			var step = security.VolumeStep ?? 0m;

			if (step > 0m && volume > 0m)
				volume = Math.Floor(volume / step) * step;

			if (volume <= 0m)
				volume = min > 0m ? min : UserLot;

			if (min > 0m && volume < min)
				volume = min;

			if (max > 0m && volume > max)
				volume = max;
		}

		if (volume <= 0m && UserLot > 0m)
			volume = UserLot;

		return volume;
	}

	private bool IsSpreadAllowed(decimal bestBid, decimal bestAsk)
	{
		if (MaxSpreadPoints <= 0m || _tickSize <= 0m)
			return true;

		var spreadPoints = (bestAsk - bestBid) / _tickSize;
		return spreadPoints <= MaxSpreadPoints;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var current = time.TimeOfDay;

		if (start == end)
			return true;

		return start < end
			? current >= start && current < end
			: current >= start || current < end;
	}

	private bool HasActiveEntryOrPosition()
	{
		if (Position != 0m)
			return true;

		return _entryOrder != null && _entryOrder.State == OrderStates.Active;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return Math.Round(price, 5);

		return security.ShrinkPrice(price);
	}

	private void UpdateStopOrder(decimal? desiredPrice, decimal volume)
	{
		if (desiredPrice == null || desiredPrice <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			return;
		}

		if (_stopOrder == null)
		{
			_stopOrder = BuyStop(volume, desiredPrice.Value);
			return;
		}

		if (_stopOrder.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			_stopOrder = null;
			UpdateStopOrder(desiredPrice, volume);
			return;
		}

		var target = desiredPrice.Value;
		if (_stopOrder.Price - target > _tickSize / 2m || _stopOrder.Volume != volume)
			ReRegisterOrder(_stopOrder, target, volume);
	}

	private void UpdateTakeProfitOrder(decimal desiredPrice, decimal volume)
	{
		if (_takeProfitOrder == null)
		{
			_takeProfitOrder = BuyLimit(volume, desiredPrice);
			return;
		}

		if (_takeProfitOrder.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			_takeProfitOrder = null;
			UpdateTakeProfitOrder(desiredPrice, volume);
			return;
		}

		if (_takeProfitOrder.Price != desiredPrice || _takeProfitOrder.Volume != volume)
			ReRegisterOrder(_takeProfitOrder, desiredPrice, volume);
	}

	private void CancelProtectiveOrder(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		if (trade.Order == _entryOrder)
		{
			_entryOrder = null;

			var volume = Math.Abs(Position);
			var stopPrice = _plannedStopPrice ?? NormalizePrice(trade.Trade.Price + StopLossPips * _pipSize);
			var takePrice = _plannedTakePrice ?? (TakeProfitPips > 0m
				? NormalizePrice(trade.Trade.Price - TakeProfitPips * _pipSize)
				: (decimal?)null);

			UpdateStopOrder(stopPrice, volume);

			if (takePrice != null)
				UpdateTakeProfitOrder(takePrice.Value, volume);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			_plannedStopPrice = null;
			_plannedTakePrice = null;
		}
	}
}

