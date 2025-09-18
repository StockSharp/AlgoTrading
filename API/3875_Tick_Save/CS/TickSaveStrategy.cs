using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Replicates the MetaTrader 4 "TickSave" expert by streaming bid prices into CSV logs.
/// </summary>
public class TickSaveStrategy : Strategy
{
	private const string ConnectionLostMarker = "--------------------------Connection lost";
	private const string ExpertStoppedMarker = "--------------------------Expert was stopped";

	private readonly StrategyParam<string> _symbolList;
	private readonly StrategyParam<bool> _writeWarnings;
	private readonly StrategyParam<string> _outputRoot;
	private readonly StrategyParam<string> _serverFolder;

	private readonly Dictionary<Security, SymbolContext> _contexts = new();
	private string? _fileSuffix;

	/// <summary>
	/// Comma separated collection of security identifiers to record.
	/// </summary>
	public string SymbolList
	{
		get => _symbolList.Value;
		set => _symbolList.Value = value;
	}

	/// <summary>
	/// When enabled the strategy appends diagnostic markers alongside recorded ticks.
	/// </summary>
	public bool WriteWarnings
	{
		get => _writeWarnings.Value;
		set => _writeWarnings.Value = value;
	}

	/// <summary>
	/// Root directory used to store tick CSV files.
	/// </summary>
	public string OutputRoot
	{
		get => _outputRoot.Value;
		set => _outputRoot.Value = value;
	}

	/// <summary>
	/// Optional folder name that groups recordings by server or environment.
	/// </summary>
	public string ServerFolder
	{
		get => _serverFolder.Value;
		set => _serverFolder.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with sensible defaults mirroring the original expert advisor.
	/// </summary>
	public TickSaveStrategy()
	{
		_symbolList = Param(nameof(SymbolList), "EURUSD,GBPUSD,AUDUSD,USDCAD,USDJPY,USDCHF")
			.SetDisplay("Symbol List", "Comma separated identifiers to record", "General");

		_writeWarnings = Param(nameof(WriteWarnings), false)
			.SetDisplay("Write Warnings", "Append diagnostic markers to the log", "General");

		var defaultRoot = Path.Combine(Environment.CurrentDirectory, "Ticks");
		_outputRoot = Param(nameof(OutputRoot), defaultRoot)
			.SetDisplay("Output Root", "Directory where tick CSV files are stored", "Storage");

		_serverFolder = Param(nameof(ServerFolder), string.Empty)
			.SetDisplay("Server Folder", "Optional subdirectory representing the trading server", "Storage");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var context in _contexts.Values)
			yield return (context.Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		CloseWriters();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		WriteExpertStoppedMarkers();
		CloseWriters();
		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SecurityProvider == null)
			throw new InvalidOperationException("Security provider is not available.");

		var identifiers = ParseSymbols(SymbolList).ToList();
		if (identifiers.Count == 0)
			throw new InvalidOperationException("Symbol list is empty.");

		_fileSuffix = $"{time.Year:0000}.{time.Month:00}";

		var folder = ResolveServerFolder();
		var targetDirectory = string.IsNullOrEmpty(folder)
			? OutputRoot
			: Path.Combine(OutputRoot, folder);

		Directory.CreateDirectory(targetDirectory);

		CloseWriters();
		_contexts.Clear();

		var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var identifier in identifiers)
		{
			var trimmed = identifier.Trim();
			if (trimmed.Length == 0)
				continue;

			var security = SecurityProvider.LookupById(trimmed);
			if (security == null)
				throw new InvalidOperationException($"Security '{trimmed}' not found.");

			if (!uniqueIds.Add(security.Id))
				continue;

			var context = CreateContext(security, targetDirectory);
			_contexts.Add(security, context);

			SubscribeLevel1(security)
				.Bind(message => OnLevel1(security, message))
				.Start();
		}

		if (_contexts.Count == 0)
			throw new InvalidOperationException("No valid securities resolved from the provided list.");
	}

	private SymbolContext CreateContext(Security security, string rootDirectory)
	{
		var fileName = $"{security.Code}_{_fileSuffix}.csv";
		var filePath = Path.Combine(rootDirectory, fileName);

		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

		var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
		stream.Seek(0, SeekOrigin.End);

		var writer = new StreamWriter(stream)
		{
			AutoFlush = true
		};

		if (WriteWarnings)
			writer.WriteLine(ExpertStoppedMarker);

		return new SymbolContext(security, writer);
	}

	private void OnLevel1(Security security, Level1ChangeMessage message)
	{
		if (!_contexts.TryGetValue(security, out var context))
			return;

		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid is null)
			return;

		if (context.LastBid.HasValue && Math.Abs(context.LastBid.Value - bid.Value) < 0.00000001m)
			return;

		context.LastBid = bid;

		var timestamp = message.ServerTime != default ? message.ServerTime : message.LocalTime;
		if (timestamp == default)
			timestamp = CurrentTime;

		WriteTick(context, timestamp, bid.Value);
	}

	private void WriteTick(SymbolContext context, DateTimeOffset time, decimal bid)
	{
		var line = string.Join(",", time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), bid.ToString(CultureInfo.InvariantCulture));
		context.Writer.WriteLine(line);

		if (WriteWarnings)
		{
			context.Writer.WriteLine(ExpertStoppedMarker);
		}
	}

	private void WriteExpertStoppedMarkers()
	{
		if (!WriteWarnings)
			return;

		foreach (var context in _contexts.Values)
		{
			context.Writer.WriteLine(ConnectionLostMarker);
			context.Writer.WriteLine(ExpertStoppedMarker);
		}
	}

	private void CloseWriters()
	{
		foreach (var context in _contexts.Values)
		{
			context.Writer.Flush();
			context.Writer.Dispose();
		}

		_contexts.Clear();
	}

	private IEnumerable<string> ParseSymbols(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			yield break;

		var separators = new[] { ',', ';' };
		var start = 0;

		for (var i = 0; i <= value.Length; i++)
		{
			if (i < value.Length && Array.IndexOf(separators, value[i]) < 0)
				continue;

			var length = i - start;
			if (length > 0)
				yield return value.Substring(start, length);

			start = i + 1;
		}
	}

	private string ResolveServerFolder()
	{
		if (!string.IsNullOrWhiteSpace(ServerFolder))
			return ServerFolder!;

		var portfolio = Portfolio;
		if (portfolio?.Board != null && !string.IsNullOrWhiteSpace(portfolio.Board.Code))
			return portfolio.Board.Code!;

		var security = Security;
		if (security?.Board != null && !string.IsNullOrWhiteSpace(security.Board.Code))
			return security.Board.Code!;

		return string.Empty;
	}

	private sealed class SymbolContext
	{
		public SymbolContext(Security security, StreamWriter writer)
		{
			Security = security;
			Writer = writer;
		}

		public Security Security { get; }
		public StreamWriter Writer { get; }
		public decimal? LastBid { get; set; }
	}
}
