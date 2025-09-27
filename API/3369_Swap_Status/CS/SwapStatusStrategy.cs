using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that displays whether the overnight swap is positive, negative, or zero for selected currency pairs.
/// The script mirrors the MetaTrader expert by monitoring multiple symbols and reporting status changes only when necessary.
/// </summary>
public class SwapStatusStrategy : Strategy
{
	private readonly StrategyParam<SwapPreset> _preset;
	private readonly StrategyParam<string> _customSymbols;

	private readonly List<Security> _monitoredSecurities = new();
	private readonly Dictionary<Security, SwapSnapshot> _swapBySecurity = new();

	private bool _needsResolution = true;

	/// <summary>
	/// Determines which predefined group of currency pairs should be monitored.
	/// </summary>
	public SwapPreset Preset
	{
		get => _preset.Value;
		set
		{
			_preset.Value = value;
			_needsResolution = true;
		}
	}

	/// <summary>
	/// Additional symbols to monitor, provided as comma-separated identifiers.
	/// </summary>
	public string CustomSymbols
	{
		get => _customSymbols.Value;
		set
		{
			_customSymbols.Value = value;
			_needsResolution = true;
		}
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SwapStatusStrategy()
	{
		_preset = Param(nameof(Preset), SwapPreset.PrimarySymbol)
			.SetDisplay("Preset", "Predefined group of symbols to inspect", "General");

		_customSymbols = Param(nameof(CustomSymbols), string.Empty)
			.SetDisplay("Custom symbols", "Optional comma-separated list of additional securities", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		EnsureSecuritiesResolved();

		foreach (var security in _monitoredSecurities)
			yield return (security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_monitoredSecurities.Clear();
		_swapBySecurity.Clear();
		_needsResolution = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		EnsureSecuritiesResolved();

		if (_monitoredSecurities.Count == 0)
			throw new InvalidOperationException("No securities resolved for swap monitoring.");

		foreach (var security in _monitoredSecurities)
		{
			// Subscribe to Level1 updates to receive swap values for every watched security.
			SubscribeLevel1(security)
				.Bind(message => ProcessLevel1(security, message))
				.Start();
		}
	}

	private void ProcessLevel1(Security security, Level1ChangeMessage message)
	{
		var snapshot = _swapBySecurity.TryGetValue(security, out var existing)
			? existing
			: _swapBySecurity[security] = new SwapSnapshot();

		var updated = false;

		if (message.IsContainsField(Level1Fields.SwapSell))
		{
			snapshot.ShortSwap = message.TryGetDecimal(Level1Fields.SwapSell);
			updated = true;
		}

		if (message.IsContainsField(Level1Fields.SwapBuy))
		{
			snapshot.LongSwap = message.TryGetDecimal(Level1Fields.SwapBuy);
			updated = true;
		}

		if (!updated)
			return;

		if (snapshot.ShortSwap is null || snapshot.LongSwap is null)
			return;

		var shortStatus = DescribeSwap(snapshot.ShortSwap.Value);
		var longStatus = DescribeSwap(snapshot.LongSwap.Value);

		if (snapshot.LastShortStatus == shortStatus && snapshot.LastLongStatus == longStatus)
			return;

		snapshot.LastShortStatus = shortStatus;
		snapshot.LastLongStatus = longStatus;

		var shortValue = snapshot.ShortSwap.Value.ToString(CultureInfo.InvariantCulture);
		var longValue = snapshot.LongSwap.Value.ToString(CultureInfo.InvariantCulture);

		// Report the qualitative status together with the raw swap numbers.
		LogInfo($"Swap status for {security.Id}: Short {shortStatus} ({shortValue}), Long {longStatus} ({longValue})");
	}

	private void EnsureSecuritiesResolved()
	{
		if (!_needsResolution)
			return;

		_monitoredSecurities.Clear();
		_swapBySecurity.Clear();

		void AddSecurity(Security security)
		{
			if (security == null)
				return;

			if (_monitoredSecurities.Contains(security))
				return;

			_monitoredSecurities.Add(security);
		}

		if (Preset == SwapPreset.PrimarySymbol && Security != null)
		{
			// The original EA defaults to the current chart symbol, so include Strategy.Security.
			AddSecurity(Security);
		}

		var provider = SecurityProvider;
		if (provider == null)
		{
			_needsResolution = false;
			return;
		}

		foreach (var symbol in GetPresetSymbols(Preset))
		{
			var security = provider.LookupById(symbol);
			if (security != null)
			{
				AddSecurity(security);
			}
			else
			{
				// Inform the user when a predefined ticker is missing from the provider.
				LogWarning($"Security '{symbol}' is not available in the security provider.");
			}
		}

		var customList = CustomSymbols;
		if (!customList.IsEmptyOrWhiteSpace())
		{
			var separators = new[] { ',', ';', '\n', '\r', '\t', ' ' };
			var tokens = customList.Split(separators, StringSplitOptions.RemoveEmptyEntries);

			foreach (var token in tokens)
			{
				var symbol = token.Trim();
				if (symbol.Length == 0)
					continue;

				var security = provider.LookupById(symbol);
				if (security != null)
				{
					AddSecurity(security);
				}
				else
				{
					LogWarning($"Security '{symbol}' is not available in the security provider.");
				}
			}
		}

		_needsResolution = false;
	}

	private static string DescribeSwap(decimal value)
	{
		if (value > 0m)
			return "Positive";

		if (value < 0m)
			return "Negative";

		return "Zero";
	}

	private static IEnumerable<string> GetPresetSymbols(SwapPreset preset)
	{
		return preset switch
		{
			SwapPreset.MajorPairs => new[]
			{
				"EURUSD",
				"GBPUSD",
				"AUDUSD",
				"NZDUSD",
				"USDCAD",
				"USDCHF",
				"USDJPY"
			},
			SwapPreset.MinorPairs => new[]
			{
				"EURGBP",
				"EURJPY",
				"GBPJPY",
				"EURCHF",
				"GBPCHF",
				"AUDJPY",
				"NZDJPY"
			},
			SwapPreset.ExoticPairs => new[]
			{
				"EURTRY",
				"EURZAR",
				"USDTRY",
				"USDZAR"
			},
			_ => Array.Empty<string>()
		};
	}

	/// <summary>
	/// Keeps the latest swap values together with previously reported statuses.
	/// </summary>
	private sealed class SwapSnapshot
	{
		public decimal? ShortSwap;
		public decimal? LongSwap;
		public string LastShortStatus;
		public string LastLongStatus;
	}
}

/// <summary>
/// Lists available symbol presets that mirror the MetaTrader scripts.
/// </summary>
public enum SwapPreset
{
	/// <summary>
	/// Only monitors the primary strategy security (matches Swap.mq4 behaviour).
	/// </summary>
	PrimarySymbol,

	/// <summary>
	/// Uses the MetaTrader major pairs watch list.
	/// </summary>
	MajorPairs,

	/// <summary>
	/// Uses the MetaTrader minor pairs watch list.
	/// </summary>
	MinorPairs,

	/// <summary>
	/// Uses the MetaTrader exotic pairs watch list.
	/// </summary>
	ExoticPairs
}
