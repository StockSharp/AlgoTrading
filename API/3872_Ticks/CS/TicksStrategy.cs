using System;
using System.Globalization;
using System.IO;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that collects bid ticks in fixed-size batches and writes OHLC rows to a CSV file.
/// </summary>
public class TicksStrategy : Strategy
{
	private readonly StrategyParam<int> _ticksPerBatchParam;
	private readonly StrategyParam<string> _outputDirectoryParam;
	private readonly StrategyParam<string> _customFileNameParam;

	private MarketDataSubscription? _level1Subscription;
	private StreamWriter? _writer;
	private int _processedTicks;
	private decimal _openPrice;
	private decimal _highPrice;
	private decimal _lowPrice;
	private DateTimeOffset? _batchStartTime;
	private bool _isNewBatch;

	/// <summary>
	/// Initializes a new instance of the <see cref="TicksStrategy"/> class.
	/// </summary>
	public TicksStrategy()
	{
		_ticksPerBatchParam = Param(nameof(TicksPerBatch), 100)
		.SetDisplay("Ticks Per Batch", "Number of bid updates stored before writing a CSV row", "Recording")
		.SetGreaterThanZero();

		_outputDirectoryParam = Param(nameof(OutputDirectory), string.Empty)
		.SetDisplay("Output Directory", "Optional folder for the generated CSV file", "Recording")
		.SetCanOptimize(false);

		_customFileNameParam = Param(nameof(CustomFileName), string.Empty)
		.SetDisplay("Custom File Name", "Optional file name override without directory", "Recording")
		.SetCanOptimize(false);

		_isNewBatch = true;
	}

	/// <summary>
	/// Number of bid ticks that forms one OHLC record.
	/// </summary>
	public int TicksPerBatch
	{
		get => _ticksPerBatchParam.Value;
		set => _ticksPerBatchParam.Value = value;
	}

	/// <summary>
	/// Optional directory where the CSV file is stored.
	/// </summary>
	public string OutputDirectory
	{
		get => _outputDirectoryParam.Value;
		set => _outputDirectoryParam.Value = value;
	}

	/// <summary>
	/// Optional file name override without directory information.
	/// </summary>
	public string CustomFileName
	{
		get => _customFileNameParam.Value;
		set => _customFileNameParam.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		DisposeSubscription();
		CloseWriter();

		_processedTicks = 0;
		_openPrice = 0m;
		_highPrice = 0m;
		_lowPrice = 0m;
		_batchStartTime = null;
		_isNewBatch = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		{
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");
		}

		CloseWriter();
		DisposeSubscription();

		_processedTicks = 0;
		_batchStartTime = null;
		_isNewBatch = true;

		_writer = CreateWriter(Security.Id);

		_level1Subscription = SubscribeLevel1();
		_level1Subscription.Bind(ProcessLevel1).Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		DisposeSubscription();
		CloseWriter();

		base.OnStopped();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (!message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var rawBid))
		{
			return;
		}

		var bidPrice = (decimal)rawBid;

		var timestamp = message.ServerTime != default
		? message.ServerTime
		: message.LocalTime != default
		? message.LocalTime
		: CurrentTime;

		if (_isNewBatch)
		{
			// First tick in the batch defines the open price and timestamp.
			_openPrice = bidPrice;
			_highPrice = bidPrice;
			_lowPrice = bidPrice;
			_batchStartTime = timestamp;
			_isNewBatch = false;
		}
		else
		{
			// Track the running high/low for the active batch.
			if (bidPrice > _highPrice)
			{
				_highPrice = bidPrice;
			}

			if (bidPrice < _lowPrice)
			{
				_lowPrice = bidPrice;
			}
		}

		_processedTicks++;

		if (_processedTicks < TicksPerBatch)
		{
			return;
		}

		// The last tick becomes the close price and finalizes the CSV row.
		WriteBatch(timestamp, bidPrice, _processedTicks);

		_processedTicks = 0;
		_isNewBatch = true;
	}

	private void WriteBatch(DateTimeOffset timestamp, decimal closePrice, int tickCount)
	{
		if (_writer == null || _batchStartTime == null)
		{
			return;
		}

		var startTime = _batchStartTime.Value.DateTime;
		var date = startTime.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
		var time = startTime.ToString("HH:mm", CultureInfo.InvariantCulture);

		var line = string.Join(",", new[]
		{
			date,
			time,
			_openPrice.ToString(CultureInfo.InvariantCulture),
			_highPrice.ToString(CultureInfo.InvariantCulture),
			_lowPrice.ToString(CultureInfo.InvariantCulture),
			closePrice.ToString(CultureInfo.InvariantCulture),
			tickCount.ToString(CultureInfo.InvariantCulture)
		});

		_writer.WriteLine(line);
		_writer.Flush();
	}

	private StreamWriter CreateWriter(string securityId)
	{
		var directory = string.IsNullOrWhiteSpace(OutputDirectory)
		? Environment.CurrentDirectory
		: OutputDirectory;

		Directory.CreateDirectory(directory);

		var fileName = string.IsNullOrWhiteSpace(CustomFileName)
		? $"{securityId} volume {TicksPerBatch}.csv"
		: CustomFileName;

		var path = Path.Combine(directory, fileName);

		return new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
	}

	private void DisposeSubscription()
	{
		_level1Subscription?.Dispose();
		_level1Subscription = null;
	}

	private void CloseWriter()
	{
		_writer?.Dispose();
		_writer = null;
	}
}
