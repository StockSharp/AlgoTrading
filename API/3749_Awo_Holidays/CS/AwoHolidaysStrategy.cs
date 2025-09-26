using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Displays upcoming weekends and market holidays based on a CSV schedule.
/// Converted from the MetaTrader "awo Holidays" indicator.
/// </summary>
public class AwoHolidaysStrategy : Strategy
{
	private readonly StrategyParam<int> _historyDepth;
	private readonly StrategyParam<bool> _clearOnStop;
	private readonly StrategyParam<string> _fileName;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _workdayColorName;
	private readonly StrategyParam<string> _weekendColorName;
	private readonly StrategyParam<string> _holidayColorName;

	private readonly List<HolidayEntry> _holidayEntries = new();
	private string _lastStatusText;

	/// <summary>
	/// Number of past days to include in the status output.
	/// </summary>
	public int HistoryDepth
	{
		get => _historyDepth.Value;
		set => _historyDepth.Value = value;
	}

	/// <summary>
	/// Removes the cached status when the strategy stops.
	/// </summary>
	public bool ClearOnStop
	{
		get => _clearOnStop.Value;
		set => _clearOnStop.Value = value;
	}

	/// <summary>
	/// CSV file name that contains holiday definitions.
	/// </summary>
	public string FileName
	{
		get => _fileName.Value;
		set => _fileName.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Candle type used to trigger status updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Label used for workday themed messages.
	/// </summary>
	public string WorkdayColorName
	{
		get => _workdayColorName.Value;
		set => _workdayColorName.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Label used for weekend themed messages.
	/// </summary>
	public string WeekendColorName
	{
		get => _weekendColorName.Value;
		set => _weekendColorName.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Label used for holiday themed messages.
	/// </summary>
	public string HolidayColorName
	{
		get => _holidayColorName.Value;
		set => _holidayColorName.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AwoHolidaysStrategy"/> class.
	/// </summary>
	public AwoHolidaysStrategy()
	{
		_historyDepth = Param(nameof(HistoryDepth), 3)
		.SetNotNegative()
		.SetDisplay("History Depth", "Number of previous days to display in the status panel.", "Visualization");

		_clearOnStop = Param(nameof(ClearOnStop), true)
		.SetDisplay("Clear On Stop", "Reset the status comment when the strategy stops.", "Visualization");

		_fileName = Param(nameof(FileName), "holidays.csv")
		.SetDisplay("Holiday File", "CSV file that stores the holiday calendar.", "Data");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle series that triggers holiday updates.", "Data");

		_workdayColorName = Param(nameof(WorkdayColorName), "LightBlue")
		.SetDisplay("Workday Label", "Textual label for workday entries.", "Visualization");

		_weekendColorName = Param(nameof(WeekendColorName), "Blue")
		.SetDisplay("Weekend Label", "Textual label for weekend entries.", "Visualization");

		_holidayColorName = Param(nameof(HolidayColorName), "DarkOrange")
		.SetDisplay("Holiday Label", "Textual label for holiday entries.", "Visualization");
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

		_holidayEntries.Clear();
		_lastStatusText = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		LoadHolidayFile();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		UpdateStatus(CurrentTime.DateTime);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		if (ClearOnStop)
		{
			_lastStatusText = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateStatus(candle.CloseTime.DateTime);
	}

	private void UpdateStatus(DateTime referenceDate)
	{
		var builder = new StringBuilder();
		builder.AppendLine("Holiday overview:");

		for (var offset = 1; offset >= -HistoryDepth; offset--)
		{
			var targetDate = referenceDate.AddDays(offset);
			var label = BuildLabel(offset);
			var dayType = GetDayType(targetDate);
			var holiday = GetHolidayDescription(targetDate);
			var color = GetColorLabel(dayType, holiday);

			builder.Append(targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			builder.Append(" | ");
			builder.Append(label);
			builder.Append(" | ");
			builder.Append(dayType);
			builder.Append(" | ");
			builder.Append(string.IsNullOrEmpty(holiday) ? "-" : holiday);
			builder.Append(" | ");
			builder.Append(color);
			builder.AppendLine();
		}

		var text = builder.ToString().TrimEnd();
		if (text.Length == 0 || text == _lastStatusText)
		return;

		AddInfo(text);
		_lastStatusText = text;
	}

	private string GetColorLabel(string dayType, string holiday)
	{
		if (!string.IsNullOrEmpty(holiday))
		return HolidayColorName;

		if (string.Equals(dayType, "workday", StringComparison.OrdinalIgnoreCase))
		return WorkdayColorName;

		return WeekendColorName;
	}

	private static string BuildLabel(int offset)
	{
		return offset switch
		{
			1 => "Tomorrow",
			0 => "Today",
			-1 => "Yesterday",
			_ when offset < -1 => $"{Math.Abs(offset)} days ago",
			_ => $"In {offset} days",
		};
	}

	private static string GetDayType(DateTime date)
	{
		return date.DayOfWeek switch
		{
			DayOfWeek.Saturday => "saturday",
			DayOfWeek.Sunday => "sunday",
			_ => "workday",
		};
	}

	private string GetHolidayDescription(DateTime date)
	{
		if (_holidayEntries.Count == 0)
		return string.Empty;

		var symbol = Security?.Id ?? string.Empty;
		var dateOnly = date.Date;

		foreach (var entry in _holidayEntries)
		{
			if (entry.Date != dateOnly)
			continue;

			if (entry.Symbols.Length == 0 || symbol.Length == 0)
			return entry.Description;

			if (IsSymbolMatch(symbol, entry.Symbols))
			return entry.Description;
		}

		return string.Empty;
	}

	private static bool IsSymbolMatch(string symbol, string pattern)
	{
		var upperSymbol = symbol.ToUpperInvariant();
		var segments = pattern.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

		for (var i = 0; i < segments.Length; i++)
		{
			var token = segments[i].Trim();
			if (token.Length == 0)
			continue;

			var upperToken = token.ToUpperInvariant();
			if (upperSymbol.Contains(upperToken, StringComparison.Ordinal))
			return true;
		}

		return false;
	}

	private void LoadHolidayFile()
	{
		var filePath = ResolveFilePath(FileName);
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			this.LogWarning($"Holiday file '{FileName}' was not found. No external holidays will be displayed.");
			return;
		}

		_holidayEntries.Clear();

		foreach (var line in File.ReadAllLines(filePath))
		{
			if (string.IsNullOrWhiteSpace(line))
			continue;

			var parts = line.Split(';');
			if (parts.Length < 4)
			continue;

			var dateText = parts[0].Trim();
			var country = parts[1].Trim();
			var symbols = parts[2].Trim();
			var holiday = parts[3].Trim();

			if (!TryParseDate(dateText, out var date))
			continue;

			var description = holiday.Length == 0 || country.Length == 0
			? holiday
			: $"{holiday} in {country}";

			_holidayEntries.Add(new HolidayEntry(date, symbols, description));
		}

		LogInfo($"Loaded {_holidayEntries.Count} holiday entries from '{filePath}'.");
	}

	private static bool TryParseDate(string text, out DateTime date)
	{
		return DateTime.TryParseExact(text, new[] { "yyyy.MM.dd", "yyyy-MM-dd", "dd.MM.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
	}

	private static string ResolveFilePath(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		return string.Empty;

		if (Path.IsPathRooted(fileName))
		return fileName;

		return Path.Combine(Environment.CurrentDirectory, fileName);
	}

	private sealed class HolidayEntry
	{
		public HolidayEntry(DateTime date, string symbols, string description)
		{
			Date = date.Date;
			Symbols = symbols ?? string.Empty;
			Description = description ?? string.Empty;
		}

		public DateTime Date { get; }

		public string Symbols { get; }

		public string Description { get; }
	}
}
