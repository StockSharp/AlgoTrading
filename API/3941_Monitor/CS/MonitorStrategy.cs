namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that writes terminal information into a memory-mapped file and reads other terminals back.
/// </summary>
public class MonitorStrategy : Strategy
{
	private const int HeaderSize = sizeof(int) * 2;
	private const int VectorSize = sizeof(int) * 4;
	private const int MaxNameBytes = 30;
	private const int RecordSize = VectorSize + sizeof(int) + MaxNameBytes;
	private const decimal PriceScale = 0.000001m;
	private const int PlatformCode = 6;

	private readonly StrategyParam<string> _fileDirectory;
	private readonly StrategyParam<string> _filePrefix;
	private readonly StrategyParam<TimeSpan> _refreshInterval;
	private readonly StrategyParam<int> _maxLatency;

	private readonly object _syncRoot = new();

	private Timer _timer;
	private MemoryMappedFile _memoryFile;
	private MemoryMappedViewAccessor _accessor;
	private FileStream _stream;
	private int _recordIndex;
	private string _filePath;
	private string _terminalName;
	private decimal? _bestBid;

	/// <summary>
	/// Directory where the memory-mapped file is stored.
	/// </summary>
	public string FileDirectory
	{
		get => _fileDirectory.Value;
		set => _fileDirectory.Value = value;
	}

	/// <summary>
	/// Prefix used for the file name.
	/// </summary>
	public string FilePrefix
	{
		get => _filePrefix.Value;
		set => _filePrefix.Value = value;
	}

	/// <summary>
	/// Interval between synchronization attempts.
	/// </summary>
	public TimeSpan RefreshInterval
	{
		get => _refreshInterval.Value;
		set => _refreshInterval.Value = value;
	}

