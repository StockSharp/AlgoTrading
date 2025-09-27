using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// News straddle strategy converted from the SampleDetectEconomicCalendar.mq5 expert advisor.
/// Places symmetric stop orders ahead of high impact calendar events.
/// </summary>
public class SampleDetectEconomicCalendarStrategy : Strategy
{
	private readonly StrategyParam<bool> _tradeNews;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _expiryMinutes;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _buyDistancePoints;
	private readonly StrategyParam<decimal> _sellDistancePoints;
	private readonly StrategyParam<int> _leadMinutes;
	private readonly StrategyParam<int> _postMinutes;
	private readonly StrategyParam<string> _baseCurrency;
	private readonly StrategyParam<string> _calendarDefinition;

	private readonly List<NewsEventState> _events = new();

	private decimal _tickSize;
	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _highestBid;
	private decimal? _lowestAsk;

	/// <summary>
	/// Initializes a new instance of the <see cref="SampleDetectEconomicCalendarStrategy"/> class.
	/// </summary>
	public SampleDetectEconomicCalendarStrategy()
	{
		_tradeNews = Param(nameof(TradeNews), false)
		.SetDisplay("Trade News", "Enable pending orders around calendar events", "General");

		_fixedVolume = Param(nameof(OrderVolume), 0.01m)
		.SetDisplay("Fixed Volume", "Volume used when money management is disabled", "Risk")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 300m)
		.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 900m)
		.SetDisplay("Take Profit", "Take profit distance in points (0 disables)", "Risk")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 200m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in points (0 disables)", "Risk")
		.SetCanOptimize(true);

		_expiryMinutes = Param(nameof(ExpiryMinutes), 1440)
		.SetDisplay("Expiry (min)", "Minutes after release to cancel pending orders", "Orders")
		.SetGreaterThanZero();

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Money Management", "Use balance based position sizing", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 3m)
		.SetDisplay("Risk %", "Percentage of portfolio risked per trade", "Risk")
		.SetCanOptimize(true);

		_buyDistancePoints = Param(nameof(BuyDistancePoints), 400m)
		.SetDisplay("Buy Distance", "Distance above ask for buy stop (points)", "Orders")
		.SetCanOptimize(true);

		_sellDistancePoints = Param(nameof(SellDistancePoints), 400m)
		.SetDisplay("Sell Distance", "Distance below bid for sell stop (points)", "Orders")
		.SetCanOptimize(true);

		_leadMinutes = Param(nameof(LeadMinutes), 5)
		.SetDisplay("Lead Minutes", "Minutes before release to place orders", "Schedule")
		.SetGreaterThanZero();

		_postMinutes = Param(nameof(PostMinutes), 10)
		.SetDisplay("Post Minutes", "Minutes after release before cancellation", "Schedule")
		.SetGreaterThanZero();

		_baseCurrency = Param(nameof(BaseCurrency), "USD")
		.SetDisplay("Base Currency", "Currency code to trade", "Schedule");

		_calendarDefinition = Param(nameof(CalendarDefinition), string.Empty)
		.SetDisplay("Calendar", "Calendar events in 'yyyy-MM-dd HH:mm;CUR;High;Title' format", "Schedule");
	}

	/// <summary>
	/// Enable pending order placement around news releases.
	/// </summary>
	public bool TradeNews
	{
		get => _tradeNews.Value;
		set => _tradeNews.Value = value;
	}

	/// <summary>
	/// Fixed volume used when money management is disabled.
	/// </summary>
	public decimal OrderVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minutes after release used to cancel pending orders.
	/// </summary>
	public int ExpiryMinutes
	{
		get => _expiryMinutes.Value;
		set => _expiryMinutes.Value = value;
	}

	/// <summary>
	/// Enable balance based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio to risk when money management is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Distance above the ask used for the buy stop order.
	/// </summary>
	public decimal BuyDistancePoints
	{
		get => _buyDistancePoints.Value;
		set => _buyDistancePoints.Value = value;
	}

	/// <summary>
	/// Distance below the bid used for the sell stop order.
	/// </summary>
	public decimal SellDistancePoints
	{
		get => _sellDistancePoints.Value;
		set => _sellDistancePoints.Value = value;
	}

	/// <summary>
	/// Minutes before the news release when orders are submitted.
	/// </summary>
	public int LeadMinutes
	{
		get => _leadMinutes.Value;
		set => _leadMinutes.Value = value;
	}

	/// <summary>
	/// Minutes after the news release before unattended orders are cancelled.
	/// </summary>
	public int PostMinutes
	{
		get => _postMinutes.Value;
		set => _postMinutes.Value = value;
	}

	/// <summary>
	/// Currency code that must be present in the calendar event.
	/// </summary>
	public string BaseCurrency
	{
		get => _baseCurrency.Value;
		set => _baseCurrency.Value = value;
	}

	/// <summary>
	/// Calendar events definition.
	/// </summary>
	public string CalendarDefinition
	{
		get => _calendarDefinition.Value;
		set => _calendarDefinition.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return Security is null
		? Array.Empty<(Security, DataType)>()
		: new[] { (Security, DataType.Level1) };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 0.0001m;

		ParseCalendarDefinition();

		if (TradeNews && _events.Count == 0)
		LogWarning("Trading around news is enabled but the calendar list is empty.");

		StartProtection(
		takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * _tickSize, UnitTypes.Absolute) : null,
		stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * _tickSize, UnitTypes.Absolute) : null,
		useMarketOrders: true);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_events.Clear();
		_lastBid = null;
		_lastAsk = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_highestBid = null;
		_lowestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order is null)
		return;

		foreach (var evt in _events)
		{
			if (evt.Completed)
			continue;

			if (evt.BuyOrder?.Id == trade.Order.Id)
			{
				CancelOrderSafe(evt.SellOrder);
				evt.SellOrder = null;
				evt.Completed = true;
			}
			else if (evt.SellOrder?.Id == trade.Order.Id)
			{
				CancelOrderSafe(evt.BuyOrder);
				evt.BuyOrder = null;
				evt.Completed = true;
			}
		}

		if (Position > 0)
		{
			_longEntryPrice = trade.Trade.Price;
			_highestBid = _lastBid;
			_shortEntryPrice = null;
			_lowestAsk = null;
		}
		else if (Position < 0)
		{
			_shortEntryPrice = trade.Trade.Price;
			_lowestAsk = _lastAsk;
			_longEntryPrice = null;
			_highestBid = null;
		}
		else
		{
			ResetTrailing();
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		_lastBid = (decimal)bidObj;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		_lastAsk = (decimal)askObj;

		if (TradeNews)
		ProcessCalendar(level1.ServerTime);

		UpdateTrailing();
	}

	private void ProcessCalendar(DateTimeOffset now)
	{
		if (_events.Count == 0 || _lastBid is null || _lastAsk is null)
		return;

		var lead = TimeSpan.FromMinutes(LeadMinutes);
		var cancelDelay = TimeSpan.FromMinutes(PostMinutes);
		var expiryDelay = TimeSpan.FromMinutes(ExpiryMinutes);

		foreach (var evt in _events)
		{
			if (evt.Completed)
			continue;

			if (!evt.OrdersPlaced)
			{
				if (now >= evt.Time - lead && now < evt.Time)
				TryPlaceEventOrders(evt);
			}
			else
			{
				if (evt.CancelAfter is null)
				{
					if (now >= evt.Time)
					evt.CancelAfter = now + cancelDelay;
				}
				else if (now >= evt.CancelAfter || now >= evt.Time + expiryDelay)
				{
					CancelEventOrders(evt);
					evt.Completed = true;
				}
			}
		}
	}

	private void TryPlaceEventOrders(NewsEventState evt)
	{
		if (!evt.Currency.EqualsIgnoreCase(BaseCurrency))
		return;

		if (evt.Importance != NewsImportance.High)
		return;

		if (BuyDistancePoints <= 0 || SellDistancePoints <= 0)
		return;

		var volume = CalculateOrderVolume();
		if (volume <= 0)
		return;

		var buyPrice = _lastAsk!.Value + BuyDistancePoints * _tickSize;
		var sellPrice = _lastBid!.Value - SellDistancePoints * _tickSize;

		evt.BuyOrder = BuyStop(volume, buyPrice);
		evt.SellOrder = SellStop(volume, sellPrice);
		evt.OrdersPlaced = true;

		LogInfo($"Placed pending orders for '{evt.Title}' at {evt.Time:O}. Volume={volume:F2}.");
	}

	private decimal CalculateOrderVolume()
	{
		if (!UseMoneyManagement || StopLossPoints <= 0)
		return OrderVolume;

		if (Portfolio is null || Security is null)
		return OrderVolume;

		var balance = Portfolio.CurrentValue;
		if (balance <= 0)
		return OrderVolume;

		var price = _lastAsk ?? _lastBid ?? 0m;
		if (price <= 0)
		return OrderVolume;

		var step = Security.PriceStep ?? 0.0001m;
		var stopDistance = StopLossPoints * step;
		if (stopDistance <= 0)
		return OrderVolume;

		var riskAmount = balance * RiskPercent / 100m;
		if (riskAmount <= 0)
		return OrderVolume;

		var rawVolume = riskAmount / (stopDistance * price);

		var minVolume = Security.MinVolume ?? 0.0m;
		var maxVolume = Security.MaxVolume ?? 0.0m;
		var stepVolume = Security.VolumeStep ?? 0.0m;

		if (stepVolume > 0)
		rawVolume = Math.Round(rawVolume / stepVolume) * stepVolume;

		if (minVolume > 0)
		rawVolume = Math.Max(rawVolume, minVolume);

		if (maxVolume > 0)
		rawVolume = Math.Min(rawVolume, maxVolume);

		return rawVolume > 0 ? rawVolume : OrderVolume;
	}

	private void UpdateTrailing()
	{
		var stopStep = StopLossPoints * _tickSize;
		var takeStep = TakeProfitPoints * _tickSize;
		var trailStep = TrailingStopPoints * _tickSize;

		if (Position > 0 && _lastBid is decimal bid)
		{
			_highestBid = _highestBid.HasValue ? Math.Max(_highestBid.Value, bid) : bid;

			if (_longEntryPrice is decimal entry)
			{
				if (StopLossPoints > 0 && bid <= entry - stopStep)
				{
					ClosePosition();
					ResetTrailing();
					return;
				}

				if (TakeProfitPoints > 0 && bid >= entry + takeStep)
				{
					ClosePosition();
					ResetTrailing();
					return;
				}

				if (TrailingStopPoints > 0 && _highestBid is decimal high)
				{
					var stopLevel = high - trailStep;
					if (bid <= stopLevel)
					{
						ClosePosition();
						ResetTrailing();
					}
				}
			}
		}
		else if (Position < 0 && _lastAsk is decimal ask)
		{
			_lowestAsk = _lowestAsk.HasValue ? Math.Min(_lowestAsk.Value, ask) : ask;

			if (_shortEntryPrice is decimal entry)
			{
				if (StopLossPoints > 0 && ask >= entry + stopStep)
				{
					ClosePosition();
					ResetTrailing();
					return;
				}

				if (TakeProfitPoints > 0 && ask <= entry - takeStep)
				{
					ClosePosition();
					ResetTrailing();
					return;
				}

				if (TrailingStopPoints > 0 && _lowestAsk is decimal low)
				{
					var stopLevel = low + trailStep;
					if (ask >= stopLevel)
					{
						ClosePosition();
						ResetTrailing();
					}
				}
			}
		}
		else if (Position == 0)
		{
			ResetTrailing();
		}
	}

	private void CancelOrderSafe(Order order)
	{
		if (order is null)
			return;

		var state = order.State;
		if (state == OrderStates.Done || state == OrderStates.Failed || state == OrderStates.Inactive)
			return;

		CancelOrder(order);
	}

	private void ResetTrailing()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_highestBid = null;
		_lowestAsk = null;
	}

	private void CancelEventOrders(NewsEventState evt)
	{
		CancelOrderSafe(evt.BuyOrder);
		CancelOrderSafe(evt.SellOrder);

		evt.BuyOrder = null;
		evt.SellOrder = null;
		evt.OrdersPlaced = false;
	}

	private void ParseCalendarDefinition()
	{
		_events.Clear();

		var raw = CalendarDefinition;
		if (raw.IsEmptyOrWhiteSpace())
		return;

		var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines)
		{
			var parts = line.Split(';');
			if (parts.Length < 4)
			{
				LogWarning($"Cannot parse calendar line '{line}'. Expected 'time;currency;importance;title'.");
				continue;
			}

			if (!TryParseTime(parts[0], out var time))
			{
				LogWarning($"Cannot parse event time '{parts[0]}' in line '{line}'.");
				continue;
			}

			if (!TryParseImportance(parts[2], out var importance))
			{
				LogWarning($"Unknown importance '{parts[2]}' in line '{line}'.");
				continue;
			}

			var evt = new NewsEventState
			{
				Time = time,
				Currency = parts[1].Trim(),
				Importance = importance,
				Title = parts[3].Trim()
			};

			_events.Add(evt);
		}
	}

	private static bool TryParseTime(string value, out DateTimeOffset time)
	{
		var formats = new[]
		{
			"yyyy-MM-dd HH:mm",
			"yyyy-MM-dd HH:mm:ss",
			"yyyy/MM/dd HH:mm",
			"yyyy/MM/dd HH:mm:ss",
			"dd.MM.yyyy HH:mm",
			"dd.MM.yyyy HH:mm:ss"
		};

		if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out time))
		return true;

		foreach (var format in formats)
		{
			if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var local))
			{
				time = new DateTimeOffset(local, TimeSpan.Zero);
				return true;
			}
		}

		time = default;
		return false;
	}

	private static bool TryParseImportance(string value, out NewsImportance importance)
	{
		if (value.EqualsIgnoreCase("high"))
		{
			importance = NewsImportance.High;
			return true;
		}

		if (value.EqualsIgnoreCase("medium"))
		{
			importance = NewsImportance.Medium;
			return true;
		}

		if (value.EqualsIgnoreCase("low"))
		{
			importance = NewsImportance.Low;
			return true;
		}

		if (value.EqualsIgnoreCase("nfp") || value.EqualsIgnoreCase("non-farm"))
		{
			importance = NewsImportance.Nfp;
			return true;
		}

		importance = NewsImportance.Low;
		return false;
	}

	private sealed class NewsEventState
	{
		public DateTimeOffset Time { get; init; }
		public string Currency { get; init; } = string.Empty;
		public NewsImportance Importance { get; init; }
		public string Title { get; init; } = string.Empty;
		public bool OrdersPlaced { get; set; }
		public bool Completed { get; set; }
		public DateTimeOffset? CancelAfter { get; set; }
		public Order BuyOrder { get; set; }
		public Order SellOrder { get; set; }
	}

	private enum NewsImportance
	{
		Low,
		Medium,
		High,
		Nfp
	}
}
