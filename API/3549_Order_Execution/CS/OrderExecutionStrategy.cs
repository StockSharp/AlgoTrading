using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor OrderExecution.
/// Reads trade instructions from CSV files and executes them at predefined hours.
/// Designed for batch order placement where trade sizes can scale with account equity.
/// </summary>
public class OrderExecutionStrategy : Strategy
{
	private readonly StrategyParam<string> _symbolsFile;
	private readonly StrategyParam<string> _accountFile;
	private readonly StrategyParam<string> _ordersFile;
	private readonly StrategyParam<string> _downloadDirectory;
	private readonly StrategyParam<int> _lookBack;
	private readonly StrategyParam<int> _downloadingHour;
	private readonly StrategyParam<int> _tradingHour;
	private readonly StrategyParam<bool> _ignoreTradingHour;
	private readonly StrategyParam<bool> _ignoreTradeId;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _removeOrdersFile;
	private readonly StrategyParam<bool> _useMultiplier;
	private readonly StrategyParam<bool> _enableDebug;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<string, int> _symbolTradingHours = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<string, TradeInstruction> _instructions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly HashSet<string> _activeTradeIds = new(StringComparer.InvariantCultureIgnoreCase);

	private decimal _initialBalance;
	private StreamWriter? _debugWriter;

