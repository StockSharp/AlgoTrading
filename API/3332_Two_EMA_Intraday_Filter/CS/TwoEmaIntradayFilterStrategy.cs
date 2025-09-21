using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two EMA crossover strategy with ATR based order placement and intraday time filtering.
/// </summary>
public class TwoEmaIntradayFilterStrategy : Strategy
{
	private sealed class PendingOrderInfo
	{
		public int ExpirationBar { get; init; }
		public decimal StopPrice { get; init; }
		public decimal TakePrice { get; init; }
		public bool IsLong { get; init; }
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _limitMultiplier;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<int> _expirationBars;
	private readonly StrategyParam<int> _goodMinuteOfHour;
	private readonly StrategyParam<long> _badMinutesMask;
	private readonly StrategyParam<int> _goodHourOfDay;
	private readonly StrategyParam<int> _badHoursMask;
	private readonly StrategyParam<int> _goodDayOfWeek;
	private readonly StrategyParam<int> _badDaysMask;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private AverageTrueRange _atr;

	private readonly Dictionary<Order, PendingOrderInfo> _pendingOrders = new();

	private decimal _previousDelta;
	private bool _hasPreviousDelta;
	private int _currentBarIndex;

	private Order _pendingBuyOrder;
	private Order _pendingSellOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="TwoEmaIntradayFilterStrategy"/> class.
	/// </summary>
	public TwoEmaIntradayFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Period for the fast EMA", "EMA")
		.SetCanOptimize(true);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Period for the slow EMA", "EMA")
		.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Period for ATR calculation", "ATR")
		.SetCanOptimize(true);

		_limitMultiplier = Param(nameof(LimitMultiplier), 1.2m)
		.SetGreaterThanZero()
		.SetDisplay("Limit Multiplier", "ATR multiplier that offsets limit prices", "Orders")
		.SetCanOptimize(true);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Stop-Loss Multiplier", "ATR multiplier for stop-loss placement", "Orders")
		.SetCanOptimize(true);

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 8m)
		.SetGreaterThanZero()
		.SetDisplay("Take-Profit Multiplier", "ATR multiplier for take-profit placement", "Orders")
		.SetCanOptimize(true);

		_expirationBars = Param(nameof(ExpirationBars), 4)
		.SetDisplay("Expiration Bars", "Number of bars before cancelling pending orders", "Orders")
		.SetCanOptimize(true);

		_goodMinuteOfHour = Param(nameof(GoodMinuteOfHour), -1)
		.SetDisplay("Allowed Minute", "Specific minute of hour allowed for entries (-1 disables)", "Time Filter");

		_badMinutesMask = Param<long>(nameof(BadMinutesMask), 0)
		.SetDisplay("Blocked Minutes", "Bit mask that disables specific minutes (bit N blocks minute N)", "Time Filter");

		_goodHourOfDay = Param(nameof(GoodHourOfDay), -1)
		.SetDisplay("Allowed Hour", "Specific hour of day allowed for entries (-1 disables)", "Time Filter");

		_badHoursMask = Param(nameof(BadHoursMask), 0)
		.SetDisplay("Blocked Hours", "Bit mask that disables specific hours (bit N blocks hour N)", "Time Filter");

		_goodDayOfWeek = Param(nameof(GoodDayOfWeek), -1)
		.SetDisplay("Allowed Day", "Specific day of week allowed for entries (-1 disables, 0=Sunday)", "Time Filter");

