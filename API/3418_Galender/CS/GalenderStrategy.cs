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
/// Economic calendar filter that mirrors the Galender MQL5 script.
/// It collects news messages within a date range and logs matches.
/// </summary>
public class GalenderStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _dateFrom;
	private readonly StrategyParam<DateTimeOffset> _dateTo;
	private readonly StrategyParam<string> _currencyFilter;
	private readonly StrategyParam<string> _keywordFilter;
	private readonly StrategyParam<CalendarImportanceFilters> _importanceFilter;

	private readonly List<CalendarEntry> _entries = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="GalenderStrategy"/> class.
	/// </summary>
	public GalenderStrategy()
	{
		_dateFrom = Param(nameof(DateFrom), new DateTimeOffset(2020, 7, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Date From", "Earliest news timestamp (UTC)", "Filters");

		_dateTo = Param(nameof(DateTo), new DateTimeOffset(2020, 9, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Date To", "Latest news timestamp (UTC)", "Filters");

		_currencyFilter = Param(nameof(CurrencyFilter), "USD")
			.SetDisplay("Currency Filter", "Comma separated currency codes to match", "Filters");

		_keywordFilter = Param(nameof(KeywordFilter), "interest")
			.SetDisplay("Keyword Filter", "Keyword that must exist in the news text", "Filters");

		_importanceFilter = Param(nameof(ImportanceFilter), CalendarImportanceFilters.All)
			.SetDisplay("Importance", "Economic importance requirement", "Filters");
	}

	/// <summary>
	/// Start date for the calendar scan.
	/// </summary>
	public DateTimeOffset DateFrom
	{
		get => _dateFrom.Value;
		set => _dateFrom.Value = value;
	}

	/// <summary>
	/// End date for the calendar scan.
	/// </summary>
	public DateTimeOffset DateTo
	{
		get => _dateTo.Value;
		set => _dateTo.Value = value;
	}

	/// <summary>
	/// Comma separated list of currency codes to search for.
	/// </summary>
	public string CurrencyFilter
	{
		get => _currencyFilter.Value;
		set => _currencyFilter.Value = value;
	}

	/// <summary>
	/// Keyword that must be found in the news headline or story.
	/// </summary>
	public string KeywordFilter
	{
		get => _keywordFilter.Value;
		set => _keywordFilter.Value = value;
	}

	/// <summary>
	/// Importance level that must be present in the news text.
	/// </summary>
	public CalendarImportanceFilters ImportanceFilter
	{
		get => _importanceFilter.Value;
		set => _importanceFilter.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entries.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_entries.Clear();

		if (Security != null)
		{
			// Subscribe to economic news for the selected security.
			Connector.SubscribeMarketData(Security, MarketDataTypes.News);
		}

		LogInfo("Galender strategy is waiting for economic calendar events.");
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		if (Security != null)
		{
			Connector.UnSubscribeMarketData(Security, MarketDataTypes.News);
		}
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		base.OnProcessMessage(message);

		if (message.Type != MessageTypes.News)
		{
			return;
		}

		var news = (NewsMessage)message;
		var time = news.ServerTime;

		if (time < DateFrom || time > DateTo)
		{
			return;
		}

		var text = ((news.Headline ?? string.Empty) + " " + (news.Story ?? string.Empty)).Trim();

		if (text.IsEmpty())
		{
			return;
		}

		if (!MatchesCurrency(text))
		{
			return;
		}

		if (!MatchesKeyword(text))
		{
			return;
		}

		if (!MatchesImportance(text))
		{
			return;
		}

		var currency = ExtractCurrency(text);
		var impact = ExtractImpact(text);
		var previous = ExtractMetric(text, "Previous");
		var forecast = ExtractMetric(text, "Forecast");
		var actual = ExtractMetric(text, "Actual");

		var entry = new CalendarEntry(time, currency, news.Headline ?? string.Empty, impact, previous, forecast, actual);

		_entries.Add(entry);
		_entries.Sort((l, r) => l.Time.CompareTo(r.Time));

		LogInfo($"Calendar match: {entry.Time:yyyy-MM-dd HH:mm} {entry.Currency} {entry.EventCode}. Impact={entry.Impact}, Prev={entry.Previous}, Forecast={entry.Forecast}, Actual={entry.Actual}");
	}

	private bool MatchesCurrency(string text)
	{
		var filter = CurrencyFilter;

		if (filter.IsEmptyOrWhiteSpace())
		{
			return true;
		}

		var upperText = text.ToUpperInvariant();
		var tokens = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (var token in tokens)
		{
			if (ContainsWord(upperText, token.ToUpperInvariant()))
			{
				return true;
			}
		}

		return false;
	}

	private bool MatchesKeyword(string text)
	{
		var keyword = KeywordFilter;

		if (keyword.IsEmptyOrWhiteSpace())
		{
			return true;
		}

		return text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private bool MatchesImportance(string text)
	{
		var importance = ImportanceFilter;
		if (importance == CalendarImportanceFilters.All)
		{
			return true;
		}

		var upperText = text.ToUpperInvariant();

		return importance switch
		{
			CalendarImportanceFilters.None => ContainsWord(upperText, "NONE"),
			CalendarImportanceFilters.Low => ContainsWord(upperText, "LOW"),
			CalendarImportanceFilters.Moderate => ContainsWord(upperText, "MODERATE") || ContainsWord(upperText, "MEDIUM"),
			CalendarImportanceFilters.High => ContainsWord(upperText, "HIGH"),
			_ => true
		};
	}

	private string ExtractCurrency(string text)
	{
		var filter = CurrencyFilter;
		if (filter.IsEmptyOrWhiteSpace())
		{
			return string.Empty;
		}

		var tokens = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var upperText = text.ToUpperInvariant();

		foreach (var token in tokens)
		{
			var upperToken = token.ToUpperInvariant();
			if (ContainsWord(upperText, upperToken))
			{
				return upperToken;
			}
		}

		return tokens.Length > 0 ? tokens[0].ToUpperInvariant() : string.Empty;
	}

	private string ExtractImpact(string text)
	{
		var upperText = text.ToUpperInvariant();

		if (ContainsWord(upperText, "NEGATIVE"))
		{
			return "Negative";
		}

		if (ContainsWord(upperText, "POSITIVE"))
		{
			return "Positive";
		}

		if (ContainsWord(upperText, "NONE"))
		{
			return "None";
		}

		return string.Empty;
	}

	private static string ExtractMetric(string text, string label)
	{
		var index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
		if (index < 0)
		{
			return string.Empty;
		}

		index += label.Length;

		while (index < text.Length && char.IsWhiteSpace(text[index]))
		{
			index++;
		}

		if (index >= text.Length)
		{
			return string.Empty;
		}

		if (text[index] == ':' || text[index] == '=')
		{
			index++;
		}

		while (index < text.Length && char.IsWhiteSpace(text[index]))
		{
			index++;
		}

		var end = index;

		while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.' || text[end] == ',' || text[end] == '-' || text[end] == '%'))
		{
			end++;
		}

		return text[index:end].Trim();
	}

	private static bool ContainsWord(string text, string word)
	{
		var startIndex = 0;

		while (startIndex < text.Length)
		{
			var index = text.IndexOf(word, startIndex, StringComparison.OrdinalIgnoreCase);
			if (index < 0)
			{
				return false;
			}

			var beforeOk = index == 0 || !char.IsLetter(text[index - 1]);
			var afterIndex = index + word.Length;
			var afterOk = afterIndex >= text.Length || !char.IsLetter(text[afterIndex]);

			if (beforeOk && afterOk)
			{
				return true;
			}

			startIndex = index + word.Length;
		}

		return false;
	}

	private sealed record class CalendarEntry(DateTimeOffset Time, string Currency, string EventCode, string Impact, string Previous, string Forecast, string Actual);

	/// <summary>
	/// Enumeration of supported importance filters.
	/// </summary>
	public enum CalendarImportanceFilters
	{
		/// <summary>
		/// No importance filter (matches only events tagged with "none").
		/// </summary>
		None,

		/// <summary>
		/// Low importance events.
		/// </summary>
		Low,

		/// <summary>
		/// Moderate importance events.
		/// </summary>
		Moderate,

		/// <summary>
		/// High importance events.
		/// </summary>
		High,

		/// <summary>
		/// Allow all importance values.
		/// </summary>
		All
	}
}

