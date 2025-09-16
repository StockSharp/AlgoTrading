using System;
using System.Globalization;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that inspects semicolon separated text object definitions and logs their timestamps.
/// Mirrors the original MQL sample that printed the time of every OBJ_TEXT element on the chart.
/// </summary>
public class TimeObjectTextDisplayStrategy : Strategy
{
	private static readonly string[] _supportedFormats =
	[
		"yyyy-MM-dd HH:mm:ss",
		"yyyy-MM-ddTHH:mm:ss",
		"yyyy-MM-dd HH:mm:ss zzz",
		"yyyy-MM-ddTHH:mm:sszzz",
		"yyyy.MM.dd HH:mm:ss",
		"yyyy.MM.ddTHH:mm:ss",
		"yyyy.MM.dd HH:mm:ss zzz",
		"yyyy.MM.ddTHH:mm:sszzz"
	];

	private readonly StrategyParam<string> _textObjectDefinitions;

	/// <summary>
	/// Semicolon separated list that describes text objects in the format Name@Time.
	/// </summary>
	public string TextObjectDefinitions
	{
		get => _textObjectDefinitions.Value;
		set => _textObjectDefinitions.Value = value;
	}

	public TimeObjectTextDisplayStrategy()
	{
		_textObjectDefinitions = Param(nameof(TextObjectDefinitions), "My Text@2015.06.30 11:53:24")
			.SetDisplay("Text Objects", "Semicolon separated list of text objects in the format Name@Time", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Provide the same feedback as the MQL script before processing objects.
		LogInfo("Starting text object time inspection.");
		ProcessTextObjects();
	}

	/// <summary>
	/// Parses the configuration string and logs every valid text object definition.
	/// </summary>
	private void ProcessTextObjects()
	{
		var definitions = TextObjectDefinitions;

		if (string.IsNullOrWhiteSpace(definitions))
		{
			LogInfo("No text object definitions provided.");
			return;
		}

		var entries = definitions.Split(';', StringSplitOptions.RemoveEmptyEntries);

		var processedAny = false;

		foreach (var rawEntry in entries)
		{
			// Clean up spacing around the entry to match user expectations.
			var entry = rawEntry.Trim();

			if (entry.Length == 0)
				continue;

			// Split once into name and time components.
			var parts = entry.Split('@', 2, StringSplitOptions.TrimEntries);

			if (parts.Length != 2)
			{
				LogWarning($"Skipping invalid text object definition: {entry}");
				continue;
			}

			var name = parts[0];
			var timePart = parts[1];

			if (!TryParseTime(timePart, out var timestamp))
			{
				LogWarning($"Failed to parse time '{timePart}' for text object '{name}'.");
				continue;
			}

			processedAny = true;

			// Mirror the output style from the MQL sample with formatted timestamp.
			LogInfo($"The time of object {name} is {timestamp:yyyy-MM-dd HH:mm:ss}.");
		}

		if (!processedAny)
			LogInfo("Definitions parsed but no valid text objects were found.");
	}

	/// <summary>
	/// Converts the supplied textual timestamp into a <see cref="DateTimeOffset"/> value.
	/// Multiple date formats are supported to match typical MetaTrader representations.
	/// </summary>
	private static bool TryParseTime(string timePart, out DateTimeOffset timestamp)
	{
		if (DateTimeOffset.TryParseExact(timePart, _supportedFormats, CultureInfo.InvariantCulture,
			DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp))
		{
			return true;
		}

		if (DateTimeOffset.TryParse(timePart, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out timestamp))
		{
			return true;
		}

		return DateTimeOffset.TryParse(timePart, out timestamp);
	}
}
