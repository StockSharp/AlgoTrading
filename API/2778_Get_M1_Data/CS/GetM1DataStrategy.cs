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
/// Strategy that collects minute candles and exports them when execution stops.
/// </summary>
public class GetM1DataStrategy : Strategy
{
	private readonly StrategyParam<string> _fileName;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _writeCsv;
	private readonly StrategyParam<bool> _writeBinary;

	private readonly List<CandleInfo> _history = new();

	private readonly record struct CandleInfo(
		DateTimeOffset Time,
		decimal Open,
		decimal High,
		decimal Low,
		decimal Close,
		decimal Volume);

	/// <summary>
	/// Base file name used to build export paths.
	/// </summary>
	public string FileName
	{
		get => _fileName.Value;
		set => _fileName.Value = value;
	}

	/// <summary>
	/// Candle type that will be subscribed and exported.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Indicates whether CSV output should be produced.
	/// </summary>
	public bool WriteCsv
	{
		get => _writeCsv.Value;
		set => _writeCsv.Value = value;
	}

	/// <summary>
	/// Indicates whether a binary snapshot should be produced.
	/// </summary>
	public bool WriteBinary
	{
		get => _writeBinary.Value;
		set => _writeBinary.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults inspired by the MQL version.
	/// </summary>
	public GetM1DataStrategy()
	{
		_fileName = Param(nameof(FileName), "GetM1Data")
			.SetDisplay("File Name", "Base name for export files", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles collected for export", "General");

		_writeCsv = Param(nameof(WriteCsv), true)
			.SetDisplay("Write CSV", "Export a CSV file", "Output");

		_writeBinary = Param(nameof(WriteBinary), false)
			.SetDisplay("Write Binary", "Export a binary file", "Output");
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
		_history.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!WriteCsv && !WriteBinary)
			return;

		var volume = candle.TotalVolume;
		if (volume == 0m && candle.Volume != 0m)
		{
			// Some providers populate only the Volume field, therefore fallback to it.
			volume = candle.Volume;
		}

		var info = new CandleInfo(
			candle.OpenTime,
			candle.OpenPrice,
			candle.HighPrice,
			candle.LowPrice,
			candle.ClosePrice,
			volume);

		// Keep finished candle values for export during OnStopped.
		_history.Add(info);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		try
		{
			SaveExports();
		}
		finally
		{
			_history.Clear();
			base.OnStopped();
		}
	}

	private void SaveExports()
	{
		if (!WriteCsv && !WriteBinary)
		{
			LogInfo("No export formats enabled; skipping file generation.");
			return;
		}

		if (_history.Count == 0)
		{
			LogInfo("No finished candles collected for export.");
			return;
		}

		if (string.IsNullOrWhiteSpace(FileName))
		{
			LogWarning("File name is empty; cannot export candle history.");
			return;
		}

		var basePath = Path.GetFullPath(FileName);
		var directory = Path.GetDirectoryName(basePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			// Ensure destination directory exists before writing files.
			Directory.CreateDirectory(directory);
		}

		if (WriteCsv)
		{
			var csvPath = Path.ChangeExtension(basePath, ".csv");
			WriteCsvFile(csvPath);
		}

		if (WriteBinary)
		{
			var binaryPath = Path.ChangeExtension(basePath, ".bin");
			WriteBinaryFile(binaryPath);
		}
	}

	private void WriteCsvFile(string path)
	{
		using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
		writer.WriteLine("Time,Open,High,Low,Close,Volume");

		foreach (var candle in _history)
		{
			var time = candle.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			writer.WriteLine(string.Join(',', new[]
			{
				time,
				candle.Open.ToString(CultureInfo.InvariantCulture),
				candle.High.ToString(CultureInfo.InvariantCulture),
				candle.Low.ToString(CultureInfo.InvariantCulture),
				candle.Close.ToString(CultureInfo.InvariantCulture),
				candle.Volume.ToString(CultureInfo.InvariantCulture),
			}));
		}

		LogInfo($"Saved {_history.Count} candles to {path}.");
	}

	private void WriteBinaryFile(string path)
	{
		using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
		using var writer = new BinaryWriter(stream, new UTF8Encoding(false), leaveOpen: false);

		// Store a simple header with version and count to validate files later.
		writer.Write(1); // version marker
		writer.Write(_history.Count);

		foreach (var candle in _history)
		{
			writer.Write(candle.Time.UtcDateTime.ToBinary());
			writer.Write(candle.Open);
			writer.Write(candle.High);
			writer.Write(candle.Low);
			writer.Write(candle.Close);
			writer.Write(candle.Volume);
		}

		LogInfo($"Saved {_history.Count} candles to {path}.");
	}
}
