using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that periodically exports historical candles for multiple timeframes.
/// Replicates the behaviour of the MetaTrader Currency_Loader script by writing CSV snapshots.
/// </summary>
public class CurrencyLoaderStrategy : Strategy
{
	private const string ExpertName = "Currency_Loader";

	private readonly StrategyParam<int> _barsMin;
	private readonly StrategyParam<int> _maxBarsInFile;
	private readonly StrategyParam<int> _frequencyUpdateSeconds;
	private readonly StrategyParam<bool> _loadM1;
	private readonly StrategyParam<bool> _loadM5;
	private readonly StrategyParam<bool> _loadM15;
	private readonly StrategyParam<bool> _loadM30;
	private readonly StrategyParam<bool> _loadH1;
	private readonly StrategyParam<bool> _loadH4;
	private readonly StrategyParam<bool> _loadD1;
	private readonly StrategyParam<bool> _loadW1;
	private readonly StrategyParam<bool> _loadMn;
	private readonly StrategyParam<bool> _allowInfo;
	private readonly StrategyParam<bool> _allowLogFile;

	private readonly List<TimeframeExport> _exports = new();

	private string? _baseDirectory;
	private string? _safeSymbol;
	private int _priceDigits;

	private readonly record struct CandleSnapshot(
	DateTimeOffset Time,
	decimal Open,
	decimal High,
	decimal Low,
	decimal Close,
	decimal Volume);

	private sealed class TimeframeExport
	{
		public TimeframeExport(string suffix, TimeSpan timeframe, string filePath, DataType candleType)
		{
			Suffix = suffix;
			Timeframe = timeframe;
			FilePath = filePath;
			CandleType = candleType;
		}

		public string Suffix { get; }

		public TimeSpan Timeframe { get; }

		public DataType CandleType { get; }

		public string FilePath { get; }

		public List<CandleSnapshot> History { get; } = new();

		public bool HasPendingExport { get; set; }
	}

	/// <summary>
	/// Minimum number of finished candles required before exporting.
	/// </summary>
	public int BarsMin
	{
		get => _barsMin.Value;
		set => _barsMin.Value = value;
	}

	/// <summary>
	/// Maximum number of candles written to a CSV snapshot.
	/// </summary>
	public int MaxBarsInFile
	{
		get => _maxBarsInFile.Value;
		set => _maxBarsInFile.Value = value;
	}

	/// <summary>
	/// Frequency of CSV updates in seconds.
	/// </summary>
	public int FrequencyUpdateSeconds
	{
		get => _frequencyUpdateSeconds.Value;
		set => _frequencyUpdateSeconds.Value = value;
	}

	/// <summary>
	/// Enables export for the M1 timeframe.
	/// </summary>
	public bool LoadM1
	{
		get => _loadM1.Value;
		set => _loadM1.Value = value;
	}

	/// <summary>
	/// Enables export for the M5 timeframe.
	/// </summary>
	public bool LoadM5
	{
		get => _loadM5.Value;
		set => _loadM5.Value = value;
	}

	/// <summary>
	/// Enables export for the M15 timeframe.
	/// </summary>
	public bool LoadM15
	{
		get => _loadM15.Value;
		set => _loadM15.Value = value;
	}

	/// <summary>
	/// Enables export for the M30 timeframe.
	/// </summary>
	public bool LoadM30
	{
		get => _loadM30.Value;
		set => _loadM30.Value = value;
	}

	/// <summary>
	/// Enables export for the H1 timeframe.
	/// </summary>
	public bool LoadH1
	{
		get => _loadH1.Value;
		set => _loadH1.Value = value;
	}

	/// <summary>
	/// Enables export for the H4 timeframe.
	/// </summary>
	public bool LoadH4
	{
		get => _loadH4.Value;
		set => _loadH4.Value = value;
	}

	/// <summary>
	/// Enables export for the D1 timeframe.
	/// </summary>
	public bool LoadD1
	{
		get => _loadD1.Value;
		set => _loadD1.Value = value;
	}

	/// <summary>
	/// Enables export for the W1 timeframe.
	/// </summary>
	public bool LoadW1
	{
		get => _loadW1.Value;
		set => _loadW1.Value = value;
	}

	/// <summary>
	/// Enables export for the monthly timeframe.
	/// </summary>
	public bool LoadMn
	{
		get => _loadMn.Value;
		set => _loadMn.Value = value;
	}

	/// <summary>
	/// Controls whether informational log messages are produced.
	/// </summary>
	public bool AllowInfo
	{
		get => _allowInfo.Value;
		set => _allowInfo.Value = value;
	}

