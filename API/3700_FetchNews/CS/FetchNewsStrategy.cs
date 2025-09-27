using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors macroeconomic calendar events and reacts according to the selected mode.
/// In alerting mode it logs upcoming medium or high importance events for the current instrument.
/// In trading mode it places a straddle using buy/sell stop orders around the current price for selected keywords.
/// </summary>
public class FetchNewsStrategy : Strategy
{
	private readonly StrategyParam<FetchNewsOperationMode> _mode;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _orderLifetimeSeconds;
	private readonly StrategyParam<int> _lookBackSeconds;
	private readonly StrategyParam<int> _lookAheadSeconds;
	private readonly StrategyParam<string> _tradingKeywords;
	private readonly StrategyParam<string> _calendarEventsDefinition;
	private readonly StrategyParam<int> _timeZoneOffsetHours;
	private readonly StrategyParam<NewsImportanceLevel> _alertImportance;
	private readonly StrategyParam<bool> _onlySymbolCurrencies;

	private readonly List<CalendarEvent> _calendarEvents = new();
	private readonly HashSet<string> _processedEvents = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _instrumentCurrencies = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<string> _keywordList = new();

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private DateTimeOffset? _pendingExpiration;
	private decimal? _lastBid;
	private decimal? _lastAsk;

	/// <summary>
	/// Initializes a new instance of the <see cref="FetchNewsStrategy"/> class.
	/// </summary>
	public FetchNewsStrategy()
	{
		_mode = Param(nameof(Mode), FetchNewsOperationMode.Alerting)
		.SetDisplay("Mode", "Select alerting or trading behaviour.", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume in lots for pending orders.", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
		.SetDisplay("Take Profit (points)", "Distance in points between entry and profit target.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
		.SetDisplay("Stop Loss (points)", "Distance in points between entry and protective stop.", "Trading");

		_orderLifetimeSeconds = Param(nameof(OrderLifetimeSeconds), 500)
		.SetDisplay("Order Lifetime (s)", "Time in seconds before pending orders expire.", "Trading");

		_lookBackSeconds = Param(nameof(LookBackSeconds), 50)
		.SetDisplay("Look Back (s)", "Seconds before event time when it becomes active.", "Calendar");

		_lookAheadSeconds = Param(nameof(LookAheadSeconds), 50)
		.SetDisplay("Look Ahead (s)", "Seconds after event time while it remains active.", "Calendar");

		_tradingKeywords = Param(nameof(TradingKeywords), "cpi;ppi;interest rate decision")
		.SetDisplay("Trading Keywords", "Semicolon separated keywords that trigger trading mode.", "Calendar");

		_calendarEventsDefinition = Param(nameof(CalendarEventsDefinition), string.Empty)
		.SetDisplay("Calendar Events", "List of scheduled events (time,currency,importance,name).", "Calendar");

		_timeZoneOffsetHours = Param(nameof(TimeZoneOffsetHours), 0)
		.SetDisplay("Calendar TZ Offset (h)", "Offset in hours applied to event timestamps.", "Calendar");

		_alertImportance = Param(nameof(AlertImportance), NewsImportanceLevel.Moderate)
		.SetDisplay("Alert Importance", "Minimum importance for alerting mode.", "Alerting");

		_onlySymbolCurrencies = Param(nameof(OnlySymbolCurrencies), true)
		.SetDisplay("Only Symbol Currencies", "Filter events to currencies found in the instrument symbol.", "Calendar");
	}

	/// <summary>
	/// Mode of operation.
	/// </summary>
	public FetchNewsOperationMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Pending order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Distance in points between entry price and take-profit level.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Distance in points between entry price and stop-loss level.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Lifetime of pending orders in seconds.
	/// </summary>
	public int OrderLifetimeSeconds
	{
		get => _orderLifetimeSeconds.Value;
		set => _orderLifetimeSeconds.Value = value;
	}

	/// <summary>
	/// Seconds before scheduled time when the event becomes eligible.
	/// </summary>
	public int LookBackSeconds
	{
		get => _lookBackSeconds.Value;
		set => _lookBackSeconds.Value = value;
	}

	/// <summary>
	/// Seconds after scheduled time when the event remains eligible.
	/// </summary>
	public int LookAheadSeconds
	{
		get => _lookAheadSeconds.Value;
		set => _lookAheadSeconds.Value = value;
	}

	/// <summary>
	/// Keyword filter applied in trading mode.
	/// </summary>
	public string TradingKeywords
	{
		get => _tradingKeywords.Value;
		set => _tradingKeywords.Value = value;
	}

	/// <summary>
	/// Raw calendar definition provided by the user.
	/// </summary>
	public string CalendarEventsDefinition
	{
		get => _calendarEventsDefinition.Value;
		set => _calendarEventsDefinition.Value = value;
	}

	/// <summary>
	/// Hours offset applied to calendar timestamps.
	/// </summary>
	public int TimeZoneOffsetHours
	{
		get => _timeZoneOffsetHours.Value;
		set => _timeZoneOffsetHours.Value = value;
	}

	/// <summary>
	/// Minimum importance processed in alerting mode.
	/// </summary>
	public NewsImportanceLevel AlertImportance
	{
		get => _alertImportance.Value;
		set => _alertImportance.Value = value;
	}

	/// <summary>
	/// Restrict processing to currencies derived from the instrument symbol.
	/// </summary>
	public bool OnlySymbolCurrencies
	{
		get => _onlySymbolCurrencies.Value;
		set => _onlySymbolCurrencies.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		LoadInstrumentCurrencies();
		LoadKeywords();
		LoadCalendarEvents();

		// Enable protective stop/take management helpers once.
		StartProtection();

		// Subscribe to Level1 data for bid/ask updates and event processing.
		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order == null || trade.Trade?.Price is not decimal price)
		return;

		if (trade.Order == _buyStopOrder)
		{
			// Cancel opposite pending order after the long entry is triggered.
			if (_sellStopOrder != null)
			{
				CancelOrder(_sellStopOrder);
				_sellStopOrder = null;
			}

			ApplyProtection(Sides.Buy, price);
			_buyStopOrder = null;
			_pendingExpiration = null;
		}
	else if (trade.Order == _sellStopOrder)
		{
			// Cancel opposite pending order after the short entry is triggered.
			if (_buyStopOrder != null)
			{
				CancelOrder(_buyStopOrder);
				_buyStopOrder = null;
			}

			ApplyProtection(Sides.Sell, price);
			_sellStopOrder = null;
			_pendingExpiration = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			// Clear pending expiration when flat to allow new events.
			_pendingExpiration = null;
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1 == null)
		return;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		_lastBid = Convert.ToDecimal(bidValue, CultureInfo.InvariantCulture);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		_lastAsk = Convert.ToDecimal(askValue, CultureInfo.InvariantCulture);

		var currentTime = level1.ServerTime != default ? level1.ServerTime : CurrentTime;

		CancelExpiredPending(currentTime);
		ProcessCalendar(currentTime);
	}

	private void ProcessCalendar(DateTimeOffset now)
	{
		if (_calendarEvents.Count == 0)
		return;

		var lookBack = TimeSpan.FromSeconds(Math.Max(0, LookBackSeconds));
		var lookAhead = TimeSpan.FromSeconds(Math.Max(0, LookAheadSeconds));

		foreach (var calendarEvent in _calendarEvents)
		{
			if (_processedEvents.Contains(calendarEvent.Id))
			continue;

			var delta = calendarEvent.Time - now;
			if (delta < -lookBack || delta > lookAhead)
			continue;

			if (OnlySymbolCurrencies && _instrumentCurrencies.Count > 0 && !_instrumentCurrencies.Contains(calendarEvent.Currency))
			{
				_processedEvents.Add(calendarEvent.Id);
				continue;
			}

			switch (Mode)
			{
			case FetchNewsOperationMode.Alerting:
				ProcessAlert(calendarEvent);
				_processedEvents.Add(calendarEvent.Id);
				break;

			case FetchNewsOperationMode.Trading:
				if (ProcessTrading(calendarEvent, now))
				_processedEvents.Add(calendarEvent.Id);
				break;
			}
		}
	}

	private void ProcessAlert(CalendarEvent calendarEvent)
	{
		if (calendarEvent.Importance < AlertImportance)
		return;

		// Log information about the upcoming event for the operator.
		LogInfo($"Upcoming important news event: {calendarEvent.Name} at {calendarEvent.Time:O} ({calendarEvent.Currency}).");
	}

	private bool ProcessTrading(CalendarEvent calendarEvent, DateTimeOffset now)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return false;

		if (_buyStopOrder != null || _sellStopOrder != null)
		return false;

		if (Position != 0m)
		return false;

		if (!MatchesKeywords(calendarEvent.Name))
		return false;

		if (_lastAsk is not decimal ask || _lastBid is not decimal bid)
		return false;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			LogWarning("Security price step is zero. Unable to place pending orders.");
			return false;
		}