	/// <summary>
	/// CSV file with symbol names and trading hours (symbol,hour).
	/// </summary>
	public string SymbolsFile
	{
		get => _symbolsFile.Value;
		set => _symbolsFile.Value = value;
	}

/// <summary>
/// CSV file where the strategy writes current balance, equity, and position count.
/// </summary>
public string AccountFile
{
	get => _accountFile.Value;
	set => _accountFile.Value = value;
}

/// <summary>
/// CSV file containing trade instructions.
/// </summary>
public string OrdersFile
{
	get => _ordersFile.Value;
	set => _ordersFile.Value = value;
}

/// <summary>
/// Optional directory where downloaded market data files are stored.
/// </summary>
public string DownloadDirectory
{
	get => _downloadDirectory.Value;
	set => _downloadDirectory.Value = value;
}

/// <summary>
/// Number of candles exported when DownloadDirectory is used.
/// </summary>
public int LookBack
{
	get => _lookBack.Value;
	set => _lookBack.Value = value;
}

/// <summary>
/// Hour of the day when the account snapshot and optional downloads are performed.
/// </summary>
public int DownloadingHour
{
	get => _downloadingHour.Value;
	set => _downloadingHour.Value = value;
}

/// <summary>
/// Fallback trading hour applied when the symbols file does not contain a value.
/// </summary>
public int TradingHour
{
	get => _tradingHour.Value;
	set => _tradingHour.Value = value;
}

/// <summary>
/// Ignores the trading hour filters and executes instructions whenever the trading date matches.
/// </summary>
public bool IgnoreTradingHour
{
	get => _ignoreTradingHour.Value;
	set => _ignoreTradingHour.Value = value;
}

/// <summary>
/// Closes all positions regardless of instruction identifiers when true.
/// </summary>
public bool IgnoreTradeId
{
	get => _ignoreTradeId.Value;
	set => _ignoreTradeId.Value = value;
}

/// <summary>
/// Maximum absolute position size allowed by the strategy.
/// </summary>
public int MaxPositions
{
	get => _maxPositions.Value;
	set => _maxPositions.Value = value;
}

/// <summary>
/// Removes the orders file after it has been processed.
/// </summary>
public bool RemoveOrdersFile
{
	get => _removeOrdersFile.Value;
	set => _removeOrdersFile.Value = value;
}

/// <summary>
/// Scales trade volume by the current equity divided by the initial equity snapshot.
/// </summary>
public bool UseMultiplier
{
	get => _useMultiplier.Value;
	set => _useMultiplier.Value = value;
}

/// <summary>
/// Writes equity values to a debug log file in the orders directory when enabled.
/// </summary>
public bool EnableDebug
{
	get => _enableDebug.Value;
	set => _enableDebug.Value = value;
}

/// <summary>
/// Candle type used to detect new bars and evaluate trading hours.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

public OrderExecutionStrategy()
{
	_symbolsFile = Param(nameof(SymbolsFile), "OrdersExecution/Symbols.csv")
	.SetDisplay("Symbols File", "CSV file with symbol trading hours", "Files");

	_accountFile = Param(nameof(AccountFile), "OrdersExecution/Account.csv")
	.SetDisplay("Account File", "CSV output with balance and equity", "Files");

	_ordersFile = Param(nameof(OrdersFile), "OrdersExecution/Orders.csv")
	.SetDisplay("Orders File", "CSV with trade instructions", "Files");

	_downloadDirectory = Param(nameof(DownloadDirectory), "OrdersExecution/Data")
	.SetDisplay("Download Directory", "Folder for exported candles", "Files");

	_lookBack = Param(nameof(LookBack), 252)
	.SetGreaterThanZero()
	.SetDisplay("Look Back", "Number of candles written when exporting data", "Files");

	_downloadingHour = Param(nameof(DownloadingHour), 21)
	.SetDisplay("Downloading Hour", "Hour of the day when account info is saved", "Schedule");

	_tradingHour = Param(nameof(TradingHour), 23)
	.SetDisplay("Trading Hour", "Default hour used for order execution", "Schedule");

	_ignoreTradingHour = Param(nameof(IgnoreTradingHour), false)
	.SetDisplay("Ignore Trading Hour", "Execute instructions regardless of the configured hour", "Schedule");

	_ignoreTradeId = Param(nameof(IgnoreTradeId), true)
	.SetDisplay("Ignore Trade Id", "Close all positions even if identifiers do not match", "Risk");

	_maxPositions = Param(nameof(MaxPositions), 1)
	.SetGreaterOrEqual(0)
	.SetDisplay("Max Positions", "Maximum absolute position size", "Risk");

	_removeOrdersFile = Param(nameof(RemoveOrdersFile), true)
	.SetDisplay("Remove Orders File", "Delete the orders file after reading", "Files");

	_useMultiplier = Param(nameof(UseMultiplier), false)
	.SetDisplay("Use Multiplier", "Scale trade size by equity ratio", "Risk");

	_enableDebug = Param(nameof(EnableDebug), false)
	.SetDisplay("Enable Debug", "Write equity values to a debug log", "Files");

	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
	.SetDisplay("Candle Type", "Candle type used for scheduling", "General");
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

	_symbolTradingHours.Clear();
	_instructions.Clear();
	_activeTradeIds.Clear();
	_initialBalance = 0m;
	_debugWriter?.Dispose();
	_debugWriter = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_initialBalance = Portfolio?.CurrentValue ?? 0m;
	Volume = 1m;

	LoadSymbolTradingHours();
	TryLoadOrders();
	PrepareDebugWriter();

	SubscribeCandles(CandleType).Bind(ProcessCandle).Start();

	StartProtection();
}

/// <inheritdoc />
protected override void OnStopped()
{
	base.OnStopped();

	_debugWriter?.Dispose();
	_debugWriter = null;
}

private void ProcessCandle(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
		return;

	TryLoadOrders();
	TryWriteAccountSnapshot(candle.CloseTime);

	var multiplier = GetVolumeMultiplier();
	var candleTime = candle.CloseTime;

	if (EnableDebug)
		{
		var equity = Portfolio?.CurrentValue ?? 0m;
		_debugWriter?.WriteLine(FormattableString.Invariant($"{candleTime:O},{equity:0.####}"));
		_debugWriter?.Flush();
	}

foreach (var instruction in _instructions.Values)
	{
	if (instruction.Executed)
		continue;

	if (!InstructionMatchesSymbol(instruction))
		continue;

	if (!IsInstructionDate(candleTime, instruction))
		continue;

	if (!IgnoreTradingHour && !MatchesTradingHour(candleTime.Hour, instruction.Symbol))
		continue;

	if (instruction.Amount == 0m)
		{
		ProcessCloseInstruction(instruction);
	}
else
{
	ProcessOpenInstruction(instruction, multiplier);
}
}
}

private void ProcessCloseInstruction(TradeInstruction instruction)
{
	if (Position == 0m)
		{
		instruction.Executed = true;
		return;
	}

if (!IgnoreTradeId && !_activeTradeIds.Contains(instruction.Id))
	return;

ClosePosition();
instruction.Executed = true;

if (!IgnoreTradeId)
	_activeTradeIds.Remove(instruction.Id);
}

private void ProcessOpenInstruction(TradeInstruction instruction, decimal multiplier)
{
	if (MaxPositions > 0 && Math.Abs(Position) >= MaxPositions)
		return;

	var volume = Math.Abs(instruction.Amount) * multiplier;
	if (volume <= 0m)
		{
		instruction.Executed = true;
		return;
	}

if (instruction.Amount > 0m)
	BuyMarket(volume);
else
SellMarket(volume);

instruction.Executed = true;

if (!IgnoreTradeId && !string.IsNullOrWhiteSpace(instruction.Id))
	_activeTradeIds.Add(instruction.Id);
}

private void TryWriteAccountSnapshot(DateTimeOffset time)
{
	if (DownloadingHour < 0)
		return;

	if (time.Hour != DownloadingHour)
		return;

	var directory = Path.GetDirectoryName(AccountFile);
	if (!string.IsNullOrEmpty(directory))
		Directory.CreateDirectory(directory);

	var balance = Portfolio?.CurrentValue ?? 0m;
	var equity = Portfolio?.CurrentValue ?? 0m;
	var positions = Position;

	using var writer = new StreamWriter(AccountFile, false);
	writer.WriteLine("Balance,{0:0.####}", balance);
	writer.WriteLine("Equity,{0:0.####}", equity);
	writer.WriteLine("Positions,{0:0.####}", positions);

	TryExportData();
}

private void TryExportData()
{
	if (string.IsNullOrWhiteSpace(DownloadDirectory))
		return;

	Directory.CreateDirectory(DownloadDirectory);

	var filePath = Path.Combine(DownloadDirectory, $"{Security.Id}.csv");
	using var writer = new StreamWriter(filePath, false);
	writer.WriteLine("Timestamp,Open,High,Low,Close");
	writer.WriteLine("# Historical export is not implemented in this sample. This placeholder mirrors the original behavior.");
}

private void LoadSymbolTradingHours()
{
	_symbolTradingHours.Clear();

	if (string.IsNullOrWhiteSpace(SymbolsFile))
		return;

	if (!File.Exists(SymbolsFile))
		return;

	foreach (var line in File.ReadAllLines(SymbolsFile))
		{
		if (string.IsNullOrWhiteSpace(line))
			continue;

		var parts = line.Split(',');
		if (parts.Length < 2)
			continue;

		var symbol = parts[0].Trim();
		if (string.IsNullOrEmpty(symbol))
			continue;

		if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour))
			continue;

