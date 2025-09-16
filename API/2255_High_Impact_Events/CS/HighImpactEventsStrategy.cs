using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that downloads high impact events from ForexFactory
/// and notifies a few minutes before they occur.
/// </summary>
public class HighImpactEventsStrategy : Strategy
{
	private readonly StrategyParam<int> _alertBeforeMinutes;
	private readonly List<EconomicEvent> _events = new();
	private Timer? _timer;

	/// <summary>
	/// Minutes before the event to trigger alert.
	/// </summary>
	public int AlertBeforeMinutes
	{
		get => _alertBeforeMinutes.Value;
		set => _alertBeforeMinutes.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="HighImpactEventsStrategy"/>.
	/// </summary>
	public HighImpactEventsStrategy()
	{
		_alertBeforeMinutes = Param(nameof(AlertBeforeMinutes), 5)
			.SetDisplay("Alert Before Minutes")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		LoadEvents();

		// Check events every minute.
		_timer = new Timer(_ => CheckEvents(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		_timer?.Dispose();
		_timer = null;
	}

	// Download and parse the economic calendar.
	private async void LoadEvents()
	{
		using var client = new HttpClient();

		var url = $"https://www.forexfactory.com/calendar.php?day={DateTime.UtcNow:MMMdd.yyyy}";
		try
		{
			var html = await client.GetStringAsync(url);
			ParseEvents(html);
		}
		catch (Exception ex)
		{
			AddErrorLog(ex.Message);
		}
	}

	// Extract events with high impact from the HTML.
	private void ParseEvents(string html)
	{
		var rowRegex = new Regex("<tr[\s\S]*?</tr>", RegexOptions.Compiled);
		foreach (Match row in rowRegex.Matches(html))
		{
			var rowText = row.Value;
			if (!rowText.Contains("calendar__impact--high"))
				continue;

			var time = Regex.Match(rowText, "calendar__time[^>]*>([^<]*)</td>").Groups[1].Value.Trim();
			var currency = Regex.Match(rowText, "calendar__currency[^>]*>([^<]*)</td>").Groups[1].Value.Trim();
			var title = Regex.Match(rowText, "calendar__event[^>]*>([^<]*)</td>").Groups[1].Value.Trim();

			if (!DateTime.TryParseExact(time, "h:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime) &&
				!DateTime.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedTime))
				continue;

			var date = DateTime.UtcNow.Date;
			var eventTime = new DateTimeOffset(date.Year, date.Month, date.Day, parsedTime.Hour, parsedTime.Minute, 0, TimeSpan.Zero);

			_events.Add(new EconomicEvent
			{
				Time = eventTime,
				Title = title,
				Currency = currency,
				Displayed = false
			});
		}
	}

	// Check upcoming events and create alerts.
	private void CheckEvents()
	{
		var now = DateTimeOffset.UtcNow;
		foreach (var ev in _events)
		{
			if (ev.Displayed)
				continue;

			if (now >= ev.Time - TimeSpan.FromMinutes(AlertBeforeMinutes) && now < ev.Time)
			{
				ev.Displayed = true;
				AddInfoLog($"{ev.Title} ({ev.Currency}) in {AlertBeforeMinutes} minutes.");
			}
		}
	}

	private sealed class EconomicEvent
	{
		public DateTimeOffset Time { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Currency { get; set; } = string.Empty;
		public bool Displayed { get; set; }
	}
}
