
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the MetaTrader 4 AllMinutes script that maintains minute aligned history files.
/// The strategy subscribes to several symbols and writes continuous HST files that skip week-end gaps.
/// </summary>
public class AllMinutesStrategy : Strategy
{
	private readonly StrategyParam<string> _chartList;
	private readonly StrategyParam<bool> _skipWeekends;
	private readonly StrategyParam<int> _refreshInterval;
	private readonly StrategyParam<string> _outputDirectory;

	private readonly List<ChartState> _chartStates = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="AllMinutesStrategy"/> class.
	/// </summary>
	public AllMinutesStrategy()
	{
		_chartList = Param(nameof(ChartList), "EURUSD@FXCM 1,GBPUSD@FXCM 1")
		.SetDisplay("Chart List", "Comma separated list with pattern 'symbol timeframe'", "General");

		_skipWeekends = Param(nameof(SkipWeekends), true)
		.SetDisplay("Skip Weekends", "Ignore Saturdays and Sundays when filling gaps", "General");

		_refreshInterval = Param(nameof(RefreshInterval), 1000)
		.SetGreaterThanZero()
		.SetDisplay("Flush Interval", "Timer interval in milliseconds used to flush files", "General");

		_outputDirectory = Param(nameof(OutputDirectory), Path.Combine(Environment.CurrentDirectory, "AllMinutes"))
		.SetDisplay("Output Directory", "Destination folder where HST files are created", "Storage");
	}

	/// <summary>
	/// Comma separated list with entries such as "EURUSD@FXCM 1".
	/// </summary>
	public string ChartList
	{
		get => _chartList.Value;
		set => _chartList.Value = value;
	}

	/// <summary>
	/// Ignore weekend timestamps while creating synthetic candles.
	/// </summary>
	public bool SkipWeekends
	{
		get => _skipWeekends.Value;
		set => _skipWeekends.Value = value;
	}

	/// <summary>
	/// Timer interval that ensures file buffers are flushed periodically.
	/// </summary>
	public int RefreshInterval
	{
		get => _refreshInterval.Value;
		set => _refreshInterval.Value = value;
	}

	/// <summary>
	/// Directory where the generated HST files are stored.
	/// </summary>
	public string OutputDirectory
	{
		get => _outputDirectory.Value;
		set => _outputDirectory.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		DisposeStates();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var states = ParseChartList().ToList();
		if (states.Count == 0)
		throw new InvalidOperationException("ChartList is empty. Provide at least one symbol and timeframe pair.");

		InitializeDirectory();

		foreach (var state in states)
		{
			InitializeStorage(state);

			state.Subscription = SubscribeCandles(state.CandleType, true, state.Security)
			.Bind(candle => ProcessCandle(state, candle))
			.Start();

			_chartStates.Add(state);
		}

		RegisterTimer(TimeSpan.FromMilliseconds(RefreshInterval), OnTimer);

		LogInfo("AllMinutes strategy started with {0} chart definitions.", _chartStates.Count);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		UnregisterTimer(OnTimer);

		DisposeStates();

		base.OnStopped();
	}

	private void OnTimer()
	{
		foreach (var state in _chartStates)
		{
			state.Flush();
		}
	}

	private IEnumerable<ChartState> ParseChartList()
	{
		var provider = SecurityProvider ?? throw new InvalidOperationException("SecurityProvider is not assigned.");

		var raw = ChartList ?? string.Empty;
		var entries = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var entry in entries)
		{
			var tokens = entry.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length != 2)
			throw new InvalidOperationException($"Entry '{entry}' is invalid. Expected 'symbol timeframe'.");

			var symbolToken = tokens[0];
			var timeframeToken = tokens[1];

			if (!int.TryParse(timeframeToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) || minutes <= 0)
			throw new InvalidOperationException($"Timeframe '{timeframeToken}' is invalid. Use positive integers representing minutes.");

			var security = ResolveSecurity(symbolToken, provider);
			var candleType = TimeSpan.FromMinutes(minutes).TimeFrame();

