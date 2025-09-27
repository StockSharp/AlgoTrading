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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors price crossings of user-defined horizontal lines and trendlines.
/// </summary>
public class TrendlineCrossAlertStrategy : Strategy
{
	private readonly StrategyParam<string> _monitoringColor;
	private readonly StrategyParam<string> _crossedColor;
	private readonly StrategyParam<string> _horizontalLevelsInput;
	private readonly StrategyParam<string> _trendlineDefinitionsInput;
	private readonly StrategyParam<bool> _enableAlerts;
	private readonly StrategyParam<bool> _enableNotifications;
	private readonly StrategyParam<bool> _enableEmails;
	private readonly StrategyParam<DataType> _candleType;

	private HorizontalLevel[] _horizontalLevels = Array.Empty<HorizontalLevel>();
	private TrendlineDefinition[] _trendlines = Array.Empty<TrendlineDefinition>();
	private readonly HashSet<string> _crossedHorizontal = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _crossedTrendlines = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of <see cref="TrendlineCrossAlertStrategy"/>.
	/// </summary>
	public TrendlineCrossAlertStrategy()
	{
		_monitoringColor = Param(nameof(MonitoringColor), "Yellow")
			.SetDisplay("Monitoring Color", "Only lines with this color are monitored", "Line Filters");

		_crossedColor = Param(nameof(CrossedColor), "Green")
			.SetDisplay("Crossed Color", "Color assigned to lines after they are crossed", "Line Filters");

		_horizontalLevelsInput = Param(nameof(HorizontalLevelsInput), "Pivot|Yellow|1.0985;Resistance|Yellow|1.1050")
			.SetDisplay("Horizontal Levels", "Semicolon separated list in the form Name|Color|Price", "Line Definitions");

		_trendlineDefinitionsInput = Param(nameof(TrendlineDefinitions), "TL1|Yellow|2024-01-01T00:00:00Z|1.0950|2024-01-01T06:00:00Z|1.1100")
			.SetDisplay("Trendlines", "Semicolon separated list in the form Name|Color|StartTime|StartPrice|EndTime|EndPrice", "Line Definitions");

		_enableAlerts = Param(nameof(EnableAlerts), true)
			.SetDisplay("Enable Alerts", "Write alert messages to the strategy log", "Notifications");

		_enableNotifications = Param(nameof(EnableNotifications), false)
			.SetDisplay("Enable Notifications", "Simulate push notifications using log messages", "Notifications");

		_enableEmails = Param(nameof(EnableEmails), false)
			.SetDisplay("Enable Emails", "Simulate email alerts using log messages", "Notifications");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for monitoring", "General");
	}

	/// <summary>
	/// Color filter for monitored lines.
	/// </summary>
	public string MonitoringColor
	{
		get => _monitoringColor.Value;
		set => _monitoringColor.Value = value;
	}

	/// <summary>
	/// Color applied to lines after they are crossed.
	/// </summary>
	public string CrossedColor
	{
		get => _crossedColor.Value;
		set => _crossedColor.Value = value;
	}

	/// <summary>
	/// Raw definition string for horizontal levels.
	/// </summary>
	public string HorizontalLevelsInput
	{
		get => _horizontalLevelsInput.Value;
		set => _horizontalLevelsInput.Value = value;
	}

	/// <summary>
	/// Raw definition string for trendlines.
	/// </summary>
	public string TrendlineDefinitions
	{
		get => _trendlineDefinitionsInput.Value;
		set => _trendlineDefinitionsInput.Value = value;
	}

	/// <summary>
	/// Enables writing alert messages.
	/// </summary>
	public bool EnableAlerts
	{
		get => _enableAlerts.Value;
		set => _enableAlerts.Value = value;
	}

	/// <summary>
	/// Enables simulated push notifications.
	/// </summary>
	public bool EnableNotifications
	{
		get => _enableNotifications.Value;
		set => _enableNotifications.Value = value;
	}

	/// <summary>
	/// Enables simulated email notifications.
	/// </summary>
	public bool EnableEmails
	{
		get => _enableEmails.Value;
		set => _enableEmails.Value = value;
	}

	/// <summary>
	/// Candle type used for monitoring price crosses.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_crossedHorizontal.Clear();
		_crossedTrendlines.Clear();

		_horizontalLevels = ParseHorizontalLevels(HorizontalLevelsInput, MonitoringColor);
		_trendlines = ParseTrendlines(TrendlineDefinitions, MonitoringColor);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		foreach (var level in _horizontalLevels)
		{
			if (_crossedHorizontal.Contains(level.Name))
				continue;

			if (!TryGetCrossDirection(open, close, level.Price, out var direction))
				continue;

			_crossedHorizontal.Add(level.Name);
			NotifyCross(level.Name, "horizontal line", direction, level.Price, candle.CloseTime, close);
		}