	/// <summary>
	/// Controls whether a side log file is generated.
	/// </summary>
	public bool AllowLogFile
	{
		get => _allowLogFile.Value;
		set => _allowLogFile.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults derived from the MetaTrader script.
	/// </summary>
	public CurrencyLoaderStrategy()
	{
		_barsMin = Param(nameof(BarsMin), 100)
		.SetDisplay("Minimum Bars", "Minimum history depth required before exporting", "General")
		.SetGreaterThanZero();

		_maxBarsInFile = Param(nameof(MaxBarsInFile), 20000)
		.SetDisplay("Maximum Bars", "Upper limit for candles stored in each CSV", "General")
		.SetGreaterThanZero();

		_frequencyUpdateSeconds = Param(nameof(FrequencyUpdateSeconds), 60)
		.SetDisplay("Update Frequency", "Seconds between export attempts", "General")
		.SetGreaterThanZero();

		_loadM1 = Param(nameof(LoadM1), false)
		.SetDisplay("Export M1", "Write minute candles", "Timeframes")
		.SetCanOptimize(false);

		_loadM5 = Param(nameof(LoadM5), false)
		.SetDisplay("Export M5", "Write five-minute candles", "Timeframes")
		.SetCanOptimize(false);

		_loadM15 = Param(nameof(LoadM15), false)
		.SetDisplay("Export M15", "Write fifteen-minute candles", "Timeframes")
		.SetCanOptimize(false);

		_loadM30 = Param(nameof(LoadM30), false)
		.SetDisplay("Export M30", "Write thirty-minute candles", "Timeframes")
		.SetCanOptimize(false);

		_loadH1 = Param(nameof(LoadH1), false)
		.SetDisplay("Export H1", "Write hourly candles", "Timeframes")
		.SetCanOptimize(false);

		_loadH4 = Param(nameof(LoadH4), false)
		.SetDisplay("Export H4", "Write four-hour candles", "Timeframes")
		.SetCanOptimize(false);

		_loadD1 = Param(nameof(LoadD1), false)
		.SetDisplay("Export D1", "Write daily candles", "Timeframes")
		.SetCanOptimize(false);

		_loadW1 = Param(nameof(LoadW1), false)
		.SetDisplay("Export W1", "Write weekly candles", "Timeframes")
		.SetCanOptimize(false);

		_loadMn = Param(nameof(LoadMn), false)
		.SetDisplay("Export MN", "Write monthly candles", "Timeframes")
		.SetCanOptimize(false);

		_allowInfo = Param(nameof(AllowInfo), true)
		.SetDisplay("Allow Info", "Log informational messages", "Logging")
		.SetCanOptimize(false);

		_allowLogFile = Param(nameof(AllowLogFile), true)
		.SetDisplay("Allow Log File", "Append a secondary text log", "Logging")
		.SetCanOptimize(false);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security == null)
		{
			yield break;
		}

		if (LoadM1)
		{
			yield return (security, TimeSpan.FromMinutes(1).TimeFrame());
		}

		if (LoadM5)
		{
			yield return (security, TimeSpan.FromMinutes(5).TimeFrame());
		}

		if (LoadM15)
		{
			yield return (security, TimeSpan.FromMinutes(15).TimeFrame());
		}

		if (LoadM30)
		{
			yield return (security, TimeSpan.FromMinutes(30).TimeFrame());
		}

		if (LoadH1)
		{
			yield return (security, TimeSpan.FromHours(1).TimeFrame());
		}

		if (LoadH4)
		{
			yield return (security, TimeSpan.FromHours(4).TimeFrame());
		}

		if (LoadD1)
		{
			yield return (security, TimeSpan.FromDays(1).TimeFrame());
		}

		if (LoadW1)
		{
			yield return (security, TimeSpan.FromDays(7).TimeFrame());
		}

		if (LoadMn)
		{
			yield return (security, TimeSpan.FromDays(30).TimeFrame());
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_exports.Clear();
		_baseDirectory = null;
		_safeSymbol = null;
		_priceDigits = 0;
		TimerInterval = TimeSpan.Zero;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_exports.Clear();

		var security = Security ?? throw new InvalidOperationException("Security is not specified.");

		_safeSymbol = MakeSafeName(security.Id);
		_baseDirectory = Path.Combine("Export_History", _safeSymbol);
		Directory.CreateDirectory(_baseDirectory);

		_priceDigits = CalculatePriceDigits();

		SetupExport(LoadM1, TimeSpan.FromMinutes(1), "M1");
		SetupExport(LoadM5, TimeSpan.FromMinutes(5), "M5");
		SetupExport(LoadM15, TimeSpan.FromMinutes(15), "M15");
		SetupExport(LoadM30, TimeSpan.FromMinutes(30), "M30");
		SetupExport(LoadH1, TimeSpan.FromHours(1), "H1");
		SetupExport(LoadH4, TimeSpan.FromHours(4), "H4");
		SetupExport(LoadD1, TimeSpan.FromDays(1), "D1");
		SetupExport(LoadW1, TimeSpan.FromDays(7), "W1");
		SetupExport(LoadMn, TimeSpan.FromDays(30), "MN");

		if (_exports.Count == 0)
		{
			WriteLog("No timeframes enabled for export.", false);
			return;
		}

		TimerInterval = TimeSpan.FromSeconds(FrequencyUpdateSeconds);
	}

	private void SetupExport(bool enabled, TimeSpan timeframe, string suffix)
	{
		if (!enabled)
		{
			return;
		}

		if (_baseDirectory == null || _safeSymbol == null)
		{
			return;
		}

		var filePath = Path.Combine(_baseDirectory, $"{_safeSymbol}_{suffix}.csv");
		var candleType = timeframe.TimeFrame();
		var export = new TimeframeExport(suffix, timeframe, filePath, candleType);
		_exports.Add(export);

		var subscription = SubscribeCandles(candleType);
		subscription
		.Bind(candle => ProcessCandle(export, candle))
		.Start();
	}

	private void ProcessCandle(TimeframeExport export, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var volume = candle.TotalVolume;
		if (volume == 0m && candle.Volume != 0m)
		{
			volume = candle.Volume;
		}

		var snapshot = new CandleSnapshot(
		candle.OpenTime,
		candle.OpenPrice,
		candle.HighPrice,
		candle.LowPrice,
		candle.ClosePrice,
		volume);

		var history = export.History;
		history.Add(snapshot);

		var maxBars = Math.Max(1, MaxBarsInFile);
		var excess = history.Count - maxBars;
		if (excess > 0)
		{
			history.RemoveRange(0, excess);
		}

		export.HasPendingExport = true;
	}

	/// <inheritdoc />
	protected override void OnTimer()
	{
		base.OnTimer();

		ExportAll(false);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		try
		{
			ExportAll(true);
		}
		finally
		{
			base.OnStopped();
		}
	}

	private void ExportAll(bool force)
	{
		if (_baseDirectory == null)
		{
			return;
		}

		var minimumBars = Math.Max(1, BarsMin);

		foreach (var export in _exports)
		{
			if (!force && !export.HasPendingExport)
			{
				continue;
			}

			if (export.History.Count < minimumBars)
			{
				continue;
			}

			try
			{
				WriteCsv(export);
				export.HasPendingExport = false;
			}
			catch (Exception ex)
			{
				WriteLog($"Failed to export {export.Suffix} candles: {ex.Message}", true);
			}
		}
	}

	private void WriteCsv(TimeframeExport export)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(export.FilePath)!);

