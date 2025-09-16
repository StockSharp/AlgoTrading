using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade EA Template for News strategy converted from MQL.
/// </summary>
public class TradeEaTemplateForNewsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useLowNews;
	private readonly StrategyParam<int> _lowMinutesBefore;
	private readonly StrategyParam<int> _lowMinutesAfter;
	private readonly StrategyParam<bool> _useMediumNews;
	private readonly StrategyParam<int> _mediumMinutesBefore;
	private readonly StrategyParam<int> _mediumMinutesAfter;
	private readonly StrategyParam<bool> _useHighNews;
	private readonly StrategyParam<int> _highMinutesBefore;
	private readonly StrategyParam<int> _highMinutesAfter;
	private readonly StrategyParam<bool> _useNfpNews;
	private readonly StrategyParam<int> _nfpMinutesBefore;
	private readonly StrategyParam<int> _nfpMinutesAfter;
	private readonly StrategyParam<bool> _onlySymbolNews;
	private readonly StrategyParam<string> _newsEventsDefinition;
	private readonly StrategyParam<int> _timeZoneOffsetHours;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;

	private readonly List<NewsEvent> _newsEvents = new();
	private readonly HashSet<string> _instrumentCurrencies = new(StringComparer.OrdinalIgnoreCase);

	private decimal? _previousOpenPrice;
	private bool _newsBlocking;
	private string _lastNewsMessage = string.Empty;

	public TradeEaTemplateForNewsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
		_useLowNews = Param(nameof(UseLowNews), true);
		_lowMinutesBefore = Param(nameof(LowMinutesBefore), 15);
		_lowMinutesAfter = Param(nameof(LowMinutesAfter), 15);
		_useMediumNews = Param(nameof(UseMediumNews), true);
		_mediumMinutesBefore = Param(nameof(MediumMinutesBefore), 30);
		_mediumMinutesAfter = Param(nameof(MediumMinutesAfter), 30);
		_useHighNews = Param(nameof(UseHighNews), true);
		_highMinutesBefore = Param(nameof(HighMinutesBefore), 60);
		_highMinutesAfter = Param(nameof(HighMinutesAfter), 60);
		_useNfpNews = Param(nameof(UseNfpNews), true);
		_nfpMinutesBefore = Param(nameof(NfpMinutesBefore), 180);
		_nfpMinutesAfter = Param(nameof(NfpMinutesAfter), 180);
		_onlySymbolNews = Param(nameof(OnlySymbolNews), true);
		_newsEventsDefinition = Param(nameof(NewsEventsDefinition), string.Empty);
		_timeZoneOffsetHours = Param(nameof(TimeZoneOffsetHours), 0);
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100);
		_stopLossPoints = Param(nameof(StopLossPoints), 100);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public bool UseLowNews
	{
		get => _useLowNews.Value;
		set => _useLowNews.Value = value;
	}

	public int LowMinutesBefore
	{
		get => _lowMinutesBefore.Value;
		set => _lowMinutesBefore.Value = value;
	}

	public int LowMinutesAfter
	{
		get => _lowMinutesAfter.Value;
		set => _lowMinutesAfter.Value = value;
	}

	public bool UseMediumNews
	{
		get => _useMediumNews.Value;
		set => _useMediumNews.Value = value;
	}

	public int MediumMinutesBefore
	{
		get => _mediumMinutesBefore.Value;
		set => _mediumMinutesBefore.Value = value;
	}

	public int MediumMinutesAfter
	{
		get => _mediumMinutesAfter.Value;
		set => _mediumMinutesAfter.Value = value;
	}

	public bool UseHighNews
	{
		get => _useHighNews.Value;
		set => _useHighNews.Value = value;
	}

	public int HighMinutesBefore
	{
		get => _highMinutesBefore.Value;
		set => _highMinutesBefore.Value = value;
	}

	public int HighMinutesAfter
	{
		get => _highMinutesAfter.Value;
		set => _highMinutesAfter.Value = value;
	}

	public bool UseNfpNews
	{
		get => _useNfpNews.Value;
		set => _useNfpNews.Value = value;
	}

	public int NfpMinutesBefore
	{
		get => _nfpMinutesBefore.Value;
		set => _nfpMinutesBefore.Value = value;
	}

	public int NfpMinutesAfter
	{
		get => _nfpMinutesAfter.Value;
		set => _nfpMinutesAfter.Value = value;
	}

	public bool OnlySymbolNews
	{
		get => _onlySymbolNews.Value;
		set => _onlySymbolNews.Value = value;
	}

	public string NewsEventsDefinition
	{
		get => _newsEventsDefinition.Value;
		set => _newsEventsDefinition.Value = value;
	}

	public int TimeZoneOffsetHours
	{
		get => _timeZoneOffsetHours.Value;
		set => _timeZoneOffsetHours.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	private bool HasNewsFilter => UseLowNews || UseMediumNews || UseHighNews || UseNfpNews;

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_previousOpenPrice = null;
		_newsEvents.Clear();
		_newsBlocking = false;
		_lastNewsMessage = string.Empty;
		_instrumentCurrencies.Clear();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		BuildInstrumentCurrencies();
		ParseNewsEvents();
		ConfigureProtection();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ConfigureProtection()
	{
		// Configure stop-loss and take-profit to mirror the 100 point brackets from the template EA.
		var step = Security?.Step ?? 0m;

		if (step <= 0m)
		{
			LogWarn("Security step is zero. Protective orders cannot be configured.");
			return;
		}

		var takeUnit = TakeProfitPoints > 0 ? new Unit(step * TakeProfitPoints, UnitTypes.Absolute) : new Unit();
		var stopUnit = StopLossPoints > 0 ? new Unit(step * StopLossPoints, UnitTypes.Absolute) : new Unit();

		if (TakeProfitPoints > 0 || StopLossPoints > 0)
			StartProtection(takeUnit, stopUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateNewsState(candle.CloseTime);

		// Skip trading while the infrastructure is not ready to process orders.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Abort any signals while a news blackout is active.
		if (_newsBlocking)
		{
			CancelActiveOrders();
			return;
		}

		if (_previousOpenPrice == null)
		{
			// Store the first open price so the next candle can compare against it.
			_previousOpenPrice = candle.OpenPrice;
			return;
		}

		var previousOpen = _previousOpenPrice.Value;
		_previousOpenPrice = candle.OpenPrice;

		if (Volume <= 0)
			return;

		if (Position != 0)
		{
			// The original template trades only when there are no existing positions.
			return;
		}

		if (candle.ClosePrice > previousOpen)
		{
			BuyMarket(Volume);
			LogInfo($"Long entry after bullish close at {candle.ClosePrice} compared to prior open {previousOpen}.");
		}
		else if (candle.ClosePrice < previousOpen)
		{
			SellMarket(Volume);
			LogInfo($"Short entry after bearish close at {candle.ClosePrice} compared to prior open {previousOpen}.");
		}
	}

	private void UpdateNewsState(DateTimeOffset currentTime)
	{
		// Without configured events the strategy should allow trading freely.
		if (!HasNewsFilter || _newsEvents.Count == 0)
		{
			if (_newsBlocking)
			{
				_newsBlocking = false;
				NotifyNewsMessage("No upcoming news events.");
			}
			return;
		}

		NewsEvent? blockingEvent = null;
		NewsEvent? upcomingEvent = null;

		for (var i = 0; i < _newsEvents.Count; i++)
		{
			var evt = _newsEvents[i];

			if (!IsImportanceEnabled(evt.Importance))
				continue;

			if (!MatchesSecurity(evt))
				continue;

			if (IsInsideWindow(evt, currentTime))
			{
				if (blockingEvent == null || evt.Importance > blockingEvent.Importance)
					blockingEvent = evt;
			}
			else if (evt.Time > currentTime)
			{
				if (upcomingEvent == null || evt.Time < upcomingEvent.Time)
					upcomingEvent = evt;
			}
		}

		var wasBlocking = _newsBlocking;
		_newsBlocking = blockingEvent != null;

		NotifyNewsMessage(BuildNewsMessage(blockingEvent, upcomingEvent));

		if (_newsBlocking && !wasBlocking)
			CancelActiveOrders();
	}

	private void NotifyNewsMessage(string message)
	{
		if (string.Equals(_lastNewsMessage, message, StringComparison.Ordinal))
			return;

		_lastNewsMessage = message;
		LogInfo(message);
	}

	private bool IsImportanceEnabled(NewsImportance importance)
	=> importance switch
	{
		NewsImportance.Low => UseLowNews,
		NewsImportance.Medium => UseMediumNews,
		NewsImportance.High => UseHighNews,
		NewsImportance.Nfp => UseNfpNews,
		_ => false
	};

	private bool IsInsideWindow(NewsEvent evt, DateTimeOffset currentTime)
	{
		var before = TimeSpan.FromMinutes(GetMinutesBefore(evt.Importance));
		var after = TimeSpan.FromMinutes(GetMinutesAfter(evt.Importance));
		var start = evt.Time - before;
		var end = evt.Time + after;
		return currentTime >= start && currentTime <= end;
	}

	private int GetMinutesBefore(NewsImportance importance)
	=> importance switch
	{
		NewsImportance.Low => Math.Max(0, LowMinutesBefore),
		NewsImportance.Medium => Math.Max(0, MediumMinutesBefore),
		NewsImportance.High => Math.Max(0, HighMinutesBefore),
		NewsImportance.Nfp => Math.Max(0, NfpMinutesBefore),
		_ => 0
	};

	private int GetMinutesAfter(NewsImportance importance)
	=> importance switch
	{
		NewsImportance.Low => Math.Max(0, LowMinutesAfter),
		NewsImportance.Medium => Math.Max(0, MediumMinutesAfter),
		NewsImportance.High => Math.Max(0, HighMinutesAfter),
		NewsImportance.Nfp => Math.Max(0, NfpMinutesAfter),
		_ => 0
	};

	private string BuildNewsMessage(NewsEvent? activeEvent, NewsEvent? upcomingEvent)
	{
		if (activeEvent != null)
		{
			var label = GetImportanceLabel(activeEvent.Importance);
			var timeText = activeEvent.Time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
			var currencyPart = string.IsNullOrWhiteSpace(activeEvent.Currency) ? string.Empty : $" [{activeEvent.Currency}]";
			var titlePart = string.IsNullOrWhiteSpace(activeEvent.Title) ? string.Empty : $" - {activeEvent.Title}";
			return $"Trading paused due to {label} news{currencyPart} at {timeText}{titlePart}.";
		}

		if (upcomingEvent != null)
		{
			var label = GetImportanceLabel(upcomingEvent.Importance);
			var timeText = upcomingEvent.Time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
			var currencyPart = string.IsNullOrWhiteSpace(upcomingEvent.Currency) ? string.Empty : $" [{upcomingEvent.Currency}]";
			var titlePart = string.IsNullOrWhiteSpace(upcomingEvent.Title) ? string.Empty : $" - {upcomingEvent.Title}";
			return $"Next scheduled news: {label}{currencyPart} at {timeText}{titlePart}.";
		}

		return "No upcoming news events.";
	}

	private static string GetImportanceLabel(NewsImportance importance)
	=> importance switch
	{
		NewsImportance.Low => "low",
		NewsImportance.Medium => "medium",
		NewsImportance.High => "high",
		NewsImportance.Nfp => "non-farm payroll",
		_ => "unknown"
	};

	private void ParseNewsEvents()
	{
		// Parse the manual economic calendar description provided in the parameters.
		_newsEvents.Clear();

		var raw = NewsEventsDefinition;

		if (string.IsNullOrWhiteSpace(raw))
		{
			LogInfo("News events list is empty. The filter will allow trading at all times.");
			return;
		}

		var separators = new[] { ';', '\n', '\r' };
		var entries = raw.Split(separators, StringSplitOptions.RemoveEmptyEntries);

		for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
		{
			var entry = entries[entryIndex].Trim();

			if (entry.Length == 0)
				continue;

			var rawParts = entry.Split(',');

			if (rawParts.Length < 3)
			{
				LogWarn($"Unable to parse news entry '{entry}'. Expected at least time, currency and importance.");
				continue;
			}

			var parts = new string[rawParts.Length];
			for (var i = 0; i < rawParts.Length; i++)
				parts[i] = rawParts[i].Trim();

			if (!DateTimeOffset.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var time))
			{
				LogWarn($"Unable to parse time '{parts[0]}' in news entry '{entry}'.");
				continue;
			}

			var currencies = parts[1].ToUpperInvariant();

			if (!TryParseImportance(parts[2], out var importance))
			{
				LogWarn($"Unable to parse importance '{parts[2]}' in news entry '{entry}'.");
				continue;
			}

			var title = string.Empty;
			if (parts.Length > 3)
			{
				var count = parts.Length - 3;
				var combined = string.Join(",", parts, 3, count);
				title = combined.Trim();
			}

			time = time.ToOffset(TimeSpan.FromHours(TimeZoneOffsetHours));

			_newsEvents.Add(new NewsEvent(time, currencies, importance, title));
		}

		_newsEvents.Sort((left, right) => left.Time.CompareTo(right.Time));

		if (_newsEvents.Count > 0)
			LogInfo($"Loaded {_newsEvents.Count} manual news event(s).");
		else
			LogInfo("No valid news events parsed. The filter will remain inactive.");
	}

	private static bool TryParseImportance(string value, out NewsImportance importance)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			importance = default;
			return false;
		}

		var normalized = value.Trim();

		if (normalized.Equals("LOW", StringComparison.OrdinalIgnoreCase))
		{
			importance = NewsImportance.Low;
			return true;
		}

		if (normalized.Equals("MEDIUM", StringComparison.OrdinalIgnoreCase) ||
				normalized.Equals("MID", StringComparison.OrdinalIgnoreCase) ||
				normalized.Equals("MIDLE", StringComparison.OrdinalIgnoreCase) ||
				normalized.Equals("MODERATE", StringComparison.OrdinalIgnoreCase))
		{
			importance = NewsImportance.Medium;
			return true;
		}

		if (normalized.Equals("HIGH", StringComparison.OrdinalIgnoreCase))
		{
			importance = NewsImportance.High;
			return true;
		}

		if (normalized.Equals("NFP", StringComparison.OrdinalIgnoreCase) ||
				normalized.Contains("NONFARM", StringComparison.OrdinalIgnoreCase) ||
				normalized.Contains("NON-FARM", StringComparison.OrdinalIgnoreCase))
		{
			importance = NewsImportance.Nfp;
			return true;
		}

		importance = default;
		return false;
	}

	private bool MatchesSecurity(NewsEvent evt)
	{
		if (!OnlySymbolNews)
			return true;

		// Match the configured currencies against the current instrument if required.

		if (_instrumentCurrencies.Count == 0)
			return true;

		if (string.IsNullOrWhiteSpace(evt.Currency))
			return true;

		var separators = new[] { '/', ',', '|', ';', ' ' };
		var tokens = evt.Currency.Split(separators, StringSplitOptions.RemoveEmptyEntries);

		for (var i = 0; i < tokens.Length; i++)
		{
			var token = tokens[i].Trim();
			if (token.Length == 0)
				continue;

			if (_instrumentCurrencies.Contains(token))
				return true;
		}

		return false;
	}

	private void BuildInstrumentCurrencies()
	{
		// Extract major currency codes from the security symbol (e.g., EURUSD -> EUR, USD).
		_instrumentCurrencies.Clear();

		var code = Security?.Code;

		if (string.IsNullOrWhiteSpace(code))
			return;

		var trimmed = code.Trim().ToUpperInvariant();

		if (trimmed.Length >= 6)
		{
			_instrumentCurrencies.Add(trimmed.Substring(0, 3));
			_instrumentCurrencies.Add(trimmed.Substring(trimmed.Length - 3, 3));
		}
		else
		{
			_instrumentCurrencies.Add(trimmed);
		}
	}

	private sealed record class NewsEvent(DateTimeOffset Time, string Currency, NewsImportance Importance, string Title);

	private enum NewsImportance
	{
		Low = 1,
		Medium = 2,
		High = 3,
		Nfp = 4
	}
}
