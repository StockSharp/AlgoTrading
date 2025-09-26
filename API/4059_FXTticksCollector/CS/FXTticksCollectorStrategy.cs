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
/// Collects tick snapshots and candle backfill data into an FXT-like binary file.
/// Converted from the MetaTrader 4 expert advisor that generates tester data files.
/// </summary>
public class FXTticksCollectorStrategy : Strategy
{
	private const int HeaderMagic = 0x46585443; // 'CXTF'
	private const int HeaderVersion = 1;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _initialBarCount;
	private readonly StrategyParam<string> _fileDirectory;
	private readonly StrategyParam<bool> _appendToExisting;

	private readonly object _sync = new();

	private FileStream? _stream;
	private BinaryWriter _writer;
	private BinaryReader _reader;
	private string _filePath;
	private DateTimeOffset? _lastRecordedBar;
	private int _recordedBars;
	private long _recordedTicks;
	private long _barsOffset = -1;
	private long _ticksOffset = -1;
	private long _lastBarTimeOffset = -1;
	private int _prefillRemaining;
	private TimeSpan _timeFrame = TimeSpan.Zero;
	private ICandleMessage _currentCandle;

	/// <summary>
	/// Initializes a new instance of the <see cref="FXTticksCollectorStrategy"/> class.
	/// </summary>
	public FXTticksCollectorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to align bars", "General");

		_initialBarCount = Param(nameof(InitialBarCount), 100)
			.SetNotNegative()
			.SetDisplay("Initial Bars", "Number of historical candles written when creating a new file", "Storage");

		_fileDirectory = Param(nameof(FileDirectory), ".")
			.SetDisplay("Directory", "Folder where the FXT binary file is stored", "Storage");

