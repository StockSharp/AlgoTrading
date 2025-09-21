using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that records best ask and bid ticks into a CSV file for microstructure analysis.
/// </summary>
public class AskBidTicksStrategy : Strategy
{
	private readonly StrategyParam<string> _fileName;
	private readonly StrategyParam<TimestampMode> _timestampMode;
	private readonly StrategyParam<CsvDelimiter> _delimiter;

	private StreamWriter _writer;
	private string _filePath;
	private Stopwatch _stopwatch;
	private char _delimiterChar;

	/// <summary>
	/// File name that will be used for CSV output.
	/// </summary>
	public string FileName { get => _fileName.Value; set => _fileName.Value = value; }

	/// <summary>
	/// Timestamp formatting applied to captured ticks.
	/// </summary>
	public TimestampMode TimeStampMode { get => _timestampMode.Value; set => _timestampMode.Value = value; }

	/// <summary>
	/// CSV delimiter separating exported columns.
	/// </summary>
	public CsvDelimiter Delimiter { get => _delimiter.Value; set => _delimiter.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AskBidTicksStrategy"/>.
	/// </summary>
	public AskBidTicksStrategy()
	{
		_fileName = Param(nameof(FileName), "Use default")
			.SetDisplay("File Name", "Output CSV file name", "General");

		_timestampMode = Param(nameof(TimeStampMode), TimestampMode.Millisecond)
			.SetDisplay("Timestamp Mode", "Formatting applied to timestamps", "General");

		_delimiter = Param(nameof(Delimiter), CsvDelimiter.Tab)
			.SetDisplay("Delimiter", "Character separating CSV columns", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_writer?.Dispose();
		_writer = null;
		_filePath = null;
		_stopwatch = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_delimiterChar = Delimiter switch
		{
			CsvDelimiter.Tab => '\t',
			CsvDelimiter.Comma => ',',
			CsvDelimiter.Semicolon => ';',
			_ => ','
		};

		_filePath = ResolveFileName();

		try
		{
			var directory = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			_writer = new StreamWriter(File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read));
			_writer.WriteLine(string.Join(_delimiterChar, "Time", "Symbol", "Ask", "Bid"));
		}
		catch (Exception ex)
		{
			LogError(ex);
			Stop();
			return;
		}

		_stopwatch = Stopwatch.StartNew();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private string ResolveFileName()
	{
		if (!string.Equals(FileName, "Use default", StringComparison.OrdinalIgnoreCase) &&
			!string.IsNullOrWhiteSpace(FileName))
		{
			var fileName = Path.HasExtension(FileName) ? FileName : Path.ChangeExtension(FileName, ".csv");
			return Path.GetFullPath(fileName);
		}

		var timestamp = DateTimeOffset.Now.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
		var symbol = Security?.Id?.SecurityCode ?? "Unknown";
		var defaultName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}.csv", timestamp, symbol);
		return Path.Combine(Environment.CurrentDirectory, defaultName);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (_writer is null)
			return;

		if (!level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) ||
			!level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
			return;

		var askPrice = (decimal)askObj;
		var bidPrice = (decimal)bidObj;

		var decimals = Security?.Decimals ?? 5;
		var format = decimals > 0 ? "0." + new string('0', decimals) : "0";

		var stamp = FormatTimestamp();
		var symbol = Security?.Id?.SecurityCode ?? "Unknown";
		var askText = askPrice.ToString(format, CultureInfo.InvariantCulture);
		var bidText = bidPrice.ToString(format, CultureInfo.InvariantCulture);

		var line = string.Join(_delimiterChar, stamp, symbol, askText, bidText);
		_writer.WriteLine(line);
		_writer.Flush();

		LogInfo("{0}	{1}	ask:{2}	bid:{3}", stamp, symbol, askText, bidText);
	}

	private string FormatTimestamp()
	{
		var now = DateTimeOffset.Now;

		return TimeStampMode switch
		{
			TimestampMode.Standard => now.ToString("yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture),
			TimestampMode.Millisecond => now.ToString("yyyy.MM.dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
			TimestampMode.Analysis => (_stopwatch?.ElapsedMilliseconds ?? 0L).ToString(CultureInfo.InvariantCulture),
			_ => now.ToString("yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture)
		};
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_writer?.Flush();
		_writer?.Dispose();
		_writer = null;
		_stopwatch?.Stop();

		base.OnStopped();
	}
}

/// <summary>
/// Delimiters available for CSV export.
/// </summary>
public enum CsvDelimiter
{
	/// <summary>
	/// Tab character '	'.
	/// </summary>
	Tab,

	/// <summary>
	/// Comma ','.
	/// </summary>
	Comma,

	/// <summary>
	/// Semicolon ';'.
	/// </summary>
	Semicolon
}

/// <summary>
/// Modes available for timestamp formatting.
/// </summary>
public enum TimestampMode
{
	/// <summary>
	/// Format yyyy.MM.dd HH:mm:ss.
	/// </summary>
	Standard,

	/// <summary>
	/// Format yyyy.MM.dd HH:mm:ss.fff with milliseconds.
	/// </summary>
	Millisecond,

	/// <summary>
	/// Relative milliseconds counted from strategy start.
	/// </summary>
	Analysis
}