		_symbolTradingHours[symbol] = hour;
	}
}

private void TryLoadOrders()
{
	if (string.IsNullOrWhiteSpace(OrdersFile))
		return;

	if (!File.Exists(OrdersFile))
		return;

	using (var reader = new StreamReader(OrdersFile))
		{
		while (!reader.EndOfStream)
			{
			var line = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(line))
				continue;

			var parts = line.Split(',');
			if (parts.Length < 6)
				continue;

			var symbol = parts[0].Trim();
			if (!DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp))
				continue;

			if (!decimal.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
				continue;

			var stopLoss = ParseNullableDecimal(parts[3]);
			var takeProfit = ParseNullableDecimal(parts[4]);
			var id = parts[5].Trim();

			var key = CreateInstructionKey(symbol, timestamp, id);
			if (_instructions.TryGetValue(key, out var existing))
				{
				existing.Symbol = symbol;
				existing.ExecutionTime = timestamp;
				existing.Amount = amount;
				existing.StopLoss = stopLoss;
				existing.TakeProfit = takeProfit;
				existing.Id = id;
				existing.Executed = false;
			}
		else
		{
			_instructions[key] = new TradeInstruction
			{
				Symbol = symbol,
				ExecutionTime = timestamp,
				Amount = amount,
				StopLoss = stopLoss,
				TakeProfit = takeProfit,
				Id = id,
			};
	}
}
}

if (RemoveOrdersFile)
	File.Delete(OrdersFile);
}

private static decimal? ParseNullableDecimal(string value)
{
	var trimmed = value?.Trim();
	if (string.IsNullOrEmpty(trimmed))
		return null;

	return decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
}

private void PrepareDebugWriter()
{
	if (!EnableDebug)
		return;

	var directory = Path.GetDirectoryName(OrdersFile);
	if (!string.IsNullOrEmpty(directory))
		Directory.CreateDirectory(directory);

	var path = Path.Combine(directory ?? string.Empty, "debug_file.txt");
	_debugWriter = new StreamWriter(path, false);
}

private static string CreateInstructionKey(string symbol, DateTime timestamp, string id)
{
	return FormattableString.Invariant($"{symbol}|{timestamp:O}|{id}");
}

private decimal GetVolumeMultiplier()
{
	if (!UseMultiplier)
		return 1m;

	if (_initialBalance <= 0m)
		return 1m;

	var equity = Portfolio?.CurrentValue ?? _initialBalance;
	return equity / _initialBalance;
}

private bool InstructionMatchesSymbol(TradeInstruction instruction)
{
	if (string.IsNullOrEmpty(instruction.Symbol))
		return true;

	return string.Equals(instruction.Symbol, Security.Id, StringComparison.InvariantCultureIgnoreCase);
}

private static bool IsInstructionDate(DateTimeOffset currentTime, TradeInstruction instruction)
{
	return currentTime.Date == instruction.ExecutionTime.Date;
}

private bool MatchesTradingHour(int hour, string symbol)
{
	var overrideHour = TradingHour;
	if (overrideHour >= 0)
		return hour == overrideHour;

	if (_symbolTradingHours.TryGetValue(symbol, out var symbolHour))
		return hour == symbolHour;

	return true;
}

private sealed class TradeInstruction
{
	public string Symbol { get; set; } = string.Empty;
	public DateTime ExecutionTime { get; set; }
	= DateTime.MinValue;
	public decimal Amount { get; set; }
	= 0m;
	public decimal? StopLoss { get; set; }
	= null;
	public decimal? TakeProfit { get; set; }
	= null;
	public string Id { get; set; } = string.Empty;
	public bool Executed { get; set; }
	= false;
}
}