	/// <summary>
	/// Maximum allowed latency of the records in milliseconds.
	/// </summary>
	public int MaxLatency
	{
		get => _maxLatency.Value;
		set => _maxLatency.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MonitorStrategy"/>.
	/// </summary>
	public MonitorStrategy()
	{
		_fileDirectory = Param(nameof(FileDirectory), "Local")
			.SetDisplay("Directory", "Folder for memory-mapped file", "General");

		_filePrefix = Param(nameof(FilePrefix), "Monitor_")
			.SetDisplay("File Prefix", "Prefix for memory-mapped file", "General");

		_refreshInterval = Param(nameof(RefreshInterval), TimeSpan.FromMilliseconds(500))
			.SetDisplay("Refresh Interval", "Delay between synchronization runs", "General");

		_maxLatency = Param(nameof(MaxLatency), 5000)
			.SetGreaterThanZero()
			.SetDisplay("Max Latency", "Maximum age of active records in milliseconds", "General");
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

		_bestBid = null;
		_filePath = Path.Combine(FileDirectory, GetFileNameSuffix());
		_terminalName = Environment.MachineName;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (string.IsNullOrEmpty(_filePath))
			_filePath = Path.Combine(FileDirectory, GetFileNameSuffix());

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		OpenMemoryFile();

		_timer = new Timer(_ => ProcessMonitor(), null, TimeSpan.Zero, RefreshInterval);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		_timer = null;

		CloseMemoryFile();

		base.OnStopped();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (!level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
			return;

		_bestBid = (decimal)bidObj;
	}

	private void ProcessMonitor()
	{
		try
		{
			UpdateLocalRecord();

			var (total, active) = ReadRecords();

			var builder = new StringBuilder();
			builder.AppendLine($"Terminals={total} active={active.Count}");

			foreach (var entry in active)
			{
				var elapsed = unchecked(Environment.TickCount - entry.Timestamp);
				if (elapsed < 0)
					elapsed = 0;

				builder.AppendLine($"{elapsed} ({entry.Timestamp}) | {entry.Account} | {entry.Price:F6} | {entry.Terminal} | MT {entry.Platform}");
			}

			if (builder.Length > 0)
				this.LogInfo(builder.ToString());
		}
		catch (Exception ex)
		{
			this.LogError(ex);
		}
	}

	private void UpdateLocalRecord()
	{
		if (_accessor is null)
			return;

		var bid = _bestBid ?? 0m;
		var scaledBid = decimal.ToInt32(decimal.Round(bid / PriceScale, MidpointRounding.AwayFromZero));

		var account = GetAccountId();
		var timestamp = unchecked((int)Environment.TickCount);

		var offset = HeaderSize + (long)_recordIndex * RecordSize;

		lock (_syncRoot)
		{
			_accessor.Write(offset, timestamp);
			_accessor.Write(offset + sizeof(int), account);
			_accessor.Write(offset + sizeof(int) * 2, scaledBid);
			_accessor.Write(offset + sizeof(int) * 3, PlatformCode);
			WriteTerminalName(offset + VectorSize, _terminalName);
			_accessor.Flush();
		}
	}

	private (int total, List<MonitorEntry> active) ReadRecords()
	{
		var result = new List<MonitorEntry>();

		if (_accessor is null)
			return (0, result);

		lock (_syncRoot)
		{
			var total = _accessor.ReadInt32(sizeof(int)) + 1;
			var now = Environment.TickCount;

			for (var i = 0; i < total; i++)
			{
				var offset = HeaderSize + (long)i * RecordSize;
				var timestamp = _accessor.ReadInt32(offset);
				var age = unchecked(now - timestamp);
				if (age < 0)
					age = 0;

				if (age > MaxLatency)
					continue;

				var account = _accessor.ReadInt32(offset + sizeof(int));
				var priceScaled = _accessor.ReadInt32(offset + sizeof(int) * 2);
				var platform = _accessor.ReadInt32(offset + sizeof(int) * 3);
				var terminal = ReadTerminalName(offset + VectorSize);

				var entry = new MonitorEntry
				{
					Timestamp = timestamp,
					Account = account,
					Price = priceScaled * PriceScale,
					Platform = platform,
					Terminal = terminal,
				};

				result.Add(entry);
			}

			return (total, result);
		}
	}

	private void OpenMemoryFile()
	{
		try
		{
			var directory = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			var exists = File.Exists(_filePath);
			_stream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

			using var reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
			using var writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);

			if (!exists)
			{
				_stream.SetLength(HeaderSize + RecordSize);
				_stream.Seek(0, SeekOrigin.Begin);
				writer.Write(1);
				writer.Write(0);
				_recordIndex = 0;
			}
			else
			{
				if (_stream.Length < HeaderSize)
					_stream.SetLength(HeaderSize);

				_stream.Seek(0, SeekOrigin.Begin);
				var uses = reader.ReadInt32();
				var lastIndex = reader.ReadInt32();

				uses++;
				lastIndex++;
				_recordIndex = lastIndex;

				var requiredLength = HeaderSize + (long)(_recordIndex + 1) * RecordSize;
				if (_stream.Length < requiredLength)
					_stream.SetLength(requiredLength);

				_stream.Seek(0, SeekOrigin.Begin);
				writer.Write(uses);
				writer.Write(lastIndex);
			}

			writer.Flush();
			_stream.Flush(true);

			_memoryFile = MemoryMappedFile.CreateFromFile(_stream, null, _stream.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
			_accessor = _memoryFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
		}
		catch (Exception ex)
		{
			this.LogError(ex);
			_accessor = null;
		}
	}

	private void CloseMemoryFile()
	{
		if (_accessor is null)
			return;

		try
		{
			lock (_syncRoot)
			{
				var uses = _accessor.ReadInt32(0);
				if (uses > 0)
				{
					uses--;
					_accessor.Write(0, uses);
				}
			}
		}
		catch (Exception ex)
		{
			this.LogError(ex);
		}
		finally
		{
			_accessor.Dispose();
			_accessor = null;
			_memoryFile?.Dispose();
			_memoryFile = null;
			_stream?.Dispose();
			_stream = null;
		}
	}

	private int GetAccountId()
	{
		if (Portfolio?.Name is string name)
		{
			if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
				return numeric;

			return name.GetHashCode(StringComparison.Ordinal);
		}

		return 0;
	}

	private void WriteTerminalName(long offset, string value)
	{
		var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
		if (bytes.Length > MaxNameBytes)
			Array.Resize(ref bytes, MaxNameBytes);

		_accessor.Write(offset, Math.Min(bytes.Length, MaxNameBytes));
		_accessor.WriteArray(offset + sizeof(int), bytes, 0, bytes.Length);

		if (bytes.Length < MaxNameBytes)
		{
			var padding = new byte[MaxNameBytes - bytes.Length];
			_accessor.WriteArray(offset + sizeof(int) + bytes.Length, padding, 0, padding.Length);
		}
	}

	private string ReadTerminalName(long offset)
	{
		var length = _accessor.ReadInt32(offset);
		var count = Math.Min(length, MaxNameBytes);
		var buffer = new byte[count];
		_accessor.ReadArray(offset + sizeof(int), buffer, 0, count);
		return Encoding.UTF8.GetString(buffer, 0, count);
	}

	private string GetFileNameSuffix()
	{
		var code = Security?.Id.SecurityCode ?? "UNKNOWN";
		if (code.Length > 6)
			code = code[..6];
		return FilePrefix + code;
	}

	private struct MonitorEntry
	{
		public int Timestamp;
		public int Account;
		public decimal Price;
		public int Platform;
		public string Terminal;
	}
}
