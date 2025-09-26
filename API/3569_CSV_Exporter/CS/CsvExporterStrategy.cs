using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Periodically exports the latest candle close prices to a CSV file, mirroring the MetaTrader CSV Exporter expert.
/// </summary>
public class CsvExporterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _candleCount;
	private readonly StrategyParam<TimeSpan> _updateInterval;
	private readonly StrategyParam<string> _exportDirectory;

	private ICandleManagerSubscription? _candleSubscription;
	private decimal[] _closeBuffer = Array.Empty<decimal>();
	private int _writeIndex;
	private int _valueCount;
	private DateTimeOffset _nextExportTime = DateTimeOffset.MinValue;
	private string? _lastExportStatus;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvExporterStrategy"/> class.
	/// </summary>
	public CsvExporterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for exporting price data.", "General");

		_candleCount = Param(nameof(CandleCount), 133)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Candle Count", "Number of finished candles exported to the CSV file.", "Export");

		_updateInterval = Param(nameof(UpdateInterval), TimeSpan.FromMinutes(15))
			.SetDisplay("Update Interval", "Time interval between CSV exports.", "Export");

		_exportDirectory = Param(nameof(ExportDirectory), "DataExports")
			.SetDisplay("Export Directory", "Directory path where the CSV file will be saved.", "Export");
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of completed candles written to the export file.
	/// </summary>
	public int CandleCount
	{
		get => _candleCount.Value;
		set => _candleCount.Value = value;
	}

	/// <summary>
	/// Time interval between consecutive exports.
	/// </summary>
	public TimeSpan UpdateInterval
	{
		get => _updateInterval.Value;
		set => _updateInterval.Value = value;
	}

	/// <summary>
	/// Directory where the CSV file is stored.
	/// </summary>
	public string ExportDirectory
	{
		get => _exportDirectory.Value;
		set => _exportDirectory.Value = value;
	}

	/// <summary>
	/// Returns the text of the latest export notification.
	/// </summary>
	public string? LastExportStatus => _lastExportStatus;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		EnsureBufferSize();

		if (UpdateInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("UpdateInterval must be positive.");

		_nextExportTime = time + TimeSpan.FromSeconds(1);

		if (Security is null)
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		_candleSubscription = SubscribeCandles(CandleType);
		_candleSubscription
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		_candleSubscription?.Stop();
		_candleSubscription = null;

		base.OnStopped(time);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		EnsureBufferSize();

		if (_closeBuffer.Length == 0)
			return;

		StoreClose(candle.ClosePrice);

		var currentTime = candle.CloseTime;

		if (_nextExportTime == DateTimeOffset.MinValue)
			_nextExportTime = currentTime + TimeSpan.FromSeconds(1);

		if (currentTime < _nextExportTime)
			return;

		ExportCloses(currentTime);

		_nextExportTime = currentTime + UpdateInterval;
	}

	private void StoreClose(decimal closePrice)
	{
		_closeBuffer[_writeIndex] = closePrice;
		_writeIndex = (_writeIndex + 1) % _closeBuffer.Length;

		if (_valueCount < _closeBuffer.Length)
			_valueCount++;
	}

	private void ExportCloses(DateTimeOffset time)
	{
		if (_valueCount == 0)
			return;

		var orderedValues = new decimal[_valueCount];

		for (var i = 0; i < orderedValues.Length; i++)
		{
			var index = _writeIndex - _valueCount + i;

			if (index < 0)
				index += _closeBuffer.Length;

			orderedValues[i] = _closeBuffer[index];
		}

		var directory = string.IsNullOrWhiteSpace(ExportDirectory)
			? Environment.CurrentDirectory
			: ExportDirectory;

		Directory.CreateDirectory(directory);

		var fileName = BuildFileName();
		var path = Path.Combine(directory, fileName);

		using (var writer = new StreamWriter(path, false, Encoding.UTF8))
		{
			foreach (var value in orderedValues)
				writer.WriteLine(value.ToString(CultureInfo.InvariantCulture));
		}

		_lastExportStatus = $"Exported {orderedValues.Length} closes to {path} at {time:O}.";
		this.LogInfo(_lastExportStatus);
	}

	private void EnsureBufferSize()
	{
		var required = Math.Max(CandleCount, 0);

		if (required == 0)
		{
			_closeBuffer = Array.Empty<decimal>();
			_writeIndex = 0;
			_valueCount = 0;
			return;
		}

		if (_closeBuffer.Length == required)
			return;

		_closeBuffer = new decimal[required];
		_writeIndex = 0;
		_valueCount = 0;
	}

	private string BuildFileName()
	{
		var securityId = Security?.Id.ToString() ?? "UnknownSecurity";
		var candleSuffix = CandleType.Arg switch
		{
			TimeSpan frame => $"TF_{(long)frame.TotalSeconds}s",
			null => CandleType.MessageType.Name,
			_ => CandleType.Arg.ToString() ?? CandleType.MessageType.Name,
		};

		return $"{MakeFileSystemSafe(securityId)}_{MakeFileSystemSafe(candleSuffix)}.csv";
	}

	private static string MakeFileSystemSafe(string value)
	{
		var invalid = Path.GetInvalidFileNameChars();
		return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
	}
}
