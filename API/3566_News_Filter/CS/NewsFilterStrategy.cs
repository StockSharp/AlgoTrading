using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// News filter strategy converted from MQL4 template.
/// Downloads upcoming economic events from the FXStreet calendar,
/// filters them by currency and importance, and exposes a flag indicating
/// whether trading should be paused during the pre/post news window.
/// </summary>
public class NewsFilterStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableNewsFilter;
	private readonly StrategyParam<bool> _useLowImportance;
	private readonly StrategyParam<bool> _useMediumImportance;
	private readonly StrategyParam<bool> _useHighImportance;
	private readonly StrategyParam<int> _stopBeforeNewsMinutes;
	private readonly StrategyParam<int> _startAfterNewsMinutes;
	private readonly StrategyParam<string> _currenciesFilter;
	private readonly StrategyParam<bool> _filterByKeyword;
	private readonly StrategyParam<string> _keyword;
	private readonly StrategyParam<int> _refreshIntervalMinutes;

	private readonly List<EconomicEvent> _events = new();
	private readonly object _sync = new();
	private Timer? _timer;
	private DateTime _lastUpdateUtc;
	private bool _isNewsActive;
	private string _lastStatus = string.Empty;

	/// <summary>
	/// Enables or disables the news filter.
	/// </summary>
	public bool EnableNewsFilter
	{
		get => _enableNewsFilter.Value;
		set => _enableNewsFilter.Value = value;
	}

	/// <summary>
	/// Allows events marked as low importance.
	/// </summary>
	public bool UseLowImportance
	{
		get => _useLowImportance.Value;
		set => _useLowImportance.Value = value;
	}

	/// <summary>
	/// Allows events marked as medium importance.
	/// </summary>
	public bool UseMediumImportance
	{
		get => _useMediumImportance.Value;
		set => _useMediumImportance.Value = value;
	}

	/// <summary>
	/// Allows events marked as high importance.
	/// </summary>
	public bool UseHighImportance
	{
		get => _useHighImportance.Value;
		set => _useHighImportance.Value = value;
	}

	/// <summary>
	/// Minutes before the news event when trading should be paused.
	/// </summary>
	public int StopBeforeNewsMinutes
	{
		get => _stopBeforeNewsMinutes.Value;
		set => _stopBeforeNewsMinutes.Value = value;
	}

	/// <summary>
	/// Minutes after the news event when trading remains paused.
	/// </summary>
	public int StartAfterNewsMinutes
	{
		get => _startAfterNewsMinutes.Value;
		set => _startAfterNewsMinutes.Value = value;
	}

	/// <summary>
	/// Comma separated list of currency codes to monitor.
	/// </summary>
	public string CurrenciesFilter
	{
		get => _currenciesFilter.Value;
		set => _currenciesFilter.Value = value;
	}

	/// <summary>
	/// Enables keyword filtering for event titles.
	/// </summary>
	public bool FilterByKeyword
	{
		get => _filterByKeyword.Value;
		set => _filterByKeyword.Value = value;
	}

	/// <summary>
	/// Keyword that must be present in the event title.
	/// </summary>
	public string Keyword
	{
		get => _keyword.Value;
		set => _keyword.Value = value;
	}

	/// <summary>
	/// Minutes between calendar refreshes.
	/// </summary>
	public int RefreshIntervalMinutes
	{
		get => _refreshIntervalMinutes.Value;
		set => _refreshIntervalMinutes.Value = value;
	}

	/// <summary>
	/// Indicates whether the strategy is currently inside the news window.
	/// </summary>
	public bool IsNewsActive => _isNewsActive;

	/// <summary>
	/// Initializes <see cref="NewsFilterStrategy"/> parameters.
	/// </summary>
	public NewsFilterStrategy()
	{
		_enableNewsFilter = Param(nameof(EnableNewsFilter), true)
		.SetDisplay("News Filter", "Enable downloading and filtering of economic news", "General");

		_useLowImportance = Param(nameof(UseLowImportance), false)
		.SetDisplay("Low Importance", "Include low importance events", "Filters");

		_useMediumImportance = Param(nameof(UseMediumImportance), true)
		.SetDisplay("Medium Importance", "Include medium importance events", "Filters");

		_useHighImportance = Param(nameof(UseHighImportance), true)
		.SetDisplay("High Importance", "Include high importance events", "Filters");

		_stopBeforeNewsMinutes = Param(nameof(StopBeforeNewsMinutes), 30)
		.SetDisplay("Stop Before (min)", "Minutes before news to pause trading", "Timing")
		.SetCanOptimize(true);

		_startAfterNewsMinutes = Param(nameof(StartAfterNewsMinutes), 30)
		.SetDisplay("Start After (min)", "Minutes after news to resume trading", "Timing")
		.SetCanOptimize(true);

		_currenciesFilter = Param(nameof(CurrenciesFilter), "USD,EUR,CAD,AUD,NZD,GBP")
		.SetDisplay("Currencies", "Comma separated list of currencies to monitor", "Filters");

		_filterByKeyword = Param(nameof(FilterByKeyword), false)
		.SetDisplay("Use Keyword", "Only allow events that contain the specified keyword", "Filters");

		_keyword = Param(nameof(Keyword), "employment")
		.SetDisplay("Keyword", "Keyword that must be present in the event title", "Filters");

		_refreshIntervalMinutes = Param(nameof(RefreshIntervalMinutes), 10)
		.SetDisplay("Refresh Interval", "Minutes between calendar downloads", "General")
		.SetGreaterThanZero();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initial download immediately after start.
		_ = LoadNewsAsync();

		// Reuse single timer for periodic refresh and status updates.
		_timer = new Timer(_ => OnTimer(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		_timer?.Dispose();
		_timer = null;
	}

	private async void OnTimer()
	{
		try
		{
			if (!EnableNewsFilter)
			{
				UpdateStatus(false, null);
				return;
			}

			// Refresh the calendar when the cached data becomes stale.
			if ((DateTime.UtcNow - _lastUpdateUtc).TotalMinutes >= Math.Max(1, RefreshIntervalMinutes))
			{
				await LoadNewsAsync();
			}

			CheckNewsWindow(DateTimeOffset.UtcNow);
		}
		catch (Exception ex)
		{
			LogError($"Timer failure: {ex.Message}");
		}
	}

	private async Task LoadNewsAsync()
	{
		try
		{
			using var client = new HttpClient();

			var now = DateTime.UtcNow;
			var start = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
			var end = now.AddDays(7).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
			var url = $"http://calendar.fxstreet.com/EventDateWidget/GetMini?culture=en-US&view=range&start={start}&end={end}&timezone=UTC&columns=date%2Ctime%2Ccountry%2Ccountrycurrency%2Cevent%2Cconsensus%2Cprevious%2Cvolatility%2Cactual&showcountryname=false&showcurrencyname=true&isfree=true";

			var response = await client.GetAsync(url).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();

			var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if (!TryParseJson(payload))
			{
				ParseHtml(payload);
			}

			_lastUpdateUtc = DateTime.UtcNow;
			LogInfo($"Loaded {_events.Count} events from FXStreet calendar.");
		}
		catch (Exception ex)
		{
			LogError($"Failed to load news: {ex.Message}");
		}
	}

	private bool TryParseJson(string payload)
	{
		try
		{
			using var document = JsonDocument.Parse(payload);
			var eventsElement = document.RootElement;

			if (eventsElement.ValueKind == JsonValueKind.Object && eventsElement.TryGetProperty("events", out var eventsProperty))
			{
				eventsElement = eventsProperty;
			}

			if (eventsElement.ValueKind != JsonValueKind.Array)
			{
				return false;
			}

			var parsedEvents = new List<EconomicEvent>();

			foreach (var item in eventsElement.EnumerateArray())
			{
				var dateText = item.TryGetProperty("date", out var dateProp) ? dateProp.GetString() : null;
				var timeText = item.TryGetProperty("time", out var timeProp) ? timeProp.GetString() : null;
				var currency = item.TryGetProperty("countrycurrency", out var currencyProp) ? currencyProp.GetString() : null;
				var title = item.TryGetProperty("event", out var eventProp) ? eventProp.GetString() : null;
				var volatility = item.TryGetProperty("volatility", out var volProp) ? volProp.GetString() : null;
				var actual = item.TryGetProperty("actual", out var actualProp) ? actualProp.GetString() : null;
				var forecast = item.TryGetProperty("consensus", out var forecastProp) ? forecastProp.GetString() : null;
				var previous = item.TryGetProperty("previous", out var previousProp) ? previousProp.GetString() : null;

				if (!TryBuildTime(dateText, timeText, out var time))
				{
					continue;
				}

				parsedEvents.Add(new EconomicEvent
				{
					Time = time,
					Currency = currency ?? string.Empty,
					Title = title ?? string.Empty,
					Importance = NormalizeImportance(volatility),
					Actual = actual ?? string.Empty,
					Forecast = forecast ?? string.Empty,
					Previous = previous ?? string.Empty
				});
			}

			CommitEvents(parsedEvents);
			return parsedEvents.Count > 0;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	private void ParseHtml(string payload)
	{
		var parsedEvents = new List<EconomicEvent>();

		var bodyStart = payload.IndexOf("<tbody", StringComparison.OrdinalIgnoreCase);
		if (bodyStart < 0)
		{
			CommitEvents(parsedEvents);
			return;
		}

		bodyStart = payload.IndexOf('>', bodyStart);
		if (bodyStart < 0)
		{
			CommitEvents(parsedEvents);
			return;
		}

		var bodyEnd = payload.IndexOf("</tbody>", bodyStart, StringComparison.OrdinalIgnoreCase);
		if (bodyEnd < 0)
		{
			bodyEnd = payload.Length;
		}

		var body = payload.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);
		var rowRegex = new Regex("<tr[\\s\\S]*?</tr>", RegexOptions.IgnoreCase);
		var currentDate = DateTime.UtcNow.Date;

		foreach (Match match in rowRegex.Matches(body))
		{
			var row = match.Value;

			var dateText = ExtractCell(row, "fxst-td-date");
			if (!string.IsNullOrEmpty(dateText))
			{
				if (DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate))
				{
					currentDate = parsedDate.Date;
				}

				continue;
			}

			if (!row.Contains("fxst-evenRow", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var timeText = ExtractCell(row, "fxst-td-time");
			var currency = ExtractCell(row, "fxst-td-currency");
			var importanceRaw = ExtractCell(row, "fxst-i-vol");
			var title = ExtractCell(row, "fxst-td-event");
			var actual = ExtractCell(row, "fxst-td-act");
			var forecast = ExtractCell(row, "fxst-td-cons");
			var previous = ExtractCell(row, "fxst-td-prev");

			if (!TryBuildTime(currentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), timeText, out var time))
			{
				continue;
			}

			parsedEvents.Add(new EconomicEvent
			{
				Time = time,
				Currency = currency,
				Title = title,
				Importance = NormalizeImportance(importanceRaw),
				Actual = actual,
				Forecast = forecast,
				Previous = previous
			});
		}

		CommitEvents(parsedEvents);
	}

	private static string ExtractCell(string row, string className)
	{
		var regex = new Regex($"<td[^>]*{className}[^>]*>([\\s\\S]*?)</td>", RegexOptions.IgnoreCase);
		var match = regex.Match(row);
		if (!match.Success)
		{
			// Importance uses span element instead of td.
			if (className == "fxst-i-vol")
			{
				var spanRegex = new Regex($"<span[^>]*{className}[^>]*>([\\s\\S]*?)</span>", RegexOptions.IgnoreCase);
				match = spanRegex.Match(row);
			}
		}

		if (!match.Success)
		{
			return string.Empty;
		}

		var text = match.Groups[1].Value;
		text = Regex.Replace(text, "<.*?>", string.Empty);
		return System.Net.WebUtility.HtmlDecode(text).Trim();
	}

	private static bool TryBuildTime(string dateText, string timeText, out DateTimeOffset time)
	{
		time = default;

		if (string.IsNullOrWhiteSpace(dateText))
		{
			return false;
		}

		if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) &&
		!DateTime.TryParse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out date))
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(timeText) || timeText.Contains("All Day", StringComparison.OrdinalIgnoreCase))
		{
			time = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
			return true;
		}

		if (DateTime.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedTime) ||
		DateTime.TryParse(timeText, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out parsedTime))
		{
			time = new DateTimeOffset(date.Year, date.Month, date.Day, parsedTime.Hour, parsedTime.Minute, 0, TimeSpan.Zero);
			return true;
		}

		return false;
	}

	private void CommitEvents(List<EconomicEvent> parsedEvents)
	{
		lock (_sync)
		{
			_events.Clear();
			_events.AddRange(parsedEvents.OrderBy(e => e.Time));
		}
	}

	private void CheckNewsWindow(DateTimeOffset currentTime)
	{
		var activeEvent = default(EconomicEvent);
		var upcomingEvent = default(EconomicEvent);
		var stopBefore = TimeSpan.FromMinutes(Math.Max(0, StopBeforeNewsMinutes));
		var startAfter = TimeSpan.FromMinutes(Math.Max(0, StartAfterNewsMinutes));
		var filterCurrencies = ParseCurrencies();
		var keyword = Keyword;
		var useKeyword = FilterByKeyword && !string.IsNullOrWhiteSpace(keyword);

		lock (_sync)
		{
			foreach (var evt in _events)
			{
				if (!IsImportanceAllowed(evt.Importance))
				{
					continue;
				}

				if (!IsCurrencyAllowed(evt.Currency, filterCurrencies))
				{
					continue;
				}

				if (useKeyword && !evt.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				var windowStart = evt.Time - stopBefore;
				var windowEnd = evt.Time + startAfter;

				if (currentTime >= windowStart && currentTime <= windowEnd)
				{
					activeEvent = evt;
					break;
				}

				if (evt.Time > currentTime)
				{
					upcomingEvent = evt;
					break;
				}
			}
		}

		UpdateStatus(activeEvent is not null, activeEvent ?? upcomingEvent);
	}

	private void UpdateStatus(bool isNewsActive, EconomicEvent referenceEvent)
	{
		_isNewsActive = isNewsActive;

		var message = isNewsActive
		? referenceEvent is not null
		? $"News time: {referenceEvent.Currency} {referenceEvent.Title} at {referenceEvent.Time:yyyy-MM-dd HH:mm}"
		: "News time..."
		: referenceEvent is not null
		? $"Next news: {referenceEvent.Currency} {referenceEvent.Title} at {referenceEvent.Time:yyyy-MM-dd HH:mm}"
		: "No upcoming news.";

		if (string.Equals(_lastStatus, message, StringComparison.Ordinal))
		{
			return;
		}

		_lastStatus = message;

		if (isNewsActive)
		{
			LogInfo(message);
		}
		else
		{
			LogDebug(message);
		}
	}

	private bool IsImportanceAllowed(string importance)
	{
		var stars = importance.Count(ch => ch == '*');
		return stars switch
		{
			0 => UseLowImportance || UseMediumImportance || UseHighImportance,
			1 => UseLowImportance,
			2 => UseMediumImportance,
			_ => UseHighImportance,
		};
	}

	private static bool IsCurrencyAllowed(string currency, HashSet<string> filterCurrencies)
	{
		if (filterCurrencies.Count == 0)
		{
			return true;
		}

		if (string.IsNullOrWhiteSpace(currency))
		{
			return false;
		}

		return filterCurrencies.Contains(currency.Trim().ToUpperInvariant());
	}

	private HashSet<string> ParseCurrencies()
	{
		var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var parts = (CurrenciesFilter ?? string.Empty)
		.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var part in parts)
		{
			var code = part.Trim();
			if (!string.IsNullOrEmpty(code))
			{
				currencies.Add(code.ToUpperInvariant());
			}
		}

		return currencies;
	}

	private static string NormalizeImportance(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return string.Empty;
		}

		var trimmed = raw.Trim();

		if (trimmed.All(ch => ch == '*'))
		{
			return trimmed;
		}

		if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
		{
			return new string('*', Math.Max(0, Math.Min(3, level)));
		}

		var stars = trimmed.Count(ch => ch == '*');
		if (stars > 0)
		{
			return new string('*', Math.Min(3, stars));
		}

		return trimmed;
	}

	private sealed class EconomicEvent
	{
		public DateTimeOffset Time { get; set; }
		public string Currency { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Importance { get; set; } = string.Empty;
		public string Actual { get; set; } = string.Empty;
		public string Forecast { get; set; } = string.Empty;
		public string Previous { get; set; } = string.Empty;
	}
}
