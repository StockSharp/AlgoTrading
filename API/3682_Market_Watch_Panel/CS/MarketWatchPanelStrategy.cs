using System;
using System.Collections.Generic;
using System.IO;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the Market Watch panel from MetaTrader 5.
/// Loads a list of symbols from a text file, subscribes to their level1
/// streams, and logs live price updates with optional file persistence.
/// </summary>
public class MarketWatchPanelStrategy : Strategy
{
	private readonly StrategyParam<string> _symbolsFile;
	private readonly StrategyParam<string> _priceLogFile;
	private readonly StrategyParam<bool> _enablePriceLogging;

	private readonly List<string> _symbols = new();
	private readonly Dictionary<string, MarketDataSubscription> _subscriptions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<Security, decimal> _lastPrices = new();
	private readonly Dictionary<Security, decimal> _previouslyLoggedPrices = new();

	private bool _symbolsLoaded;

	/// <summary>
	/// Path to the text file containing instrument identifiers (one per line).
	/// </summary>
	public string SymbolsFile
	{
		get => _symbolsFile.Value;
		set => _symbolsFile.Value = value;
	}

	/// <summary>
	/// Path to the log file where price snapshots will be appended.
	/// </summary>
	public string PriceLogFile
	{
		get => _priceLogFile.Value;
		set => _priceLogFile.Value = value;
	}

	/// <summary>
	/// Enables writing price updates to <see cref="PriceLogFile"/>.
	/// </summary>
	public bool EnablePriceLogging
	{
		get => _enablePriceLogging.Value;
		set => _enablePriceLogging.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MarketWatchPanelStrategy"/>.
	/// </summary>
	public MarketWatchPanelStrategy()
	{
		_symbolsFile = Param(nameof(SymbolsFile), "symbols.txt")
			.SetDisplay("Symbols File", "Text file with instrument identifiers", "General");

		_priceLogFile = Param(nameof(PriceLogFile), "symbols_prices.log")
			.SetDisplay("Price Log File", "Destination file for streaming price updates", "General");

		_enablePriceLogging = Param(nameof(EnablePriceLogging), false)
			.SetDisplay("Enable Logging", "Write last trade prices to the log file", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		EnsureSymbolsLoaded();

		foreach (var symbol in _symbols)
		{
			Security security;

			try
			{
				security = this.GetSecurity(symbol);
			}
			catch (Exception error)
			{
				LogError(error);
				continue;
			}

			yield return (security, DataType.Level1);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_symbolsLoaded = false;
		_symbols.Clear();
		_subscriptions.Clear();
		_lastPrices.Clear();
		_previouslyLoggedPrices.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		EnsureSymbolsLoaded();

		if (_symbols.Count == 0)
		{
			LogInfo("No symbols were loaded. Add instruments to the symbols file to start monitoring.");
			return;
		}

		foreach (var symbol in _symbols)
		{
			StartSubscription(symbol);
		}
	}

	/// <summary>
	/// Adds a new symbol to the watch list and persists it on disk.
	/// </summary>
	/// <param name="symbol">Instrument identifier.</param>
	public void AddSymbol(string symbol)
	{
		EnsureSymbolsLoaded();

		if (string.IsNullOrWhiteSpace(symbol))
		{
			LogWarning("Cannot add an empty symbol.");
			return;
		}

		symbol = symbol.Trim();

		foreach (var existing in _symbols)
		{
			if (string.Equals(existing, symbol, StringComparison.InvariantCultureIgnoreCase))
			{
				LogInfo($"Symbol {symbol} is already tracked.");
				return;
			}
		}

		_symbols.Add(symbol);
		SaveSymbolsToFile();

		if (State == StrategyStates.Started)
		{
			StartSubscription(symbol);
		}
	}

	/// <summary>
	/// Clears the watch list, stops subscriptions, and truncates the symbols file.
	/// </summary>
	public void ClearSymbols()
	{
		EnsureSymbolsLoaded();

		foreach (var pair in _subscriptions)
		{
			try
			{
				pair.Value.Dispose();
			}
			catch (Exception error)
			{
				LogError(error);
			}
		}

		_subscriptions.Clear();
		_symbols.Clear();
		_lastPrices.Clear();
		_previouslyLoggedPrices.Clear();

		SaveSymbolsToFile();
	}

	private void StartSubscription(string symbol)
	{
		if (_subscriptions.ContainsKey(symbol))
			return;

		try
		{
			var security = this.GetSecurity(symbol);

			var subscription = SubscribeLevel1(security);
			subscription.Bind(ProcessLevel1).Start();

			_subscriptions[symbol] = subscription;

			LogInfo($"Started level1 subscription for {symbol}.");
		}
		catch (Exception error)
		{
			LogError(error);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var security = this.GetSecurity(level1.SecurityId);

		if (!level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var value) &&
			!level1.Changes.TryGetValue(Level1Fields.ClosePrice, out value))
		{
			return;
		}

		var lastPrice = (decimal)value;
		_lastPrices[security] = lastPrice;

		// Provide live status messages for every processed update.
		LogInfo($"{security.Id} last price: {lastPrice}");

		if (!EnablePriceLogging)
			return;

		if (_previouslyLoggedPrices.TryGetValue(security, out var previous) && previous == lastPrice)
			return;

		_previouslyLoggedPrices[security] = lastPrice;

		try
		{
			// Append a snapshot with ISO timestamp and the latest price.
			using var stream = new FileStream(PriceLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
			using var writer = new StreamWriter(stream);
			writer.WriteLine($"{DateTimeOffset.Now:O};{security.Id};{lastPrice}");
		}
		catch (Exception error)
		{
			LogError(error);
		}
	}

	private void EnsureSymbolsLoaded()
	{
		if (_symbolsLoaded)
			return;

		_symbolsLoaded = true;

		var fileName = SymbolsFile;

		if (string.IsNullOrWhiteSpace(fileName))
		{
			LogWarning("Symbols file name is not specified.");
			return;
		}

		if (!File.Exists(fileName))
		{
			LogInfo($"Symbols file '{fileName}' not found. Create the file and add symbols (one per line).");
			return;
		}

		try
		{
			foreach (var line in File.ReadLines(fileName))
			{
				var symbol = line.Trim();

				if (string.IsNullOrEmpty(symbol))
					continue;

				var duplicate = false;

				foreach (var existing in _symbols)
				{
					if (string.Equals(existing, symbol, StringComparison.InvariantCultureIgnoreCase))
					{
						duplicate = true;
						break;
					}
				}

				if (!duplicate)
					_symbols.Add(symbol);
			}
		}
		catch (Exception error)
		{
			LogError(error);
		}
	}

	private void SaveSymbolsToFile()
	{
		var fileName = SymbolsFile;

		if (string.IsNullOrWhiteSpace(fileName))
		{
			LogWarning("Cannot save symbols because the file name is empty.");
			return;
		}

		try
		{
			// Rewrite the file so it reflects the current watch list state.
			using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			using var writer = new StreamWriter(stream);

			foreach (var symbol in _symbols)
			{
				writer.WriteLine(symbol);
			}
		}
		catch (Exception error)
		{
			LogError(error);
		}
	}
}
