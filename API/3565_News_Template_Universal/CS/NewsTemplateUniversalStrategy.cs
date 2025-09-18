using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the News Template Universal MQL advisor.
/// It pauses trading around economic news events based on configurable filters.
/// </summary>
public class NewsTemplateUniversalStrategy : Strategy
{
	private readonly StrategyParam<bool> _useNewsFilter;
	private readonly StrategyParam<bool> _includeLow;
	private readonly StrategyParam<bool> _includeMedium;
	private readonly StrategyParam<bool> _includeHigh;
	private readonly StrategyParam<int> _stopBeforeMinutes;
	private readonly StrategyParam<int> _startAfterMinutes;
	private readonly StrategyParam<string> _currencies;
	private readonly StrategyParam<bool> _checkSpecificNews;
	private readonly StrategyParam<string> _specificNewsText;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<NewsEvent> _newsEvents = new();

	private bool _isNewsActive;

	/// <summary>
	/// Initializes a new instance of the <see cref="NewsTemplateUniversalStrategy"/> class.
	/// </summary>
	public NewsTemplateUniversalStrategy()
	{
		_useNewsFilter = Param(nameof(UseNewsFilter), true)
			.SetDisplay("Use News Filter", "Enable blocking trades around news", "General");

		_includeLow = Param(nameof(IncludeLow), false)
			.SetDisplay("Include Low", "Include low importance events", "Filters");

		_includeMedium = Param(nameof(IncludeMedium), true)
			.SetDisplay("Include Medium", "Include medium importance events", "Filters");

		_includeHigh = Param(nameof(IncludeHigh), true)
			.SetDisplay("Include High", "Include high importance events", "Filters");

		_stopBeforeMinutes = Param(nameof(StopBeforeNewsMinutes), 30)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Minutes Before", "Minutes to stop before news", "Timing");

		_startAfterMinutes = Param(nameof(StartAfterNewsMinutes), 30)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Minutes After", "Minutes to resume after news", "Timing");

		_currencies = Param(nameof(Currencies), "USD,EUR,CAD,AUD,NZD,GBP")
			.SetDisplay("Currencies", "Comma separated currency codes", "Filters");

		_checkSpecificNews = Param(nameof(CheckSpecificNews), false)
			.SetDisplay("Filter by text", "Require specific text in news", "Filters");

		_specificNewsText = Param(nameof(SpecificNewsText), "employment")
			.SetDisplay("Text filter", "Substring that must be present when enabled", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for time checks", "General");
	}

	/// <summary>
	/// Enable or disable the news based blocking logic.
	/// </summary>
	public bool UseNewsFilter
	{
		get => _useNewsFilter.Value;
		set => _useNewsFilter.Value = value;
	}

	/// <summary>
	/// Include low importance news events.
	/// </summary>
	public bool IncludeLow
	{
		get => _includeLow.Value;
		set => _includeLow.Value = value;
	}

	/// <summary>
	/// Include medium importance news events.
	/// </summary>
	public bool IncludeMedium
	{
		get => _includeMedium.Value;
		set => _includeMedium.Value = value;
	}

	/// <summary>
	/// Include high importance news events.
	/// </summary>
	public bool IncludeHigh
	{
		get => _includeHigh.Value;
		set => _includeHigh.Value = value;
	}

	/// <summary>
	/// Minutes to stop trading before a news event.
	/// </summary>
	public int StopBeforeNewsMinutes
	{
		get => _stopBeforeMinutes.Value;
		set => _stopBeforeMinutes.Value = value;
	}

	/// <summary>
	/// Minutes to resume trading after a news event.
	/// </summary>
	public int StartAfterNewsMinutes
	{
		get => _startAfterMinutes.Value;
		set => _startAfterMinutes.Value = value;
	}

	/// <summary>
	/// Comma separated list of currency tickers searched inside news text.
	/// </summary>
	public string Currencies
	{
		get => _currencies.Value;
		set => _currencies.Value = value;
	}

	/// <summary>
	/// Enable filtering for a specific text fragment.
	/// </summary>
	public bool CheckSpecificNews
	{
		get => _checkSpecificNews.Value;
		set => _checkSpecificNews.Value = value;
	}

	/// <summary>
	/// Text fragment required when <see cref="CheckSpecificNews"/> is enabled.
	/// </summary>
	public string SpecificNewsText
	{
		get => _specificNewsText.Value;
		set => _specificNewsText.Value = value;
	}

	/// <summary>
	/// Candle type used to drive time based processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Gets a value indicating whether news currently block trading operations.
	/// </summary>
	public bool IsNewsActive => _isNewsActive;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_newsEvents.Clear();
		_isNewsActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Connector.SubscribeMarketData(Security, MarketDataTypes.News);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		Connector.UnSubscribeMarketData(Security, MarketDataTypes.News);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var now = candle.CloseTime;
		var afterWindow = TimeSpan.FromMinutes(StartAfterNewsMinutes);

		for (var i = _newsEvents.Count - 1; i >= 0; i--)
		{
			if ((now - _newsEvents[i].Time) > afterWindow)
			_newsEvents.RemoveAt(i);
		}

		if (!UseNewsFilter)
		{
			if (_isNewsActive)
			{
				_isNewsActive = false;
				LogInfo("News filter disabled.");
			}

			return;
		}

		var beforeWindow = TimeSpan.FromMinutes(StopBeforeNewsMinutes);
		var active = false;

		foreach (var evt in _newsEvents)
		{
			if (now >= evt.Time - beforeWindow && now <= evt.Time + afterWindow)
			{
				active = true;
				break;
			}
		}

		if (active != _isNewsActive)
		{
			_isNewsActive = active;

			if (_isNewsActive)
			{
				LogInfo("News time...");
			}
			else
			{
				LogInfo("No news");
			}
		}
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		base.OnProcessMessage(message);

		if (message.Type != MessageTypes.News)
		return;

		var news = (NewsMessage)message;
		var importance = ParseImportance(news);

		if (!IsImportanceAccepted(importance))
		return;

		if (!MatchesCurrency(news))
		return;

		if (CheckSpecificNews && !MatchesSpecificText(news))
		return;

		_newsEvents.Add(new NewsEvent(news.ServerTime, importance, BuildHeadline(news)));
		_newsEvents.Sort(static (l, r) => l.Time.CompareTo(r.Time));

		LogInfo($"Upcoming news at {news.ServerTime:O}: {news.Headline}");
	}