		foreach (var line in _trendlines)
		{
			if (_crossedTrendlines.Contains(line.Name))
				continue;

			var price = line.GetPrice(candle.CloseTime);
			if (price is null)
				continue;

			if (!TryGetCrossDirection(open, close, price.Value, out var direction))
				continue;

			_crossedTrendlines.Add(line.Name);
			NotifyCross(line.Name, "trendline", direction, price.Value, candle.CloseTime, close);
		}
	}

	private void NotifyCross(string name, string type, string direction, decimal linePrice, DateTimeOffset time, decimal closePrice)
	{
		// Build a human-readable message that mirrors the original MQL notifications.
		var message = $"Price crossed {type} '{name}' {direction} at {linePrice:0.#####} on {time:yyyy-MM-dd HH:mm:ss}. Close price {closePrice:0.#####}. Marking line as {CrossedColor}.";

		if (EnableAlerts)
			LogInfo(message);

		if (EnableNotifications)
			LogInfo($"Push notification: {message}");

		if (EnableEmails)
			LogInfo($"Email notification: {message}");
	}

	private static bool TryGetCrossDirection(decimal open, decimal close, decimal target, out string direction)
	{
		// Upward cross occurs when the candle opens at or below the line and closes above it.
		if (open <= target && close > target)
		{
			direction = "upward";
			return true;
		}

		// Downward cross occurs when the candle opens at or above the line and closes below it.
		if (open >= target && close < target)
		{
			direction = "downward";
			return true;
		}

		direction = string.Empty;
		return false;
	}

	private static HorizontalLevel[] ParseHorizontalLevels(string input, string monitoringColor)
	{
		// Convert the textual definition into strongly typed horizontal levels.
		if (input.IsEmptyOrWhiteSpace())
			return Array.Empty<HorizontalLevel>();

		var segments = input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var result = new List<HorizontalLevel>();
		var index = 1;

		foreach (var segment in segments)
		{
			var parts = segment.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			string name;
			string color;
			string pricePart;

			switch (parts.Length)
			{
				case 1:
					name = $"Horizontal {index}";
					color = monitoringColor;
					pricePart = parts[0];
					break;

				case 2:
					name = parts[0];
					color = monitoringColor;
					pricePart = parts[1];
					break;

				default:
					name = parts[0];
					color = parts[1];
					pricePart = parts[2];
					break;
			}

			if (!color.EqualsIgnoreCase(monitoringColor))
			{
				index++;
				continue;
			}

			if (!decimal.TryParse(pricePart, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
			{
				index++;
				continue;
			}

			if (name.IsEmptyOrWhiteSpace())
				name = $"Horizontal {index}";

			result.Add(new HorizontalLevel(name, color, price));
			index++;
		}

		return result.ToArray();
	}

	private static TrendlineDefinition[] ParseTrendlines(string input, string monitoringColor)
	{
		// Convert the textual definition into strongly typed trendlines with two anchor points.
		if (input.IsEmptyOrWhiteSpace())
			return Array.Empty<TrendlineDefinition>();

		var segments = input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var result = new List<TrendlineDefinition>();
		var index = 1;

		foreach (var segment in segments)
		{
			var parts = segment.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			string name;
			string color;
			string startTimePart;
			string startPricePart;
			string endTimePart;
			string endPricePart;

			if (parts.Length == 5)
			{
				name = parts[0];
				color = monitoringColor;
				startTimePart = parts[1];
				startPricePart = parts[2];
				endTimePart = parts[3];
				endPricePart = parts[4];
			}
			else if (parts.Length >= 6)
			{
				name = parts[0];
				color = parts[1];
				startTimePart = parts[2];
				startPricePart = parts[3];
				endTimePart = parts[4];
				endPricePart = parts[5];
			}
			else
			{
				index++;
				continue;
			}

			if (!color.EqualsIgnoreCase(monitoringColor))
			{
				index++;
				continue;
			}

			if (!DateTimeOffset.TryParse(startTimePart, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var startTime))
			{
				index++;
				continue;
			}

			if (!DateTimeOffset.TryParse(endTimePart, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var endTime))
			{
				index++;
				continue;
			}

			if (!decimal.TryParse(startPricePart, NumberStyles.Number, CultureInfo.InvariantCulture, out var startPrice) ||
				!decimal.TryParse(endPricePart, NumberStyles.Number, CultureInfo.InvariantCulture, out var endPrice))
			{
				index++;
				continue;
			}

			if (startTime == endTime)
			{
				index++;
				continue;
			}

			if (name.IsEmptyOrWhiteSpace())
				name = $"Trendline {index}";

			result.Add(new TrendlineDefinition(name, color, startTime, startPrice, endTime, endPrice));
			index++;
		}

		return result.ToArray();
	}

	private sealed record HorizontalLevel(string Name, string Color, decimal Price);

	private sealed record TrendlineDefinition(string Name, string Color, DateTimeOffset StartTime, decimal StartPrice, DateTimeOffset EndTime, decimal EndPrice)
	{
		public decimal? GetPrice(DateTimeOffset time)
		{
			// Trendlines in MetaTrader extend beyond their anchor points, so we allow extrapolation here as well.
			var totalSeconds = (decimal)(EndTime - StartTime).TotalSeconds;
			if (totalSeconds == 0m)
				return null;

			var elapsedSeconds = (decimal)(time - StartTime).TotalSeconds;
			var slope = (EndPrice - StartPrice) / totalSeconds;
			return StartPrice + slope * elapsedSeconds;
		}
	}
}