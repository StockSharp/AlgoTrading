using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Saves bid/ask snapshots for multiple securities into CSV and binary files.
/// </summary>
public class SaveTicksStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _recordingInterval;
	private readonly StrategyParam<SymbolSelectionMode> _symbolSelection;
	private readonly StrategyParam<string> _symbolsList;
	private readonly StrategyParam<string> _symbolsFileName;
	private readonly StrategyParam<RecordingFormat> _recordingFormat;
	private readonly StrategyParam<TimeFormat> _timeFormat;
	private readonly StrategyParam<string> _outputDirectory;

	private readonly List<Security> _trackedSecurities = [];
	private readonly Dictionary<Security, TickSnapshot> _snapshots = [];
	private readonly Dictionary<Security, StreamWriter> _csvWriters = [];
	private readonly Dictionary<Security, BinaryWriter> _binaryWriters = [];

	private readonly object _sync = new();
	private Timer? _timer;

	public TimeSpan RecordingInterval
	{
		get => _recordingInterval.Value;
		set => _recordingInterval.Value = value;
	}

	public SymbolSelectionMode SymbolSelection
	{
		get => _symbolSelection.Value;
		set => _symbolSelection.Value = value;
	}

	public string SymbolsList
	{
		get => _symbolsList.Value;
		set => _symbolsList.Value = value;
	}

	public string SymbolsFileName
	{
		get => _symbolsFileName.Value;
		set => _symbolsFileName.Value = value;
	}

	public RecordingFormat RecordingFormat
	{
		get => _recordingFormat.Value;
		set => _recordingFormat.Value = value;
	}

	public TimeFormat TimeFormat
	{
		get => _timeFormat.Value;
		set => _timeFormat.Value = value;
	}

	public string OutputDirectory
	{
		get => _outputDirectory.Value;
		set => _outputDirectory.Value = value;
	}

	public SaveTicksStrategy()
	{
		_recordingInterval = Param(nameof(RecordingInterval), TimeSpan.FromMilliseconds(500))
			.SetDisplay("Recording Interval", "Time interval between tick snapshots", "General");

		_symbolSelection = Param(nameof(SymbolSelection), SymbolSelectionMode.MainSecurity)
			.SetDisplay("Symbol Selection", "Source of symbols to record", "Symbols");

		_symbolsList = Param(nameof(SymbolsList), string.Empty)
			.SetDisplay("Symbols List", "Comma separated list of symbols", "Symbols");

		_symbolsFileName = Param(nameof(SymbolsFileName), "InputSymbolList.txt")
			.SetDisplay("Symbols File", "File with symbol identifiers", "Symbols");

		_recordingFormat = Param(nameof(RecordingFormat), RecordingFormat.Csv)
			.SetDisplay("Recording Format", "File format for stored ticks", "Storage");

		_timeFormat = Param(nameof(TimeFormat), TimeFormat.Server)
			.SetDisplay("Time Format", "Timestamp type stored in files", "Storage");

		var defaultDirectory = Path.Combine(Environment.CurrentDirectory, "ticks");
		_outputDirectory = Param(nameof(OutputDirectory), defaultDirectory)
			.SetDisplay("Output Directory", "Folder where tick files are stored", "Storage");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		lock (_sync)
		{
			if (_trackedSecurities.Count == 0 && Security != null)
			{
				yield return (Security, DataType.Level1);
				yield break;
			}

			foreach (var security in _trackedSecurities)
				yield return (security, DataType.Level1);
		}
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		StopTimerInternal();
		CloseWriters();
		lock (_sync)
		{
			_trackedSecurities.Clear();
			_snapshots.Clear();
		}
	}

	protected override void OnStopped()
	{
		StopTimerInternal();
		base.OnStopped();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StopTimerInternal();
		CloseWriters();

		if (SecurityProvider == null)
			throw new InvalidOperationException("Security provider is not available.");

		var securities = ResolveSecurities();

		if (RecordingInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("Recording interval must be positive.");

		if (securities.Count == 0)
			throw new InvalidOperationException("No securities resolved for recording.");

		Directory.CreateDirectory(OutputDirectory);
		SaveSymbolList(securities);

		lock (_sync)
		{
			_trackedSecurities.Clear();

			foreach (var security in securities)
			{
				_trackedSecurities.Add(security);
				_snapshots[security] = new TickSnapshot();

				SubscribeLevel1(security)
					.Bind(message => OnLevel1(security, message))
					.Start();

				OpenWriters(security);
			}
		}

		StartTimer();
	}

	private void OnLevel1(Security security, Level1ChangeMessage message)
	{
		lock (_sync)
		{
			if (!_snapshots.TryGetValue(security, out var snapshot))
				return;

			snapshot.ServerTime = message.ServerTime != default ? message.ServerTime : snapshot.ServerTime;
			snapshot.LocalTime = message.LocalTime != default ? message.LocalTime : snapshot.LocalTime;

			var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
			if (bid.HasValue)
				snapshot.Bid = bid.Value;

			var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);
			if (ask.HasValue)
				snapshot.Ask = ask.Value;

			var last = message.TryGetDecimal(Level1Fields.LastTradePrice);
			if (last.HasValue)
				snapshot.Last = last.Value;
		}
	}

	private void StartTimer()
	{
		lock (_sync)
		{
			_timer?.Dispose();
			_timer = new Timer(OnTimer, null, RecordingInterval, RecordingInterval);
		}
	}

	private void StopTimerInternal()
	{
		Timer? timer;
		lock (_sync)
		{
			timer = _timer;
			_timer = null;
		}
		timer?.Dispose();
	}

	private void OnTimer(object? state)
	{
		lock (_sync)
		{
			foreach (var security in _trackedSecurities)
			{
				if (!_snapshots.TryGetValue(security, out var snapshot))
					continue;

				if (!snapshot.IsReady)
					continue;

				var timestamp = SelectTimestamp(snapshot);

				try
				{
					WriteCsv(security, timestamp, snapshot);
					WriteBinary(security, timestamp, snapshot);
				}
				catch (Exception error)
				{
					LogError($"Failed to write tick for {security.Id}: {error.Message}");
				}
			}
		}
	}

	private DateTimeOffset SelectTimestamp(TickSnapshot snapshot)
	{
		if (TimeFormat == TimeFormat.Local)
		{
			if (snapshot.LocalTime.HasValue)
				return snapshot.LocalTime.Value;

			if (snapshot.ServerTime.HasValue)
				return snapshot.ServerTime.Value.ToLocalTime();
		}
		else if (TimeFormat == TimeFormat.Server && snapshot.ServerTime.HasValue)
		{
			return snapshot.ServerTime.Value;
		}

		return CurrentTime;
	}

	private void WriteCsv(Security security, DateTimeOffset timestamp, TickSnapshot snapshot)
	{
		if (!_csvWriters.TryGetValue(security, out var writer))
			return;

		var timestampText = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		var bidText = FormatDecimal(snapshot.Bid);
		var askText = FormatDecimal(snapshot.Ask);
		var lastText = FormatDecimal(snapshot.Last);

		writer.Write(timestampText);
		writer.Write(',');
		writer.Write(bidText);
		writer.Write(',');
		writer.Write(askText);
		writer.Write(',');
		writer.WriteLine(lastText);
	}

	private void WriteBinary(Security security, DateTimeOffset timestamp, TickSnapshot snapshot)
	{
		if (!_binaryWriters.TryGetValue(security, out var writer))
			return;

		writer.Write(timestamp.ToUnixTimeMilliseconds());
		writer.Write(snapshot.Bid.HasValue);
		writer.Write(snapshot.Bid ?? 0m);
		writer.Write(snapshot.Ask.HasValue);
		writer.Write(snapshot.Ask ?? 0m);
		writer.Write(snapshot.Last.HasValue);
		writer.Write(snapshot.Last ?? 0m);
		writer.Flush();
	}

	private void OpenWriters(Security security)
	{
		var baseName = $"{MakeSafeFileName(security.Id)}_{MakeSafeFileName(GetStrategyName())}";

		if (RecordingFormat == RecordingFormat.Csv || RecordingFormat == RecordingFormat.All)
		{
			var csvPath = Path.Combine(OutputDirectory, baseName + ".csv");
			var csvStream = new FileStream(csvPath, FileMode.Append, FileAccess.Write, FileShare.Read);
			var csvWriter = new StreamWriter(csvStream)
			{
				AutoFlush = true
			};
			_csvWriters[security] = csvWriter;
		}

		if (RecordingFormat == RecordingFormat.Binary || RecordingFormat == RecordingFormat.All)
		{
			var binPath = Path.Combine(OutputDirectory, baseName + ".bin");
			var binStream = new FileStream(binPath, FileMode.Append, FileAccess.Write, FileShare.Read);
			_binaryWriters[security] = new BinaryWriter(binStream);
		}
	}

	private void CloseWriters()
	{
		foreach (var writer in _csvWriters.Values)
			writer.Dispose();
		_csvWriters.Clear();

		foreach (var writer in _binaryWriters.Values)
			writer.Dispose();
		_binaryWriters.Clear();
	}

	private string GetStrategyName()
	{
		return string.IsNullOrWhiteSpace(Name) ? GetType().Name : Name;
	}

	private static string MakeSafeFileName(string value)
	{
		var chars = Path.GetInvalidFileNameChars();
		var builder = new System.Text.StringBuilder(value.Length);

		foreach (var ch in value)
		{
			var invalid = false;
			foreach (var bad in chars)
			{
				if (ch == bad)
				{
					invalid = true;
					break;
				}
			}

			builder.Append(invalid ? '_' : ch);
		}

		return builder.ToString();
	}

	private static string FormatDecimal(decimal? value)
	{
		return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
	}

	private List<Security> ResolveSecurities()
	{
		var result = new List<Security>();
		var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (Security != null && ids.Add(Security.Id))
			result.Add(Security);

		switch (SymbolSelection)
		{
			case SymbolSelectionMode.MainSecurity:
				break;

			case SymbolSelectionMode.ManualList:
				AddSymbols(result, ids, ParseSymbols(SymbolsList));
				break;

			case SymbolSelectionMode.FromFile:
				AddSymbols(result, ids, LoadSymbolsFromFile());
				break;

			default:
				throw new InvalidOperationException($"Unsupported symbol selection '{SymbolSelection}'.");
		}

		return result;
	}

	private void AddSymbols(List<Security> result, HashSet<string> ids, IEnumerable<string> symbols)
	{
		foreach (var symbol in symbols)
		{
			var trimmed = symbol?.Trim();
			if (string.IsNullOrEmpty(trimmed))
				continue;

			var security = SecurityProvider!.LookupById(trimmed);
			if (security == null)
				throw new InvalidOperationException($"Security '{trimmed}' not found.");

			if (ids.Add(security.Id))
				result.Add(security);
		}
	}

	private IEnumerable<string> ParseSymbols(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			yield break;

		var separators = new[] { ',', ';', '\n', '\r', '\t', ' ' };
		var start = 0;

		for (var i = 0; i <= value.Length; i++)
		{
			if (i == value.Length || Array.IndexOf(separators, value[i]) >= 0)
			{
				var length = i - start;
				if (length > 0)
					yield return value.Substring(start, length);
				start = i + 1;
			}
		}
	}

	private IEnumerable<string> LoadSymbolsFromFile()
	{
		var filePath = ResolveSymbolsFilePath(SymbolsFileName);
		if (!File.Exists(filePath))
			throw new InvalidOperationException($"Symbols file '{filePath}' not found.");

		using var reader = new StreamReader(filePath);
		var header = reader.ReadLine();
		if (header == null || !int.TryParse(header, out var count) || count < 0)
			throw new InvalidOperationException($"Invalid header in symbols file '{filePath}'.");

		for (var i = 0; i < count; i++)
		{
			var line = reader.ReadLine();
			if (line == null)
				break;

			yield return line;
		}
	}

	private string ResolveSymbolsFilePath(string fileName)
	{
		if (Path.IsPathRooted(fileName))
			return fileName;

		var candidate = Path.Combine(OutputDirectory, fileName);
		if (File.Exists(candidate))
			return candidate;

		return Path.Combine(Environment.CurrentDirectory, fileName);
	}

	private void SaveSymbolList(IReadOnlyList<Security> securities)
	{
		var fileName = Path.Combine(OutputDirectory, $"AllSymbols_{MakeSafeFileName(GetStrategyName())}.txt");
		using var writer = new StreamWriter(fileName, false);
		writer.WriteLine(securities.Count);
		foreach (var security in securities)
			writer.WriteLine(security.Id);
	}

	private sealed class TickSnapshot
	{
		public decimal? Bid { get; set; }
		public decimal? Ask { get; set; }
		public decimal? Last { get; set; }
		public DateTimeOffset? ServerTime { get; set; }
		public DateTimeOffset? LocalTime { get; set; }

		public bool IsReady => Bid.HasValue || Ask.HasValue || Last.HasValue;
	}

	public enum SymbolSelectionMode
	{
		MainSecurity,
		ManualList,
		FromFile
	}

	public enum RecordingFormat
	{
		Csv,
		Binary,
		All
	}

	public enum TimeFormat
	{
		Server,
		Local
	}
}