	private static string BuildHeadline(NewsMessage news)
	{
		var headline = news.Headline ?? string.Empty;
		var story = news.Story ?? string.Empty;

		if (string.IsNullOrEmpty(headline))
		return story;

		if (string.IsNullOrEmpty(story))
		return headline;

		return $"{headline} - {story}";
	}

	private string BuildNormalizedText(NewsMessage news)
	{
		var headline = news.Headline ?? string.Empty;
		var story = news.Story ?? string.Empty;
		var source = news.Source ?? string.Empty;

		var combined = string.Join(' ', new[] { headline, story, source });

		return combined.ToUpperInvariant();
	}

	private bool MatchesCurrency(NewsMessage news)
	{
		var text = BuildNormalizedText(news);

		if (string.IsNullOrWhiteSpace(text))
		return false;

		var tokens = _currencies.Value
			.Split(',', StringSplitOptions.RemoveEmptyEntries);

		if (tokens.Length == 0)
		return true;

		foreach (var token in tokens)
		{
			var upper = token.Trim().ToUpperInvariant();

			if (upper.Length == 0)
			continue;

			if (text.IndexOf(upper, StringComparison.Ordinal) >= 0)
			return true;
		}

		return false;
	}

	private bool MatchesSpecificText(NewsMessage news)
	{
		if (!CheckSpecificNews)
		return true;

		var filter = SpecificNewsText;

		if (string.IsNullOrWhiteSpace(filter))
		return false;

		var text = BuildNormalizedText(news);

		return text.IndexOf(filter.Trim().ToUpperInvariant(), StringComparison.Ordinal) >= 0;
	}

	private NewsImportance ParseImportance(NewsMessage news)
	{
		var text = BuildNormalizedText(news);

		if (text.IndexOf("***", StringComparison.Ordinal) >= 0)
		return NewsImportance.High;

		if (text.IndexOf("**", StringComparison.Ordinal) >= 0)
		return NewsImportance.Medium;

		if (text.Contains('*'))
		return NewsImportance.Low;

		if (text.IndexOf("HIGH", StringComparison.Ordinal) >= 0)
		return NewsImportance.High;

		if (text.IndexOf("MEDIUM", StringComparison.Ordinal) >= 0 || text.IndexOf("MODERATE", StringComparison.Ordinal) >= 0)
		return NewsImportance.Medium;

		if (text.IndexOf("LOW", StringComparison.Ordinal) >= 0)
		return NewsImportance.Low;

		return NewsImportance.Unknown;
	}

	private bool IsImportanceAccepted(NewsImportance importance)
	{
		return importance switch
		{
			NewsImportance.High => IncludeHigh,
			NewsImportance.Medium => IncludeMedium,
			NewsImportance.Low => IncludeLow,
			NewsImportance.Unknown => IncludeHigh || IncludeMedium || IncludeLow,
			_ => false,
		};
	}

	private sealed record class NewsEvent(DateTimeOffset Time, NewsImportance Importance, string Headline);

	private enum NewsImportance
	{
		Unknown = 0,
		Low = 1,
		Medium = 2,
		High = 3
	}
}
