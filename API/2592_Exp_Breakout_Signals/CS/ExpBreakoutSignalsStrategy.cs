using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sends breakout alerts when price crosses user defined horizontal levels.
/// </summary>
public enum BreakoutNotificationMode
{
	Sound,
	Alert,
	Push,
	Mail,
}

/// <summary>
/// Converts the EXP Breakout Signals indicator into a StockSharp strategy.
/// </summary>
public class ExpBreakoutSignalsStrategy : Strategy
{
	// Separators used to split the price level string.
	private static readonly char[] LevelSeparators = new[] { ';', ',', '
', '', '	', ' ' };

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _prefix;
	private readonly StrategyParam<BreakoutNotificationMode> _signalMode;
	private readonly StrategyParam<string> _soundName;
	private readonly StrategyParam<string> _levels;
	private readonly StrategyParam<bool> _clearOnStop;

	private decimal[] _parsedLevels = Array.Empty<decimal>();
	private decimal _previousOpen;
	private bool _hasPreviousOpen;

	/// <summary>
	/// Timeframe used for monitoring breakouts.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Prefix added to generated alert messages.
	/// </summary>
	public string Prefix
	{
		get => _prefix.Value;
		set => _prefix.Value = value;
	}

	/// <summary>
	/// Notification type describing how alerts should be presented.
	/// </summary>
	public BreakoutNotificationMode SignalMode
	{
		get => _signalMode.Value;
		set => _signalMode.Value = value;
	}

	/// <summary>
	/// File name referenced when the sound mode is active.
	/// </summary>
	public string SoundName
	{
		get => _soundName.Value;
		set => _soundName.Value = value;
	}

	/// <summary>
	/// Delimited list of horizontal price levels to monitor.
	/// </summary>
	public string Levels
	{
		get => _levels.Value;
		set => _levels.Value = value;
	}

	/// <summary>
	/// If true the cached level list will be cleared once the strategy stops.
	/// </summary>
	public bool ClearLevelsOnStop
	{
		get => _clearOnStop.Value;
		set => _clearOnStop.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ExpBreakoutSignalsStrategy"/>.
	/// </summary>
	public ExpBreakoutSignalsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for breakout detection", "General");

		_prefix = Param(nameof(Prefix), "bot_")
			.SetDisplay("Prefix", "Text prefix for generated alerts", "Notifications");

		_signalMode = Param(nameof(SignalMode), BreakoutNotificationMode.Sound)
			.SetDisplay("Signal Mode", "Type of notification to produce", "Notifications");

		_soundName = Param(nameof(SoundName), "Alert2.wav")
			.SetDisplay("Sound Name", "Sound file to reference in Sound mode", "Notifications");

		_levels = Param(nameof(Levels), string.Empty)
			.SetDisplay("Levels", "Semicolon separated price levels", "Levels");

		_clearOnStop = Param(nameof(ClearLevelsOnStop), false)
			.SetDisplay("Clear On Stop", "Forget parsed levels when the strategy stops", "Levels");
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

		// Validate that configuration values are consistent before subscribing to market data.
		if (!ValidateParameters())
		{
			Stop();
			return;
		}

		// Parse the textual price level list only once per run.
		_parsedLevels = ParseLevels(Levels);

		if (_parsedLevels.Length == 0)
			AddWarningLog("No valid price levels were provided. The strategy will not trigger alerts until levels are specified.");

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		// Reset cached candle state so the next run starts from scratch.
		_previousOpen = 0m;
		_hasPreviousOpen = false;

		if (ClearLevelsOnStop)
			_parsedLevels = Array.Empty<decimal>();

		base.OnStopped();
	}

	// Ensure configuration values do not contradict each other.
	private bool ValidateParameters()
	{
		if (SignalMode == BreakoutNotificationMode.Sound && string.IsNullOrWhiteSpace(SoundName))
		{
			AddErrorLog("Sound mode requires a non-empty sound file name.");
			return false;
		}

		return true;
	}

	// Convert the textual level description into a numeric array.
	private decimal[] ParseLevels(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return Array.Empty<decimal>();

		var parts = text.Split(LevelSeparators, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
			return Array.Empty<decimal>();

		var levels = new decimal[parts.Length];
		var count = 0;

		for (var i = 0; i < parts.Length; i++)
		{
			var part = parts[i].Trim();

			if (decimal.TryParse(part, NumberStyles.Number, CultureInfo.InvariantCulture, out var level))
			{
				levels[count++] = level;
			}
			else
			{
				AddWarningLog("Unable to parse price level '{0}'.", part);
			}
		}

		if (count == 0)
			return Array.Empty<decimal>();

		if (count != levels.Length)
		{
			var trimmed = new decimal[count];
			Array.Copy(levels, trimmed, count);
			levels = trimmed;
		}

		Array.Sort(levels);
		return levels;
	}

	// Process each completed candle and fire alerts when breakouts are detected.
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_parsedLevels.Length == 0)
			return;

		if (!_hasPreviousOpen)
		{
			_previousOpen = candle.OpenPrice;
			_hasPreviousOpen = true;
			return;
		}

		for (var i = 0; i < _parsedLevels.Length; i++)
		{
			var level = _parsedLevels[i];

			// Price touched the horizontal band within the candle range.
			var crossedInsideBar = candle.HighPrice >= level && candle.LowPrice <= level;
			// Price opened above a level after previously being below it.
			var crossedFromBelow = _previousOpen < level && candle.OpenPrice >= level;
			// Price opened below a level after previously being above it.
			var crossedFromAbove = _previousOpen > level && candle.OpenPrice <= level;

			if (crossedInsideBar || crossedFromBelow || crossedFromAbove)
				NotifyBreakout(level, candle);
		}

		_previousOpen = candle.OpenPrice;
	}

	// Generate a unified log entry describing the breakout event.
	private void NotifyBreakout(decimal level, ICandleMessage candle)
	{
		var message = $"{Prefix}breakout at {level.ToString(CultureInfo.InvariantCulture)} on {candle.OpenTime:O}";

		switch (SignalMode)
		{
			case BreakoutNotificationMode.Sound:
				AddInfoLog("{0}. Play sound '{1}'.", message, SoundName);
				break;
			case BreakoutNotificationMode.Alert:
				AddWarningLog("{0}. Alert notification triggered.", message);
				break;
			case BreakoutNotificationMode.Push:
				AddInfoLog("{0}. Push notification requested.", message);
				break;
			case BreakoutNotificationMode.Mail:
				AddInfoLog("{0}. Email notification requested.", message);
				break;
			default:
				AddWarningLog("{0}. Unknown notification mode.", message);
				break;
		}
	}
}
