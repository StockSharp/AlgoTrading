using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that pauses trading around economic news events.
/// News are filtered by importance and currency codes.
/// </summary>
public class NewsEAStrategy : Strategy
{
	private readonly StrategyParam<int> _beforeNewsMinutes;
	private readonly StrategyParam<int> _afterNewsMinutes;
	private readonly StrategyParam<bool> _includeLow;
	private readonly StrategyParam<bool> _includeMedium;
	private readonly StrategyParam<bool> _includeHigh;
	private readonly StrategyParam<string> _currencies;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<DateTimeOffset> _newsTimes = new();

	/// <summary>
	/// Minutes to stop trading before news.
	/// </summary>
	public int BeforeNewsMinutes
	{
		get => _beforeNewsMinutes.Value;
		set => _beforeNewsMinutes.Value = value;
	}

	/// <summary>
	/// Minutes to stop trading after news.
	/// </summary>
	public int AfterNewsMinutes
	{
		get => _afterNewsMinutes.Value;
		set => _afterNewsMinutes.Value = value;
	}

	/// <summary>
	/// Include low impact news.
	/// </summary>
	public bool IncludeLow
	{
		get => _includeLow.Value;
		set => _includeLow.Value = value;
	}

	/// <summary>
	/// Include medium impact news.
	/// </summary>
	public bool IncludeMedium
	{
		get => _includeMedium.Value;
		set => _includeMedium.Value = value;
	}

	/// <summary>
	/// Include high impact news.
	/// </summary>
	public bool IncludeHigh
	{
		get => _includeHigh.Value;
		set => _includeHigh.Value = value;
	}

	/// <summary>
	/// Comma separated list of currency codes.
	/// </summary>
	public string Currencies
	{
		get => _currencies.Value;
		set => _currencies.Value = value;
	}

	/// <summary>
	/// Candle type used to drive time processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NewsEAStrategy"/> class.
	/// </summary>
	public NewsEAStrategy()
	{
		_beforeNewsMinutes = Param(nameof(BeforeNewsMinutes), 5)
			.SetGreaterThanZero()
			.SetDisplay("Minutes Before", "Stop trading minutes before news", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 30, 1);

		_afterNewsMinutes = Param(nameof(AfterNewsMinutes), 5)
			.SetGreaterThanZero()
			.SetDisplay("Minutes After", "Stop trading minutes after news", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 30, 1);

		_includeLow = Param(nameof(IncludeLow), false)
			.SetDisplay("Include Low", "Include low impact news", "Filters");

		_includeMedium = Param(nameof(IncludeMedium), false)
			.SetDisplay("Include Medium", "Include medium impact news", "Filters");

		_includeHigh = Param(nameof(IncludeHigh), true)
			.SetDisplay("Include High", "Include high impact news", "Filters");

		_currencies = Param(nameof(Currencies), "USD,EUR,GBP,CHF,CAD,AUD,NZD,JPY")
			.SetDisplay("Currencies", "Comma separated currencies to monitor", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_newsTimes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to news for the selected security.
		Connector.SubscribeMarketData(Security, MarketDataTypes.News);

		// Subscribe to candles to drive time checks.
		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Remove news older than the after-news window.
		for (var i = _newsTimes.Count - 1; i >= 0; i--)
		{
			if ((candle.CloseTime - _newsTimes[i]).TotalMinutes > AfterNewsMinutes)
				_newsTimes.RemoveAt(i);
		}

		// Check if current time is within any news window.
		var inNews = false;
		foreach (var newsTime in _newsTimes)
		{
			if (newsTime - TimeSpan.FromMinutes(BeforeNewsMinutes) <= candle.CloseTime &&
				newsTime + TimeSpan.FromMinutes(AfterNewsMinutes) >= candle.CloseTime)
			{
				inNews = true;
				break;
			}
		}

		if (inNews)
		{
			// Trading is paused due to news.
			LogInfo("News time");
		}
		else
		{
			// No news, normal trading can continue.
			LogInfo("No news");
		}
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		base.OnProcessMessage(message);

		if (message.Type != MessageTypes.News)
			return;

		var news = (NewsMessage)message;
		if (!IsNewsAccepted(news))
			return;

		_newsTimes.Add(news.ServerTime);
		LogInfo($"Upcoming news at {news.ServerTime:O}: {news.Headline}");
	}

	private bool IsNewsAccepted(NewsMessage news)
	{
		var text = (news.Headline + " " + news.Story)?.ToUpperInvariant();
		if (text == null)
			return false;

		// Check currency codes in news text.
		var currencies = _currencies.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
		var foundCurrency = false;
		foreach (var code in currencies)
		{
			var trimmed = code.Trim().ToUpperInvariant();
			if (trimmed.Length > 0 && text.Contains(trimmed))
			{
				foundCurrency = true;
				break;
			}
		}

		if (!foundCurrency)
			return false;

		// Filter by importance keywords.
		if (IncludeHigh && text.Contains("HIGH"))
			return true;
		if (IncludeMedium && text.Contains("MODERATE"))
			return true;
		if (IncludeLow && text.Contains("LOW"))
			return true;

		return false;
	}
}