		var volume = OrderVolume;
		if (volume <= 0m)
		return false;

		var tpDistance = TakeProfitPoints * step;

		var buyPrice = ask + tpDistance;
		var sellPrice = bid - tpDistance;

		if (buyPrice <= 0m || sellPrice <= 0m)
		return false;

		_buyStopOrder = BuyStop(volume, buyPrice);
		_sellStopOrder = SellStop(volume, sellPrice);

		if (_buyStopOrder == null && _sellStopOrder == null)
		{
			LogWarning("Failed to register pending orders for the news straddle.");
			return false;
		}

		_pendingExpiration = OrderLifetimeSeconds > 0
		? now + TimeSpan.FromSeconds(OrderLifetimeSeconds)
		: null;

		// Provide feedback about the scheduled trade in the journal.
		LogInfo($"Placed news straddle around {calendarEvent.Name} at {calendarEvent.Time:O}.");
		return true;
	}

	private void ApplyProtection(Sides side, decimal entryPrice)
	{
		var resultingPosition = Position;
		if (side == Sides.Buy && resultingPosition <= 0m)
		return;

		if (side == Sides.Sell && resultingPosition >= 0m)
		return;

		if (TakeProfitPoints > 0)
		SetTakeProfit(TakeProfitPoints, entryPrice, resultingPosition);

		if (StopLossPoints > 0)
		SetStopLoss(StopLossPoints, entryPrice, resultingPosition);
	}

	private void CancelExpiredPending(DateTimeOffset now)
	{
		if (_pendingExpiration is not DateTimeOffset expiration)
		return;

		if (now < expiration)
		return;

		CancelPendingOrders();
		_pendingExpiration = null;
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
	}

	private void ResetState()
	{
		CancelPendingOrders();
		_calendarEvents.Clear();
		_processedEvents.Clear();
		_keywordList.Clear();
		_instrumentCurrencies.Clear();
		_pendingExpiration = null;
		_lastBid = null;
		_lastAsk = null;
	}

	private void LoadInstrumentCurrencies()
	{
		var symbol = Security?.Code;
		if (symbol.IsEmptyOrWhiteSpace())
		return;

		var upper = symbol.ToUpperInvariant();

		foreach (var segment in upper.Split(new[] { '/', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (segment.Length == 3)
			_instrumentCurrencies.Add(segment);
		}

		if (upper.Length == 6)
		{
			_instrumentCurrencies.Add(upper[..3]);
			_instrumentCurrencies.Add(upper[^3..]);
		}

		var currency = Security?.Currency;
		if (!currency.IsEmptyOrWhiteSpace())
		_instrumentCurrencies.Add(currency!.ToUpperInvariant());
	}

	private void LoadKeywords()
	{
		_keywordList.Clear();

		var source = TradingKeywords;
		if (source.IsEmptyOrWhiteSpace())
		return;

		var parts = source.Split(new[] { ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var part in parts)
		{
			var keyword = part.Trim();
			if (keyword.Length > 0)
			_keywordList.Add(keyword);
		}
	}

	private void LoadCalendarEvents()
	{
		_calendarEvents.Clear();

		var raw = CalendarEventsDefinition;
		if (raw.IsEmptyOrWhiteSpace())
		{
			LogWarning("Calendar events definition is empty. No alerts or trades will be generated.");
			return;
		}

		var records = raw.Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);
		var offset = TimeSpan.FromHours(TimeZoneOffsetHours);

		foreach (var record in records)
		{
			var trimmed = record.Trim();
			if (trimmed.Length == 0)
			continue;

			var cells = trimmed.Split(',');
			if (cells.Length < 4)
			{
				LogWarning($"Invalid calendar record '{trimmed}'. Expected at least four comma separated fields.");
				continue;
			}

			var timeText = cells[0].Trim();
			var currency = cells[1].Trim().ToUpperInvariant();
			var importanceText = cells[2].Trim();
			var name = string.Join(",", cells.Skip(3)).Trim();

			if (!DateTime.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedTime))
			{
				LogWarning($"Failed to parse event time '{timeText}'.");
				continue;
			}

			if (!TryParseImportance(importanceText, out var importance))
			{
				LogWarning($"Unknown importance label '{importanceText}'.");
				continue;
			}

			var eventTime = new DateTimeOffset(parsedTime, offset).ToUniversalTime();
			var id = $"{eventTime:O}|{currency}|{name}";

			_calendarEvents.Add(new CalendarEvent(id, eventTime, currency, importance, name));
		}

		_calendarEvents.Sort((left, right) => left.Time.CompareTo(right.Time));
	}

	private bool TryParseImportance(string text, out NewsImportanceLevel importance)
	{
		if (Enum.TryParse(text, true, out importance))
		return true;

		switch (text.Trim().ToLowerInvariant())
		{
		case "medium":
		case "moderate":
			importance = NewsImportanceLevel.Moderate;
			return true;

		case "high":
		case "important":
			importance = NewsImportanceLevel.High;
			return true;

		case "low":
			importance = NewsImportanceLevel.Low;
			return true;
		}

		importance = default;
		return false;
	}

	private bool MatchesKeywords(string text)
	{
		if (_keywordList.Count == 0)
		return false;

		foreach (var keyword in _keywordList)
		{
			if (text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
			return true;
		}

		return false;
	}

	private sealed record CalendarEvent(string Id, DateTimeOffset Time, string Currency, NewsImportanceLevel Importance, string Name);
}

/// <summary>
/// Available modes for <see cref="FetchNewsStrategy"/>.
/// </summary>
public enum FetchNewsOperationMode
{
	/// <summary>
	/// Only log matching events.
	/// </summary>
	Alerting,

	/// <summary>
	/// Place pending orders around the price near selected events.
	/// </summary>
	Trading,
}

/// <summary>
/// Importance level used by the macroeconomic calendar.
/// </summary>
public enum NewsImportanceLevel
{
	/// <summary>
	/// Low importance release.
	/// </summary>
	Low,

	/// <summary>
	/// Moderate importance release.
	/// </summary>
	Moderate,

	/// <summary>
	/// High importance release.
	/// </summary>
	High,
}
