using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// News trading strategy that brackets the price with pending stop orders around a scheduled event.
/// Orders are placed shortly before the release, cancelled after the window ends, and the open position
/// is managed with optional stop-loss, take-profit, break-even and trailing stop rules.
/// </summary>
public class NewsReleaseStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _newsTime;
	private readonly StrategyParam<int> _preNewsMinutes;
	private readonly StrategyParam<int> _postNewsMinutes;
	private readonly StrategyParam<int> _orderPairs;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _tradeOnce;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailingPips;
	private readonly StrategyParam<bool> _closeAfterEvent;

	private bool _ordersPlaced;
	private bool _eventProcessed;
	private bool _breakEvenArmed;
	private DateTimeOffset _cancelTime;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _highest;
	private decimal? _lowest;

	/// <summary>
	/// Scheduled news release time.
	/// </summary>
	public DateTimeOffset NewsTime
	{
		get => _newsTime.Value;
		set => _newsTime.Value = value;
	}

	/// <summary>
	/// Minutes before the event when pending orders should be placed.
	/// </summary>
	public int PreNewsMinutes
	{
		get => _preNewsMinutes.Value;
		set => _preNewsMinutes.Value = value;
	}

	/// <summary>
	/// Minutes after the event when pending orders are kept alive.
	/// </summary>
	public int PostNewsMinutes
	{
		get => _postNewsMinutes.Value;
		set => _postNewsMinutes.Value = value;
	}

	/// <summary>
	/// Number of buy stop / sell stop pairs to place.
	/// </summary>
	public int OrderPairs
	{
		get => _orderPairs.Value;
		set => _orderPairs.Value = value;
	}

	/// <summary>
	/// Distance in pips from current price for the first pending orders.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Additional spacing in pips between successive order pairs.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Volume for each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Allow trading only once per event window.
	/// </summary>
	public bool TradeOnce
	{
		get => _tradeOnce.Value;
		set => _tradeOnce.Value = value;
	}

	/// <summary>
	/// Enable stop-loss handling.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enable take-profit handling.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable break-even rule.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance in pips required to arm the break-even rule.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Offset in pips added to the entry price when the break-even rule is armed.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingPips
	{
		get => _trailingPips.Value;
		set => _trailingPips.Value = value;
	}

	/// <summary>
	/// Close any open position when the post-news window ends.
	/// </summary>
	public bool CloseAfterEvent
	{
		get => _closeAfterEvent.Value;
		set => _closeAfterEvent.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="NewsReleaseStrategy"/>.
	/// </summary>
	public NewsReleaseStrategy()
	{
		_newsTime = Param(nameof(NewsTime), DateTimeOffset.Now)
		.SetDisplay("News Time", "Scheduled news release time", "General");

		_preNewsMinutes = Param(nameof(PreNewsMinutes), 5)
		.SetDisplay("Pre News Minutes", "Minutes before news to place orders", "Timing")
		.SetNotNegative();

		_postNewsMinutes = Param(nameof(PostNewsMinutes), 5)
		.SetDisplay("Post News Minutes", "Minutes after news to keep orders", "Timing")
		.SetNotNegative();

		_orderPairs = Param(nameof(OrderPairs), 1)
		.SetDisplay("Order Pairs", "Number of buy/sell stop pairs", "Orders")
		.SetGreaterThanOrEqual(1);

		_distancePips = Param(nameof(DistancePips), 10m)
		.SetDisplay("Distance", "Initial distance in pips", "Orders")
		.SetGreaterThanOrEqual(0m);

		_stepPips = Param(nameof(StepPips), 5m)
		.SetDisplay("Step", "Additional spacing between orders", "Orders")
		.SetGreaterThanOrEqual(0m);

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Volume", "Volume per pending order", "Orders")
		.SetGreaterThanZero();

		_tradeOnce = Param(nameof(TradeOnce), true)
		.SetDisplay("Trade Once", "Trade only once per event", "General");

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", "Enable stop-loss control", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 10m)
		.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
		.SetGreaterThanOrEqual(0m);

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", "Enable take-profit control", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 15m)
		.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
		.SetGreaterThanOrEqual(0m);

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break Even", "Enable break-even protection", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 5m)
		.SetDisplay("Break Even Trigger", "Profit required to arm break-even", "Risk")
		.SetGreaterThanOrEqual(0m);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 1m)
		.SetDisplay("Break Even Offset", "Offset applied when moving to break-even", "Risk");

		_useTrailing = Param(nameof(UseTrailing), false)
		.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingPips = Param(nameof(TrailingPips), 5m)
		.SetDisplay("Trailing Distance", "Trailing distance in pips", "Risk")
		.SetGreaterThanOrEqual(0m);

		_closeAfterEvent = Param(nameof(CloseAfterEvent), false)
		.SetDisplay("Close After Event", "Close position when event window ends", "Risk");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ordersPlaced = false;
		_eventProcessed = false;
		_breakEvenArmed = false;
		_bestBid = null;
		_bestAsk = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_highest = null;
		_lowest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		// Keep track of the current best bid and ask prices.
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_bestAsk = (decimal)ask;

		var now = CurrentTime;

		// Skip logic if trading is disabled or the instrument prices are missing.
		if (!IsFormedAndOnlineAndAllowTrading() || _bestBid is null || _bestAsk is null)
		return;

		// Place pending orders shortly before the news release.
		if (!_ordersPlaced && (!_eventProcessed || !TradeOnce) &&
		now >= NewsTime - TimeSpan.FromMinutes(PreNewsMinutes) &&
		now < NewsTime)
		{
			PlacePendingOrders();
			_cancelTime = NewsTime + TimeSpan.FromMinutes(PostNewsMinutes);
			_ordersPlaced = true;
		}

		// Cancel pending orders after the post-news window.
		if (_ordersPlaced && now > _cancelTime)
		{
			CancelActiveOrders();
			_ordersPlaced = false;

			if (CloseAfterEvent && Position != 0)
			ClosePosition();

			if (TradeOnce)
			_eventProcessed = true;
		}

		// Manage the open position once orders are filled.
		if (Position > 0)
		ManageLongPosition();
		else if (Position < 0)
		ManageShortPosition();
	}

	private void PlacePendingOrders()
	{
		var step = GetStep();
		var baseDistance = DistancePips * step;
		var extraStep = StepPips * step;

		for (var i = 0; i < OrderPairs; i++)
		{
			var offset = baseDistance + extraStep * i;
			var buyPrice = _bestAsk!.Value + offset;
			var sellPrice = _bestBid!.Value - offset;

			// Register symmetric stop orders around the current price.
			BuyStop(OrderVolume, buyPrice);
			SellStop(OrderVolume, sellPrice);
		}
	}

	private void ManageLongPosition()
	{
		if (_bestBid is not decimal bid || _longEntryPrice is not decimal entry)
		return;

		var step = GetStep();
		_highest = _highest is decimal h ? Math.Max(h, bid) : bid;

		// Apply initial stop-loss logic.
		if (UseStopLoss && StopLossPips > 0m && bid <= entry - StopLossPips * step)
		{
			ClosePosition();
			return;
		}

		// Apply take-profit logic when available.
		if (UseTakeProfit && TakeProfitPips > 0m && bid >= entry + TakeProfitPips * step)
		{
			ClosePosition();
			return;
		}

		// Arm the break-even rule once the trigger distance is achieved.
		if (UseBreakEven && !_breakEvenArmed && bid >= entry + BreakEvenTriggerPips * step)
		{
			_breakEvenArmed = true;
		}

		// Enforce the break-even offset after the rule is armed.
		if (_breakEvenArmed && bid <= entry + BreakEvenOffsetPips * step)
		{
			ClosePosition();
			return;
		}

		// Manage trailing stop using the highest traded price.
		if (UseTrailing && TrailingPips > 0m && _highest is decimal high)
		{
			var trailStop = high - TrailingPips * step;
			if (bid <= trailStop)
			ClosePosition();
		}
	}

	private void ManageShortPosition()
	{
		if (_bestAsk is not decimal ask || _shortEntryPrice is not decimal entry)
		return;

		var step = GetStep();
		_lowest = _lowest is decimal l ? Math.Min(l, ask) : ask;

		if (UseStopLoss && StopLossPips > 0m && ask >= entry + StopLossPips * step)
		{
			ClosePosition();
			return;
		}

		if (UseTakeProfit && TakeProfitPips > 0m && ask <= entry - TakeProfitPips * step)
		{
			ClosePosition();
			return;
		}

		if (UseBreakEven && !_breakEvenArmed && ask <= entry - BreakEvenTriggerPips * step)
		{
			_breakEvenArmed = true;
		}

		if (_breakEvenArmed && ask >= entry - BreakEvenOffsetPips * step)
		{
			ClosePosition();
			return;
		}

		if (UseTrailing && TrailingPips > 0m && _lowest is decimal low)
		{
			var trailStop = low + TrailingPips * step;
			if (ask >= trailStop)
			ClosePosition();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Cancel leftover pending orders when a position is opened or closed.
		if (delta != 0)
		CancelActiveOrders();

		if (Position > 0 && delta > 0 && _bestAsk is decimal ask)
		{
			_longEntryPrice = ask;
			_shortEntryPrice = null;
			_highest = _bestBid;
			_lowest = null;
			_breakEvenArmed = false;
		}
		else if (Position < 0 && delta < 0 && _bestBid is decimal bid)
		{
			_shortEntryPrice = bid;
			_longEntryPrice = null;
			_lowest = _bestAsk;
			_highest = null;
			_breakEvenArmed = false;
		}
		else if (Position == 0)
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_highest = null;
			_lowest = null;
			_breakEvenArmed = false;

			if (TradeOnce)
			_eventProcessed = true;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		// Store precise execution price from trade information.
		if (trade.Trade is null)
		return;

		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			_longEntryPrice = price;
			_highest = price;
			_breakEvenArmed = false;
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			_shortEntryPrice = price;
			_lowest = price;
			_breakEvenArmed = false;
		}
	}

	private decimal GetStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}
}