			yield return new ChartState(security, minutes, candleType, OutputDirectory);
		}
	}

	private Security ResolveSecurity(string token, ISecurityProvider provider)
	{
		var security = provider.LookupById(token);
		if (security != null)
		return security;

		if (Security != null)
		{
			if (string.Equals(Security.Id, token, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(Security.Code, token, StringComparison.OrdinalIgnoreCase))
			return Security;

			if (!token.Contains('@', StringComparison.Ordinal))
			{
				var boardCode = Security.Board?.Code;
				if (!string.IsNullOrEmpty(boardCode))
				{
					security = provider.LookupById($"{token}@{boardCode}");
					if (security != null)
					return security;
				}
			}
		}

		throw new InvalidOperationException($"Security '{token}' cannot be resolved.");
	}

	private void InitializeDirectory()
	{
		var directory = OutputDirectory;
		if (string.IsNullOrWhiteSpace(directory))
		directory = Environment.CurrentDirectory;

		if (!Directory.Exists(directory))
		Directory.CreateDirectory(directory);
	}

	private void InitializeStorage(ChartState state)
	{
		state.Open();
		state.WriteHeader();
	}

	private void DisposeStates()
	{
		foreach (var state in _chartStates)
		{
			state.Dispose();
		}

		_chartStates.Clear();
	}

	private void ProcessCandle(ChartState state, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished && candle.State != CandleStates.Active)
		return;

		if (candle.OpenPrice is null || candle.HighPrice is null || candle.LowPrice is null || candle.ClosePrice is null)
		return;

		var openTime = candle.OpenTime;
		var alignedTime = AlignTime(openTime, state.TimeFrame);

		if (state.LastAlignedTime is null)
		{
			WriteActual(state, alignedTime, candle);
			return;
		}

		var lastTime = state.LastAlignedTime.Value;

		if (alignedTime < lastTime)
		{
			return;
		}

		if (alignedTime == lastTime)
		{
			WriteActual(state, alignedTime, candle, rewrite: true);
			return;
		}

		FillMissing(state, lastTime, alignedTime);
		WriteActual(state, alignedTime, candle);
	}

	private void FillMissing(ChartState state, DateTimeOffset lastTime, DateTimeOffset currentTime)
	{
		var expected = lastTime;
		while (true)
		{
			expected = expected.Add(state.TimeFrame);
			if (expected >= currentTime)
			break;

			if (SkipWeekends && ShouldSkipWeekend(expected, state.TimeFrame))
			continue;

			var close = state.LastClose ?? state.LastKnownPrice;
			if (close is null)
			continue;

			var value = close.Value;
			state.WriteRecord(expected, value, value, value, value, 1, rewrite: false);
		}
	}

	private void WriteActual(ChartState state, DateTimeOffset alignedTime, ICandleMessage candle, bool rewrite = false)
	{
		var open = candle.OpenPrice!.Value;
		var high = candle.HighPrice!.Value;
		var low = candle.LowPrice!.Value;
		var close = candle.ClosePrice!.Value;
		var volume = candle.TotalVolume ?? candle.Volume ?? 0m;

		state.WriteRecord(alignedTime, open, high, low, close, volume, rewrite);

		state.LastAlignedTime = alignedTime;
		state.LastOpen = open;
		state.LastHigh = high;
		state.LastLow = low;
		state.LastClose = close;
		state.LastVolume = volume;
	}

	private static DateTimeOffset AlignTime(DateTimeOffset time, TimeSpan frame)
	{
		var seconds = (long)frame.TotalSeconds;
		if (seconds <= 0)
		return time;

		var unix = time.ToUnixTimeSeconds();
		var aligned = unix / seconds * seconds;
		return DateTimeOffset.FromUnixTimeSeconds(aligned);
	}

	private bool ShouldSkipWeekend(DateTimeOffset time, TimeSpan frame)
	{
		var utc = time.UtcDateTime;
		if (utc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
		return true;

		if (utc.DayOfWeek == DayOfWeek.Friday)
		{
			var next = utc.Add(frame);
			if (utc.Hour == 23 || next.Hour == 23)
			return true;
		}

		return false;
	}

	private sealed class ChartState : IDisposable
	{
		private const int HeaderVersion = 401;
		private const int ReservedCount = 13;
		private const int RecordSize = 56;

		private readonly string _filePath;

		private FileStream? _stream;
		private BinaryWriter? _writer;

		public ChartState(Security security, int minutes, DataType candleType, string directory)
		{
			Security = security;
			Minutes = minutes;
			CandleType = candleType;
			TimeFrame = TimeSpan.FromMinutes(minutes);
			_filePath = Path.Combine(directory, $"ALL{Sanitize(security.Code)}{minutes}.hst");
		}

		public Security Security { get; }
		public int Minutes { get; }
		public DataType CandleType { get; }
		public TimeSpan TimeFrame { get; }
		public MarketDataSubscription? Subscription { get; set; }
		public DateTimeOffset? LastAlignedTime { get; set; }
		public decimal? LastOpen { get; set; }
		public decimal? LastHigh { get; set; }
		public decimal? LastLow { get; set; }
		public decimal? LastClose { get; set; }
		public decimal? LastVolume { get; set; }
		public decimal? LastKnownPrice => LastClose ?? LastOpen ?? LastHigh ?? LastLow;
		public long LastRecordOffset { get; private set; } = -1;

		public void Open()
		{
			var exists = File.Exists(_filePath);
			_stream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			_writer = new BinaryWriter(_stream, Encoding.ASCII, leaveOpen: true);

			if (exists && _stream.Length >= RecordSize)
			{
				LastRecordOffset = _stream.Length - RecordSize;
			}
		}

		public void WriteHeader()
		{
			if (_stream == null || _writer == null)
			throw new InvalidOperationException("Storage is not open.");

			if (_stream.Length > 0)
			return;

			_stream.Position = 0;
			_writer.Write(HeaderVersion);
			WriteFixedString(_writer, "Copyright 2006-2015, komposter", 64);
			WriteFixedString(_writer, $"ALL{Security.Code}", 12);
			_writer.Write(Minutes);
			_writer.Write(GetDigits(Security));
			_writer.Write(0);
			_writer.Write(0);
			for (var i = 0; i < ReservedCount; i++)
			{
				_writer.Write(0);
			}

			_writer.Flush();
			_stream.Flush(true);
		}

		public void WriteRecord(DateTimeOffset time, decimal open, decimal high, decimal low, decimal close, decimal volume, bool rewrite)
		{
			if (_stream == null || _writer == null)
			return;

			var position = rewrite && LastRecordOffset >= 0 ? LastRecordOffset : _stream.Length;
			_stream.Position = position;

			var unix = (int)time.ToUnixTimeSeconds();
			var tickVolume = Convert.ToInt64(decimal.Round(volume, MidpointRounding.AwayFromZero));
			if (tickVolume < 0)
			tickVolume = 0;

			_writer.Write(unix);
			_writer.Write((double)open);
			_writer.Write((double)high);
			_writer.Write((double)low);
			_writer.Write((double)close);
			_writer.Write(tickVolume);
			_writer.Write(0);
			_writer.Write(0L);

			_writer.Flush();
			_stream.Flush(true);

			LastRecordOffset = _stream.Position - RecordSize;
		}

		public void Flush()
		{
			_stream?.Flush(true);
		}

		public void Dispose()
		{
			Subscription?.Dispose();
			_writer?.Dispose();
			_stream?.Dispose();
		}

		private static string Sanitize(string value)
		{
			var invalid = Path.GetInvalidFileNameChars();
			var builder = new StringBuilder(value.Length);
			foreach (var ch in value)
			{
				builder.Append(invalid.Contains(ch) ? '_' : ch);
			}

			return builder.ToString();
		}

		private static void WriteFixedString(BinaryWriter writer, string value, int length)
		{
			var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
			var span = new byte[length];
			var count = Math.Min(length, bytes.Length);
			Array.Copy(bytes, span, count);
			writer.Write(span);
		}

		private static int GetDigits(Security security)
		{
			if (security.Decimals is int digits && digits >= 0)
			return digits;

			if (security.PriceStep != null && security.PriceStep > 0)
			{
				var power = (int)Math.Round(Math.Log10((double)(1m / security.PriceStep.Value)), MidpointRounding.AwayFromZero);
				if (power > 0)
				return power;
			}

			return 5;
		}
	}
}