		_appendToExisting = Param(nameof(AppendToExisting), true)
			.SetDisplay("Append", "Append to an existing file if the header matches", "Storage");
	}

	/// <summary>
	/// Candle type whose timeframe defines the FXT bar alignment.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles written when creating a new FXT file.
	/// </summary>
	public int InitialBarCount
	{
		get => _initialBarCount.Value;
		set => _initialBarCount.Value = value;
	}

	/// <summary>
	/// Directory where the FXT binary file is stored.
	/// </summary>
	public string FileDirectory
	{
		get => _fileDirectory.Value;
		set => _fileDirectory.Value = value;
	}

	/// <summary>
	/// Append to an existing FXT file when the header matches the current settings.
	/// </summary>
	public bool AppendToExisting
	{
		get => _appendToExisting.Value;
		set => _appendToExisting.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		CloseStorage();
		_recordedBars = 0;
		_recordedTicks = 0;
		_prefillRemaining = 0;
		_lastRecordedBar = null;
		_currentCandle = null;
		_timeFrame = TimeSpan.Zero;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeStorage();

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription.Bind(ProcessCandle).Start();

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CloseStorage();
		base.OnStopped();
	}

	private void InitializeStorage()
	{
		CloseStorage();

		if (Security == null)
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		var directory = FileDirectory;
		if (string.IsNullOrWhiteSpace(directory))
			directory = ".";

		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		_timeFrame = CandleType.TimeFrame ?? (CandleType.Arg as TimeSpan? ?? TimeSpan.FromMinutes(1));

		var symbolPart = SanitizeFileName(Security.Id);
		var timeFramePart = GetTimeFrameSuffix(CandleType);
		_filePath = Path.Combine(directory, $"{symbolPart}_{timeFramePart}_0.fxt");

		var fileExists = File.Exists(_filePath);
		_stream = new FileStream(_filePath, fileExists ? FileMode.Open : FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
		_reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
		_writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);

		if (fileExists && AppendToExisting && TryReadHeader())
		{
			_prefillRemaining = 0;
			_stream.Seek(0, SeekOrigin.End);
			LogInfo($"Appending to existing FXT file '{_filePath}'.");
		}
		else
		{
			if (fileExists)
				_stream.SetLength(0);

			WriteHeader();
			_prefillRemaining = Math.Max(InitialBarCount, 0);
			LogInfo($"Created new FXT file '{_filePath}'.");
		}
	}

	private bool TryReadHeader()
	{
		if (_reader == null || _stream == null || Security == null)
			return false;

		try
		{
			_stream.Seek(0, SeekOrigin.Begin);

			if (_stream.Length < sizeof(int) * 2)
				return false;

			var magic = _reader.ReadInt32();
			if (magic != HeaderMagic)
				return false;

			var version = _reader.ReadInt32();
			if (version != HeaderVersion)
				return false;

			var symbol = _reader.ReadString();
			var storedSeconds = _reader.ReadInt64();
			var storedWarmup = _reader.ReadInt32();

			_barsOffset = _stream.Position;
			_recordedBars = _reader.ReadInt32();

			_ticksOffset = _stream.Position;
			_recordedTicks = _reader.ReadInt64();

			_lastBarTimeOffset = _stream.Position;
			var lastBarSeconds = _reader.ReadInt64();

			_lastRecordedBar = lastBarSeconds > 0 ? DateTimeOffset.FromUnixTimeSeconds(lastBarSeconds) : null;

			var storedAppend = _reader.ReadBoolean();

			var expectedSymbol = Security.Id;
			var expectedSeconds = GetTimeFrameSeconds();
			var expectedWarmup = Math.Max(InitialBarCount, 0);

			if (!string.Equals(symbol, expectedSymbol, StringComparison.Ordinal))
				return false;

			if (storedSeconds != expectedSeconds)
				return false;

			if (storedWarmup != expectedWarmup)
				return false;

			if (!AppendToExisting && storedAppend)
				return false;

			return true;
		}
		catch (EndOfStreamException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	private void WriteHeader()
	{
		if (_writer == null || _stream == null || Security == null)
			return;

		_stream.Seek(0, SeekOrigin.Begin);

		_recordedBars = 0;
		_recordedTicks = 0;
		_lastRecordedBar = null;

		_writer.Write(HeaderMagic);
		_writer.Write(HeaderVersion);
		_writer.Write(Security.Id);
		_writer.Write(GetTimeFrameSeconds());
		_writer.Write(Math.Max(InitialBarCount, 0));

		_barsOffset = _stream.Position;
		_writer.Write(_recordedBars);

		_ticksOffset = _stream.Position;
		_writer.Write(_recordedTicks);

		_lastBarTimeOffset = _stream.Position;
		_writer.Write(0L);

		_writer.Write(AppendToExisting);

		_writer.Flush();
		_stream.SetLength(_stream.Position);
		_stream.Seek(0, SeekOrigin.End);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		lock (_sync)
		{
			_currentCandle = candle;

			if (candle.State != CandleStates.Finished)
				return;

			var needPrefill = _prefillRemaining > 0;
			var hasGap = _lastRecordedBar.HasValue && candle.OpenTime > _lastRecordedBar.Value;

			if (!needPrefill && !hasGap)
				return;

			var flag = needPrefill ? 0 : 1;
			WriteCandleRecord(candle, flag);

			if (needPrefill && _prefillRemaining > 0)
				_prefillRemaining--;
		}
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		lock (_sync)
		{
			if (_writer == null)
				return;

			if (trade.TradePrice is not decimal price)
				return;

			var eventTime = trade.ServerTime != default ? trade.ServerTime : (trade.LocalTime != default ? trade.LocalTime : CurrentTime);
			var candle = _currentCandle;
			var barOpenTime = candle?.OpenTime ?? AlignTime(eventTime);

			if (!_lastRecordedBar.HasValue || barOpenTime > _lastRecordedBar.Value)
			{
				_lastRecordedBar = barOpenTime;
				_recordedBars++;
			}

			var open = candle?.OpenPrice ?? price;
			var high = candle?.HighPrice ?? price;
			var low = candle?.LowPrice ?? price;
			var close = candle?.ClosePrice ?? price;
			var volume = candle?.TotalVolume ?? candle?.Volume ?? (trade.TradeVolume ?? trade.Volume ?? 0m);

			WriteTickRecord(barOpenTime, open, low, high, close, volume, eventTime);
			_recordedTicks++;
		}
	}

	private void WriteCandleRecord(ICandleMessage candle, int flag)
	{
		if (_writer == null)
			return;

		var closeTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime + (_timeFrame > TimeSpan.Zero ? _timeFrame : TimeSpan.Zero);

		_writer.Write(ToUnixSeconds(candle.OpenTime));
		_writer.Write(ToDouble(candle.OpenPrice));
		_writer.Write(ToDouble(candle.LowPrice));
		_writer.Write(ToDouble(candle.HighPrice));
		_writer.Write(ToDouble(candle.ClosePrice));
		_writer.Write(ToDouble(candle.TotalVolume ?? candle.Volume ?? 0m));
		_writer.Write(ToUnixSeconds(closeTime));
		_writer.Write(flag);

		_recordedBars++;
		_lastRecordedBar = candle.OpenTime;
	}

	private void WriteTickRecord(DateTimeOffset barOpenTime, decimal open, decimal low, decimal high, decimal close, decimal volume, DateTimeOffset eventTime)
	{
		if (_writer == null)
			return;

		_writer.Write(ToUnixSeconds(barOpenTime));
		_writer.Write(ToDouble(open));
		_writer.Write(ToDouble(low));
		_writer.Write(ToDouble(high));
		_writer.Write(ToDouble(close));
		_writer.Write(ToDouble(volume));
		_writer.Write(ToUnixSeconds(eventTime));
		_writer.Write(4);
	}

	private void CloseStorage()
	{
		lock (_sync)
		{
			if (_writer != null && _stream != null)
			{
				UpdateHeader();
				_writer.Flush();
				_writer.Dispose();
				_writer = null;
				LogInfo($"Saved {_recordedTicks} ticks and {_recordedBars} bars to '{_filePath}'.");
			}

			_reader?.Dispose();
			_reader = null;

			_stream?.Dispose();
			_stream = null;
			_filePath = null;
			_barsOffset = -1;
			_ticksOffset = -1;
			_lastBarTimeOffset = -1;
			_currentCandle = null;
		}
	}

	private void UpdateHeader()
	{
		if (_writer == null || _stream == null)
			return;

		_writer.Flush();

		if (_barsOffset >= 0)
		{
			_stream.Seek(_barsOffset, SeekOrigin.Begin);
			_writer.Write(_recordedBars);
		}

		if (_ticksOffset >= 0)
		{
			_stream.Seek(_ticksOffset, SeekOrigin.Begin);
			_writer.Write(_recordedTicks);
		}

		if (_lastBarTimeOffset >= 0)
		{
			var seconds = _lastRecordedBar.HasValue ? _lastRecordedBar.Value.ToUnixTimeSeconds() : 0L;
			_stream.Seek(_lastBarTimeOffset, SeekOrigin.Begin);
			_writer.Write(seconds);
		}

		_writer.Flush();
		_stream.Seek(0, SeekOrigin.End);
	}

	private long GetTimeFrameSeconds()
	{
		if (_timeFrame <= TimeSpan.Zero)
			return 0L;

		var seconds = _timeFrame.Ticks / TimeSpan.TicksPerSecond;
		return seconds <= 0 ? 1 : seconds;
	}

	private DateTimeOffset AlignTime(DateTimeOffset time)
	{
		if (_timeFrame <= TimeSpan.Zero)
			return time;

		var frameTicks = _timeFrame.Ticks;
		if (frameTicks <= 0)
			return time;

		var alignedTicks = time.UtcTicks - (time.UtcTicks % frameTicks);
		return new DateTimeOffset(alignedTicks, TimeSpan.Zero);
	}

	private static long ToUnixSeconds(DateTimeOffset time)
	{
		var actual = time == default ? DateTimeOffset.UtcNow : time.ToUniversalTime();
		return actual.ToUnixTimeSeconds();
	}

	private static double ToDouble(decimal value)
	{
		return (double)value;
	}

	private static double ToDouble(decimal? value)
	{
		return value.HasValue ? (double)value.Value : 0d;
	}

	private static string SanitizeFileName(string input)
	{
		if (string.IsNullOrEmpty(input))
			return "Unknown";

		var invalid = Path.GetInvalidFileNameChars();
		var builder = new StringBuilder(input.Length);

		foreach (var ch in input)
		{
			var replacement = ch;

			for (var i = 0; i < invalid.Length; i++)
			{
				if (invalid[i] != ch)
					continue;

				replacement = '_';
				break;
			}

			builder.Append(replacement);
		}

		return builder.ToString();
	}

	private static string GetTimeFrameSuffix(DataType dataType)
	{
		if (dataType.TimeFrame is TimeSpan span && span > TimeSpan.Zero)
		{
			var minutes = span.TotalMinutes;
			if (minutes >= 1 && Math.Abs(minutes - Math.Round(minutes)) < 1e-6)
				return ((int)Math.Round(minutes)).ToString(CultureInfo.InvariantCulture);

			var seconds = span.TotalSeconds;
			if (seconds >= 1 && Math.Abs(seconds - Math.Round(seconds)) < 1e-6)
				return $"S{(int)Math.Round(seconds)}";

			return $"T{span.Ticks}";
		}

		return dataType.ToString();
	}
}