		_badDaysMask = Param(nameof(BadDaysMask), 0)
		.SetDisplay("Blocked Days", "Bit mask that disables specific week days (bit N blocks day N)", "Time Filter");
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the fast EMA.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow EMA.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier used to offset entry prices.
	/// </summary>
	public decimal LimitMultiplier
	{
		get => _limitMultiplier.Value;
		set => _limitMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier used for stop-loss placement.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier used for take-profit placement.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Amount of bars after which pending orders are cancelled.
	/// </summary>
	public int ExpirationBars
	{
		get => _expirationBars.Value;
		set => _expirationBars.Value = value;
	}

	/// <summary>
	/// Specific minute of hour allowed for entries (-1 disables filtering).
	/// </summary>
	public int GoodMinuteOfHour
	{
		get => _goodMinuteOfHour.Value;
		set => _goodMinuteOfHour.Value = value;
	}

	/// <summary>
	/// Bit mask that disables specific minutes (bit N blocks minute N).
	/// </summary>
	public long BadMinutesMask
	{
		get => _badMinutesMask.Value;
		set => _badMinutesMask.Value = value;
	}

	/// <summary>
	/// Specific hour of day allowed for entries (-1 disables filtering).
	/// </summary>
	public int GoodHourOfDay
	{
		get => _goodHourOfDay.Value;
		set => _goodHourOfDay.Value = value;
	}

	/// <summary>
	/// Bit mask that disables specific hours (bit N blocks hour N).
	/// </summary>
	public int BadHoursMask
	{
		get => _badHoursMask.Value;
		set => _badHoursMask.Value = value;
	}

	/// <summary>
	/// Specific day of week allowed for entries (-1 disables filtering, 0=Sunday).
	/// </summary>
	public int GoodDayOfWeek
	{
		get => _goodDayOfWeek.Value;
		set => _goodDayOfWeek.Value = value;
	}

	/// <summary>
	/// Bit mask that disables specific week days (bit N blocks day N, 0=Sunday).
	/// </summary>
	public int BadDaysMask
	{
		get => _badDaysMask.Value;
		set => _badDaysMask.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (FastEmaPeriod >= SlowEmaPeriod)
		throw new InvalidOperationException("Slow EMA period must be greater than fast EMA period.");

		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		_pendingOrders.Clear();
		_previousDelta = 0m;
		_hasPreviousDelta = false;
		_currentBarIndex = 0;

		CancelProtectiveOrders();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastEma, _slowEma, _atr, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_currentBarIndex++;

		CancelExpiredOrders();

		var time = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		if (!IsTimeAllowed(time))
		{
			_previousDelta = fastEmaValue - slowEmaValue;
			_hasPreviousDelta = true;
			return;
		}

		var delta = fastEmaValue - slowEmaValue;

		if (!_hasPreviousDelta)
		{
			_previousDelta = delta;
			_hasPreviousDelta = true;
			return;
		}

		var spread = GetSpread();

		if (delta > 0 && _previousDelta <= 0)
		TryPlaceBuyOrder(slowEmaValue, atrValue, spread);
		else if (delta < 0 && _previousDelta >= 0)
		TryPlaceSellOrder(slowEmaValue, atrValue);

		if (Position == 0)
		CancelProtectiveOrders();

		_previousDelta = delta;
	}

	private bool IsTimeAllowed(DateTimeOffset time)
	{
		if (GoodMinuteOfHour >= 0 && time.Minute != GoodMinuteOfHour)
		return false;

		if (IsBitSet(BadMinutesMask, time.Minute))
		return false;

		if (GoodHourOfDay >= 0 && time.Hour != GoodHourOfDay)
		return false;

		if (IsBitSet(BadHoursMask, time.Hour))
		return false;

		var dayIndex = (int)time.DayOfWeek;
		if (GoodDayOfWeek >= 0 && dayIndex != GoodDayOfWeek)
		return false;

		if (IsBitSet(BadDaysMask, dayIndex))
		return false;

		return true;
	}

	private static bool IsBitSet(long mask, int bitIndex)
	=> bitIndex >= 0 && bitIndex < 63 && (mask & (1L << bitIndex)) != 0;

	private static bool IsBitSet(int mask, int bitIndex)
	=> bitIndex >= 0 && bitIndex < 31 && (mask & (1 << bitIndex)) != 0;

	private decimal GetSpread()
	{
		if (Security is null)
		return 0m;

		var bid = Security.BestBid?.Price ?? 0m;
		var ask = Security.BestAsk?.Price ?? 0m;

		if (bid <= 0m || ask <= 0m || ask <= bid)
		return 0m;

		return ask - bid;
	}

	private void TryPlaceBuyOrder(decimal slowEmaValue, decimal atrValue, decimal spread)
	{
		if (Volume <= 0)
		{
			this.LogWarning("Volume must be greater than zero to place orders.");
			return;
		}

		if (_pendingBuyOrder != null && _pendingBuyOrder.State == OrderStates.Active)
		CancelOrder(_pendingBuyOrder);

		var price = slowEmaValue - LimitMultiplier * atrValue + spread;
		var stopPrice = price + StopLossMultiplier * atrValue;
		var takePrice = price - TakeProfitMultiplier * atrValue;

		_pendingBuyOrder = BuyLimit(Volume, price);
		_pendingOrders[_pendingBuyOrder] = new PendingOrderInfo
		{
			ExpirationBar = ExpirationBars > 0 ? _currentBarIndex + ExpirationBars : int.MaxValue,
			StopPrice = stopPrice,
			TakePrice = takePrice,
			IsLong = true
		};

		if (_pendingSellOrder != null && _pendingSellOrder.State == OrderStates.Active)
		CancelOrder(_pendingSellOrder);
	}

	private void TryPlaceSellOrder(decimal slowEmaValue, decimal atrValue)
	{
		if (Volume <= 0)
		{
			this.LogWarning("Volume must be greater than zero to place orders.");
			return;
		}

		if (_pendingSellOrder != null && _pendingSellOrder.State == OrderStates.Active)
		CancelOrder(_pendingSellOrder);

		var price = slowEmaValue + LimitMultiplier * atrValue;
		var stopPrice = price + StopLossMultiplier * atrValue;
		var takePrice = price - TakeProfitMultiplier * atrValue;

		_pendingSellOrder = SellLimit(Volume, price);
		_pendingOrders[_pendingSellOrder] = new PendingOrderInfo
		{
			ExpirationBar = ExpirationBars > 0 ? _currentBarIndex + ExpirationBars : int.MaxValue,
			StopPrice = stopPrice,
			TakePrice = takePrice,
			IsLong = false
		};

		if (_pendingBuyOrder != null && _pendingBuyOrder.State == OrderStates.Active)
		CancelOrder(_pendingBuyOrder);
	}

	private void CancelExpiredOrders()
	{
		if (_pendingOrders.Count == 0)
		return;

		var expired = new List<Order>();

		foreach (var pair in _pendingOrders)
		{
			if (_currentBarIndex >= pair.Value.ExpirationBar)
			expired.Add(pair.Key);
		}

		foreach (var order in expired)
		{
			CancelOrder(order);
			_pendingOrders.Remove(order);
		}
	}

	private void CancelProtectiveOrders()
	{
		if (_stopLossOrder != null && _stopLossOrder.State == OrderStates.Active)
		CancelOrder(_stopLossOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		CancelOrder(_takeProfitOrder);
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order is null)
		return;

		if (_pendingOrders.ContainsKey(order) && order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		_pendingOrders.Remove(order);

		if (order == _pendingBuyOrder && order.State != OrderStates.Active)
		_pendingBuyOrder = null;

		if (order == _pendingSellOrder && order.State != OrderStates.Active)
		_pendingSellOrder = null;

		if ((order == _stopLossOrder || order == _takeProfitOrder) && order.State != OrderStates.Active)
		{
			if (order == _stopLossOrder)
			_stopLossOrder = null;

			if (order == _takeProfitOrder)
			_takeProfitOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		if (order is null)
		return;

		if (!_pendingOrders.TryGetValue(order, out var info))
		return;

		if (order.State != OrderStates.Done && order.State != OrderStates.PartiallyFilled)
		return;

		_pendingOrders.Remove(order);

		CancelProtectiveOrders();

		if (info.IsLong && Position > 0)
		{
			_stopLossOrder = SellStop(Position, info.StopPrice);
			_takeProfitOrder = SellLimit(Position, info.TakePrice);
		}
		else if (!info.IsLong && Position < 0)
		{
			_stopLossOrder = BuyStop(-Position, info.StopPrice);
			_takeProfitOrder = BuyLimit(-Position, info.TakePrice);
		}
	}
}
