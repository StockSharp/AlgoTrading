using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Replicates the MetaTrader "Market Watch Panel" utility.
/// Maintains a watch list loaded from a text file, subscribes to Level1 updates, and logs live prices for every tracked symbol.
/// Provides runtime management helpers mirroring the original script buttons: add, reload, and clear the stored symbols.
/// </summary>
public class MarketWatchPanelStrategy : Strategy
{
	private readonly StrategyParam<string> _symbolsFileName;
	private readonly StrategyParam<bool> _includePrimarySecurity;

	private readonly Dictionary<string, WatchEntry> _entries = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly List<string> _persistentOrder = new();
	private readonly HashSet<string> _persistentSet = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MarketWatchPanelStrategy()
	{
		_symbolsFileName = Param(nameof(SymbolsFileName), "symbols.txt")
			.SetDisplay("Symbols file", "Relative or absolute path to the Market Watch list.", "Data")
			.SetCanOptimize(false);

		_includePrimarySecurity = Param(nameof(IncludePrimarySecurity), true)
			.SetDisplay("Include main security", "Also monitor the primary strategy instrument.", "Data")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Path to the file containing one symbol per line for the Market Watch list.
	/// </summary>
	public string SymbolsFileName
	{
		get => _symbolsFileName.Value;
		set => _symbolsFileName.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Adds the main strategy security to the Market Watch list without persisting it to the file.
	/// </summary>
	public bool IncludePrimarySecurity
	{
		get => _includePrimarySecurity.Value;
		set => _includePrimarySecurity.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var entry in _entries.Values)
		{
			if (entry.Security != null)
				yield return (entry.Security, DataType.Level1);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetEntries();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		ResetEntries();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetEntries();
		LoadSymbolsFromFile();

		foreach (var code in _persistentOrder.ToArray())
		{
			ActivateSymbol(code, true);
		}

		if (IncludePrimarySecurity && Security != null)
		{
			ActivateSymbol(Security.Id, false);
		}

		if (_entries.Count == 0)
		{
			LogWarning("Market Watch list is empty. No symbols will be monitored.");
		}
	}

	/// <summary>
	/// Adds a new symbol to the Market Watch list and persists it to the storage file.
	/// </summary>
	/// <param name="symbolCode">Security identifier compatible with the security provider.</param>
	public void AddSymbol(string symbolCode)
	{
		var shouldPersist = ActivateSymbol(symbolCode, true);

		if (shouldPersist)
		{
			PersistSymbols();
		}
	}

	/// <summary>
	/// Reloads the Market Watch list from the storage file, replacing all persistent entries.
	/// </summary>
	public void ReloadSymbols()
	{
		foreach (var pair in _entries.ToArray())
		{
			if (!pair.Value.Persistent)
				continue;

			DeactivateEntry(pair.Key, pair.Value);
			_entries.Remove(pair.Key);
		}

		_persistentOrder.Clear();
		_persistentSet.Clear();

		LoadSymbolsFromFile();

		foreach (var code in _persistentOrder.ToArray())
		{
			ActivateSymbol(code, true);
		}
	}

	/// <summary>
	/// Clears the stored Market Watch list and unsubscribes from all persistent symbols.
	/// The primary security remains active if the corresponding option is enabled.
	/// </summary>
	public void ClearSymbols()
	{
		foreach (var pair in _entries.ToArray())
		{
			if (!pair.Value.Persistent)
				continue;

			DeactivateEntry(pair.Key, pair.Value);
			_entries.Remove(pair.Key);
		}

		_persistentOrder.Clear();
		_persistentSet.Clear();

		PersistSymbols();
	}

	private void ResetEntries()
	{
		foreach (var entry in _entries.Values)
		{
			DeactivateEntry(null, entry);
		}

		_entries.Clear();
		_persistentOrder.Clear();
		_persistentSet.Clear();
	}

	private void DeactivateEntry(string code, WatchEntry entry)
	{
		if (entry.Subscription != null)
		{
			// Stop Level1 subscription when the entry is removed or the strategy resets.
			entry.Subscription.Stop();
			entry.Subscription = null;
		}

		if (code != null && entry.Persistent)
		{
			_persistentSet.Remove(code);
			_persistentOrder.RemoveAll(x => string.Equals(x, code, StringComparison.InvariantCultureIgnoreCase));
		}
	}

	private void LoadSymbolsFromFile()
	{
		var path = ResolveSymbolsFilePath();

		if (!File.Exists(path))
		{
			LogInfo($"Symbols file '{path}' not found. Starting with an empty watch list.");
			return;
		}

		try
		{
			foreach (var rawLine in File.ReadAllLines(path))
			{
				if (string.IsNullOrWhiteSpace(rawLine))
					continue;

				var symbol = rawLine.Trim();

				if (!_persistentSet.Add(symbol))
					continue;

				_persistentOrder.Add(symbol);
				_entries[symbol] = new WatchEntry { Persistent = true };
			}
		}
		catch (Exception ex)
		{
			LogError($"Failed to load Market Watch symbols from '{path}': {ex.Message}");
		}
	}

	private bool ActivateSymbol(string symbolCode, bool persistent)
	{
		if (string.IsNullOrWhiteSpace(symbolCode))
		{
			LogWarning("Attempted to add an empty symbol to the Market Watch list.");
			return false;
		}

		var normalized = symbolCode.Trim();

		if (!_entries.TryGetValue(normalized, out var entry))
		{
			entry = new WatchEntry();
			_entries.Add(normalized, entry);
		}

		var addedToPersistent = false;

		if (persistent)
		{
			if (!entry.Persistent)
			{
				entry.Persistent = true;
			}

			if (_persistentSet.Add(normalized))
			{
				_persistentOrder.Add(normalized);
				addedToPersistent = true;
			}
		}

		var provider = SecurityProvider;
		if (provider == null)
		{
			LogError("Security provider is not assigned. Unable to resolve Market Watch symbols.");
			return addedToPersistent;
		}

		var security = entry.Security;
		if (security == null)
		{
			security = provider.LookupById(normalized);
			if (security == null)
			{
				LogWarning($"Security '{normalized}' not found in the security provider.");
				return addedToPersistent;
			}

			entry.Security = security;
		}

		if (entry.Subscription == null)
		{
			// Subscribe to Level1 updates to mirror the price refreshing logic of the original panel.
			var subscription = SubscribeLevel1(security);
			subscription.Bind(message => ProcessLevel1(security, message)).Start();
			entry.Subscription = subscription;
			LogInfo($"Subscribed to Level1 updates for '{normalized}'.");
		}

		return addedToPersistent;
	}

	private void PersistSymbols()
	{
		var path = ResolveSymbolsFilePath();

		try
		{
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			File.WriteAllLines(path, _persistentOrder);
			LogInfo($"Saved {_persistentOrder.Count} Market Watch symbols to '{path}'.");
		}
		catch (Exception ex)
		{
			LogError($"Failed to save Market Watch symbols to '{path}': {ex.Message}");
		}
	}

	private string ResolveSymbolsFilePath()
	{
		var fileName = SymbolsFileName;
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new InvalidOperationException("Symbols file name is not specified.");
		}

		if (Path.IsPathRooted(fileName))
		{
			return fileName;
		}

		var basePath = Environment.CurrentDirectory;
		return Path.Combine(basePath, fileName);
	}

	private void ProcessLevel1(Security security, Level1ChangeMessage message)
	{
		// Extract the freshest available price to emulate the panel text refresh.
		var price = message.TryGetDecimal(Level1Fields.LastTradePrice)
			?? message.TryGetDecimal(Level1Fields.BestBidPrice)
			?? message.TryGetDecimal(Level1Fields.BestAskPrice);

		if (!price.HasValue)
		{
			return;
		}

		LogInfo($"Market Watch update: {security.Id} price={price.Value.ToString(CultureInfo.InvariantCulture)}");
	}

	private sealed class WatchEntry
	{
		public bool Persistent { get; set; }
		public Security? Security { get; set; }
		public ISubscription? Subscription { get; set; }
	}
}
