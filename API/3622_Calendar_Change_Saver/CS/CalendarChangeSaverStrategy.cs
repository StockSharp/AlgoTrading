namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.Localization;
using StockSharp.Logging;
using StockSharp.Messages;

/// <summary>
/// CalendarChangeSaver strategy converted from MQL service example.
/// </summary>
public class CalendarChangeSaverStrategy : Strategy
{
	private readonly StrategyParam<string> _outputFileName;
	private readonly StrategyParam<int> _flushIntervalMilliseconds;
	private readonly StrategyParam<int> _bulkLimit;

	private readonly List<NewsRecord> _pendingNews = new();
	private readonly HashSet<string> _knownKeys = new(StringComparer.Ordinal);
	private readonly object _sync = new();

	private Timer? _flushTimer;
	private StreamWriter? _writer;
	private bool _wasOnline;

	/// <summary>
	/// Initializes a new instance of <see cref="CalendarChangeSaverStrategy"/>.
	/// </summary>
	public CalendarChangeSaverStrategy()
	{
		_outputFileName = Param(nameof(OutputFileName), "calendar_changes.log")
		.SetDisplay(LocalizedStrings.Str2968, "File path used to persist captured economic calendar updates.", LocalizedStrings.GeneralKey);
		_flushIntervalMilliseconds = Param(nameof(FlushIntervalMilliseconds), 1000)
		.SetDisplay("Flush interval (ms)", "Interval in milliseconds used to flush cached news batches to disk.", LocalizedStrings.GeneralKey);
		_bulkLimit = Param(nameof(BulkLimit), 100)
		.SetDisplay("Bulk limit", "Maximum amount of news messages processed in a single batch before skipping it as unreliable.", LocalizedStrings.GeneralKey);
	}

	/// <summary>
	/// Gets or sets the output file name that stores captured calendar updates.
	/// </summary>
	public string OutputFileName
	{
		get => _outputFileName.Value;
		set => _outputFileName.Value = value;
	}

	/// <summary>
	/// Gets or sets the flush interval in milliseconds.
	/// </summary>
	public int FlushIntervalMilliseconds
	{
		get => _flushIntervalMilliseconds.Value;
		set => _flushIntervalMilliseconds.Value = value;
	}

	/// <summary>
	/// Gets or sets the bulk limit that guards against unreliable large responses.
	/// </summary>
	public int BulkLimit
	{
		get => _bulkLimit.Value;
		set => _bulkLimit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, MarketDataTypes.News.ToDataType())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		lock (_sync)
		{
			_pendingNews.Clear();
			_knownKeys.Clear();
		}

		DisposeTimer();
		DisposeWriter();
		_wasOnline = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security is null)
		throw new InvalidOperationException("Security must be set before starting the strategy.");

		if (string.IsNullOrWhiteSpace(OutputFileName))
		throw new InvalidOperationException("Output file name must be specified.");

		EnsureWriter();

		Connector.SubscribeMarketData(Security, MarketDataTypes.News);

		var interval = Math.Max(100, FlushIntervalMilliseconds);
		_flushTimer = new Timer(OnTimer, null, interval, interval);
	}

	/// <inheritdoc />
	protected override void OnStopping()
	{
		DisposeTimer();
		base.OnStopping();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();
		DisposeWriter();
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		base.OnProcessMessage(message);

		switch (message.Type)
		{
			case MessageTypes.Connect:
				SetOnlineState(true);
			break;
			case MessageTypes.Disconnect:
				SetOnlineState(false);
			break;
			case MessageTypes.News:
				ProcessNewsMessage((NewsMessage)message);
			break;
		}
	}

	private void SetOnlineState(bool isOnline)
	{
		if (_wasOnline == isOnline)
		return;

		_wasOnline = isOnline;

		if (isOnline)
		LogInfo("Connector is online. Resuming calendar capture.");
		else
		LogInfo("Connector is offline. Waiting for connection...");
	}

	private void ProcessNewsMessage(NewsMessage news)
	{
		// Build a unique key from the news metadata to avoid duplicates.
		var key = string.Join("|", news.Source, news.ServerTime.ToUniversalTime().ToString("O"), news.Headline);

		lock (_sync)
		{
			if (!_knownKeys.Add(key))
			return;

			_pendingNews.Add(new NewsRecord(key, news.ServerTime, news.Source ?? string.Empty, news.Headline ?? string.Empty, news.Story ?? string.Empty));
		}

		LogInfo($"Captured news: {news.Headline}");
	}

	private void OnTimer(object? state)
	{
		if (ProcessState != ProcessStates.Started)
		return;

		NewsRecord[] batch;

		lock (_sync)
		{
			if (_pendingNews.Count == 0)
			return;

			batch = _pendingNews.ToArray();
			_pendingNews.Clear();
		}

		WriteBatch(batch);
	}

	private void WriteBatch(IReadOnlyCollection<NewsRecord> batch)
	{
		if (batch.Count == 0)
		return;

		if (batch.Count >= BulkLimit)
		{
			LogWarn($"Batch skipped because it contains {batch.Count} news records (limit {BulkLimit}).");
			return;
		}

		var writer = _writer;
		if (writer is null)
		return;

		var timestamp = CurrentTime.UtcDateTime.ToString("O");
		var serialized = string.Join(",", batch.Select(SerializeRecord));
		writer.WriteLine($"{timestamp}|{batch.Count}|[{serialized}]");
	}

	private static string SerializeRecord(NewsRecord record)
	{
		// Encode the record into a compact format suitable for later parsing.
		return string.Join(";", new[]
		{
			record.Time.ToUniversalTime().ToString("O"),
			record.Source.Replace(';', ' '),
			record.Headline.Replace(';', ' '),
			record.Story.Replace(';', ' ').Replace('\n', ' ')
		});
	}

	private void EnsureWriter()
	{
		if (_writer is not null)
		return;

		var path = OutputFileName;
		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		Directory.CreateDirectory(directory);

		var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
		stream.Seek(0, SeekOrigin.End);
		_writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false)
		{
			AutoFlush = true,
		};
	}

	private void DisposeTimer()
	{
		_flushTimer?.Dispose();
		_flushTimer = null;
	}

	private void DisposeWriter()
	{
		if (_writer is null)
		return;

		_writer.Dispose();
		_writer = null;
	}

	private sealed record class NewsRecord(string Key, DateTimeOffset Time, string Source, string Headline, string Story);
}