		using var writer = new StreamWriter(export.FilePath, false, new UTF8Encoding(false));
		writer.WriteLine("\"Date\" \"Time\" \"Open\" \"High\" \"Low\" \"Close\" \"Volume\"");

		foreach (var candle in export.History)
		{
			var localTime = candle.Time.LocalDateTime;
			var datePart = localTime.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
			var timePart = localTime.ToString("HH:mm", CultureInfo.InvariantCulture);
			var open = FormatPrice(candle.Open);
			var high = FormatPrice(candle.High);
			var low = FormatPrice(candle.Low);
			var close = FormatPrice(candle.Close);
			var volume = Math.Round(candle.Volume, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

			writer.WriteLine($"{datePart},{timePart},{open},{high},{low},{close},{volume}");
		}
	}

	private string FormatPrice(decimal price)
	{
		return price.ToString($"F{_priceDigits}", CultureInfo.InvariantCulture);
	}

	private int CalculatePriceDigits()
	{
		var step = Security?.PriceStep;
		if (step == null || step == 0m)
		{
			return 5;
		}

		var digits = 0;
		var value = step.Value;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return Math.Max(digits, 1);
	}

	private void WriteLog(string message, bool isError)
	{
		if (AllowInfo)
		{
			if (isError)
			{
				LogError(message);
			}
			else
			{
				LogInfo(message);
			}
		}

		if (!AllowLogFile || _baseDirectory == null)
		{
			return;
		}

		try
		{
			var timestamp = CurrentTime.LocalDateTime;
			var fileName = $"LOG{ExpertName}_{timestamp:yyyy.MM.dd}.log";
			var path = Path.Combine(_baseDirectory, fileName);
			var line = $"{timestamp:yyyy.MM.dd HH:mm:ss} {message}{Environment.NewLine}";
			File.AppendAllText(path, line, new UTF8Encoding(false));
		}
		catch (Exception ex)
		{
			if (AllowInfo)
			{
				LogWarning($"Failed to append log file: {ex.Message}");
			}
		}
	}

	private static string MakeSafeName(string value)
	{
		foreach (var invalid in Path.GetInvalidFileNameChars())
		{
			value = value.Replace(invalid, '_');
		}

		return value;
	}
}
