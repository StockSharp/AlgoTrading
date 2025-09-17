using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Xml.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Downloads high-impact economic news from Forex Factory and places pending straddle orders before each release.
/// </summary>
public class ReadNewsByWebRequestStrategy : Strategy
{
	private const string DefaultNewsUrl = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml";
	private static readonly HttpClient HttpClient = new();

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _buyDistancePoints;
	private readonly StrategyParam<decimal> _sellDistancePoints;
	private readonly StrategyParam<int> _leadMinutes;
	private readonly StrategyParam<int> _pendingExpirationMinutes;
	private readonly StrategyParam<int> _refreshMinutes;
	private readonly StrategyParam<bool> _showNewsLog;

	private readonly List<NewsEvent> _newsEvents = new();

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="ReadNewsByWebRequestStrategy"/> class.
	/// </summary>
	public ReadNewsByWebRequestStrategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 300m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 900m)
			.SetDisplay("Take Profit", "Take profit distance in points (0 disables)", "Risk");

		_buyDistancePoints = Param(nameof(BuyDistancePoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Distance", "Offset above the ask price for buy stops (points)", "Orders");

		_sellDistancePoints = Param(nameof(SellDistancePoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Distance", "Offset below the bid price for sell stops (points)", "Orders");

		_leadMinutes = Param(nameof(LeadMinutes), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lead Minutes", "Minutes before the release when pending orders are placed", "Schedule");

		_pendingExpirationMinutes = Param(nameof(PendingExpirationMinutes), 10)
			.SetDisplay("Expiration", "Lifetime of pending orders in minutes", "Orders");

		_refreshMinutes = Param(nameof(RefreshMinutes), 1)
			.SetGreaterThanZero()
			.SetDisplay("Refresh Minutes", "Interval between news feed downloads", "General");

		_showNewsLog = Param(nameof(ShowNewsLog), false)
			.SetDisplay("Log News", "Write the number of scheduled events to the log", "General");
	}

	/// <summary>
	/// Trade volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points. Zero disables the target.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Offset for buy stop orders expressed in points.
	/// </summary>
	public decimal BuyDistancePoints
	{
		get => _buyDistancePoints.Value;
		set => _buyDistancePoints.Value = value;
	}

	/// <summary>
	/// Offset for sell stop orders expressed in points.
	/// </summary>
	public decimal SellDistancePoints
	{
		get => _sellDistancePoints.Value;
		set => _sellDistancePoints.Value = value;
	}

	/// <summary>
	/// Minutes before the release when the straddle is submitted.
	/// </summary>
	public int LeadMinutes
	{
		get => _leadMinutes.Value;
		set => _leadMinutes.Value = value;
	}

	/// <summary>
	/// Minutes after placement before pending orders are cancelled. Zero keeps them indefinitely.
	/// </summary>
	public int PendingExpirationMinutes
	{
		get => _pendingExpirationMinutes.Value;
		set => _pendingExpirationMinutes.Value = value;
	}

	/// <summary>
	/// Interval between consecutive downloads of the news feed.
	/// </summary>
	public int RefreshMinutes
	{
		get => _refreshMinutes.Value;
		set => _refreshMinutes.Value = value;
	}

	/// <summary>
	/// When true, the strategy writes the number of high-impact releases to the log.
	/// </summary>
	public bool ShowNewsLog
	{
		get => _showNewsLog.Value;
		set => _showNewsLog.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_newsEvents.Clear();
		_bestBid = null;
		_bestAsk = null;
		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();
		if (_pointValue <= 0m)
		{
			AddWarningLog("Price step is unknown. Pending orders will not be placed.");
		}

		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints * _pointValue, UnitTypes.Absolute) : null;
		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * _pointValue, UnitTypes.Absolute) : null;
		if (stopLossUnit != null || takeProfitUnit != null)
		{
			StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);
		}

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		ReadNews();
		Timer.Start(TimeSpan.FromMinutes(Math.Max(1, RefreshMinutes)), ProcessTimer);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		{
			_bestBid = (decimal)bid;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		{
			_bestAsk = (decimal)ask;
		}

		HandleNewsEvents(level1.ServerTime);
	}

	private void ProcessTimer()
	{
		try
		{
			ReadNews();
		}
		catch (Exception ex)
		{
			AddWarningLog("Failed to download news data: {0}", ex.Message);
		}
	}

	private void HandleNewsEvents(DateTimeOffset currentTime)
	{
		for (var i = 0; i < _newsEvents.Count; i++)
		{
			var newsEvent = _newsEvents[i];

			if (!newsEvent.OrdersPlaced)
			{
				var lead = TimeSpan.FromMinutes(Math.Max(1, LeadMinutes));
				var windowStart = newsEvent.ReleaseTime - lead;

				if (currentTime >= windowStart && currentTime < newsEvent.ReleaseTime)
				{
					TryPlaceOrders(newsEvent, currentTime);
				}
			}
			else if (newsEvent.CancelTime is DateTimeOffset cancelTime && currentTime >= cancelTime)
			{
				CancelEventOrders(newsEvent);
			}

			if (currentTime >= newsEvent.ReleaseTime)
			{
				newsEvent.Completed = true;
			}

			if (currentTime >= newsEvent.ExpiryTime)
			{
				CancelEventOrders(newsEvent);
			}
		}

		for (var i = _newsEvents.Count - 1; i >= 0; i--)
		{
			var newsEvent = _newsEvents[i];
			if (!newsEvent.Completed)
			{
				continue;
			}

			var buyActive = newsEvent.BuyOrder is Order buy && buy.State == OrderStates.Active;
			var sellActive = newsEvent.SellOrder is Order sell && sell.State == OrderStates.Active;
			if (!buyActive && !sellActive)
			{
				_newsEvents.RemoveAt(i);
			}
		}
	}

	private void TryPlaceOrders(NewsEvent newsEvent, DateTimeOffset currentTime)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (_pointValue <= 0m)
		{
			return;
		}

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
		{
			return;
		}

		var volume = RoundVolume(Volume);
		if (volume <= 0m)
		{
			AddWarningLog("Volume is below the minimum allowed.");
			return;
		}

		var buyPrice = ask + BuyDistancePoints * _pointValue;
		var sellPrice = bid - SellDistancePoints * _pointValue;

		if (buyPrice <= 0m || sellPrice <= 0m)
		{
			return;
		}

		newsEvent.BuyOrder = BuyStop(volume, buyPrice);
		newsEvent.SellOrder = SellStop(volume, sellPrice);
		newsEvent.OrdersPlaced = true;

		if (PendingExpirationMinutes > 0)
		{
			newsEvent.CancelTime = currentTime + TimeSpan.FromMinutes(PendingExpirationMinutes);
		}
	}

	private void CancelEventOrders(NewsEvent newsEvent)
	{
		if (newsEvent.BuyOrder is Order buy && buy.State == OrderStates.Active)
		{
			CancelOrder(buy);
			newsEvent.BuyOrder = null;
		}

		if (newsEvent.SellOrder is Order sell && sell.State == OrderStates.Active)
		{
			CancelOrder(sell);
			newsEvent.SellOrder = null;
		}

		newsEvent.OrdersPlaced = false;
		newsEvent.CancelTime = null;
		newsEvent.Completed = true;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
		{
			return;
		}

		for (var i = 0; i < _newsEvents.Count; i++)
		{
			var newsEvent = _newsEvents[i];
			if (newsEvent.BuyOrder == order && order.State != OrderStates.Active)
			{
				newsEvent.BuyOrder = null;
			}
			else if (newsEvent.SellOrder == order && order.State != OrderStates.Active)
			{
				newsEvent.SellOrder = null;
			}
		}
	}

	private void ReadNews()
	{
		var response = HttpClient.GetStringAsync(DefaultNewsUrl).GetAwaiter().GetResult();
		if (string.IsNullOrWhiteSpace(response))
		{
			return;
		}

		var document = XDocument.Parse(response);
		var now = CurrentTime;
		var updated = new List<NewsEvent>();

		foreach (var element in document.Descendants("event"))
		{
			var impact = (element.Element("impact")?.Value ?? string.Empty).Trim();
			if (!impact.Equals("High", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!TryParseReleaseTime(element, out var releaseTime, out var expiryTime))
			{
				continue;
			}

			if (releaseTime <= now)
			{
				continue;
			}

			var title = (element.Element("title")?.Value ?? string.Empty).Trim();
			var country = (element.Element("country")?.Value ?? string.Empty).Trim();
			var forecast = (element.Element("forecast")?.Value ?? string.Empty).Trim();
			var previous = (element.Element("previous")?.Value ?? string.Empty).Trim();

			var existing = FindExistingEvent(title, releaseTime);
			if (existing != null)
			{
				existing.Country = country;
				existing.Forecast = forecast;
				existing.Previous = previous;
				existing.Impact = impact;
				existing.ExpiryTime = expiryTime;
				updated.Add(existing);
				continue;
			}

			updated.Add(new NewsEvent
			{
				Title = title,
				Country = country,
				Impact = impact,
				Forecast = forecast,
				Previous = previous,
				ReleaseTime = releaseTime,
				ExpiryTime = expiryTime
			});
		}

		_newsEvents.Clear();
		_newsEvents.AddRange(updated);

		if (ShowNewsLog)
		{
			AddInfoLog("{0} high-impact events scheduled.", _newsEvents.Count);
		}
	}

	private NewsEvent? FindExistingEvent(string title, DateTimeOffset release)
	{
		for (var i = 0; i < _newsEvents.Count; i++)
		{
			var existing = _newsEvents[i];
			if (existing.ReleaseTime == release && string.Equals(existing.Title, title, StringComparison.OrdinalIgnoreCase))
			{
				return existing;
			}
		}

		return null;
	}

	private decimal RoundVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		{
			return volume;
		}

		var step = security.VolumeStep ?? 0m;
		if (step <= 0m)
		{
			step = 1m;
		}

		var min = security.MinVolume ?? step;
		var max = security.MaxVolume ?? decimal.MaxValue;

		var rounded = Math.Round(volume / step) * step;
		if (rounded < min)
		{
			rounded = min;
		}
		if (rounded > max)
		{
			rounded = max;
		}

		return rounded;
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
		{
			return step;
		}

		var decimals = security?.Decimals ?? 0;
		return decimals > 0 ? (decimal)Math.Pow(10, -decimals) : 0.0001m;
	}

	private static bool TryParseReleaseTime(XElement element, out DateTimeOffset release, out DateTimeOffset expiry)
	{
		release = default;
		expiry = default;

		var dateText = (element.Element("date")?.Value ?? string.Empty).Trim();
		var timeText = (element.Element("time")?.Value ?? string.Empty).Trim();

		if (string.IsNullOrEmpty(dateText) || string.IsNullOrEmpty(timeText))
		{
			return false;
		}

		if (IsUnsupportedTime(timeText))
		{
			return false;
		}

		if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var datePart))
		{
			return false;
		}

		if (!TryParseTimeOfDay(timeText, out var timeOfDay))
		{
			return false;
		}

		var releaseDateTime = new DateTime(datePart.Year, datePart.Month, datePart.Day, timeOfDay.Hours, timeOfDay.Minutes, 0, DateTimeKind.Utc);
		release = new DateTimeOffset(releaseDateTime);
		expiry = release + TimeSpan.FromMinutes(15);
		return true;
	}

	private static bool TryParseTimeOfDay(string text, out TimeSpan result)
	{
		result = default;

		if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateTime))
		{
			result = dateTime.TimeOfDay;
			return true;
		}

		var formats = new[] { "H:mm", "HH:mm", "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt" };
		for (var i = 0; i < formats.Length; i++)
		{
			var format = formats[i];
			if (DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTime))
			{
				result = dateTime.TimeOfDay;
				return true;
			}
		}

		if (int.TryParse(text, out var hour) && hour >= 0 && hour <= 23)
		{
			result = TimeSpan.FromHours(hour);
			return true;
		}

		return false;
	}

	private static bool IsUnsupportedTime(string text)
	{
		var normalized = text.Trim();
		return normalized.Equals("All Day", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("All-Day", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Tentative", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Day", StringComparison.OrdinalIgnoreCase) ||
			normalized.Equals("Holiday", StringComparison.OrdinalIgnoreCase);
	}

	private sealed class NewsEvent
	{
		public string Title { get; set; } = string.Empty;
		public string Country { get; set; } = string.Empty;
		public string Impact { get; set; } = string.Empty;
		public string Forecast { get; set; } = string.Empty;
		public string Previous { get; set; } = string.Empty;
		public DateTimeOffset ReleaseTime { get; set; }
		public DateTimeOffset ExpiryTime { get; set; }
		public bool OrdersPlaced { get; set; }
		public bool Completed { get; set; }
		public DateTimeOffset? CancelTime { get; set; }
		public Order? BuyOrder { get; set; }
		public Order? SellOrder { get; set; }
	}
}
