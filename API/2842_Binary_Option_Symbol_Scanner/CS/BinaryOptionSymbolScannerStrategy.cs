namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that scans provided securities and reports binary option symbols based on metadata filters.
/// </summary>
public class BinaryOptionSymbolScannerStrategy : Strategy
{
	private readonly StrategyParam<string> _symbols;
	private readonly StrategyParam<decimal> _profitCalcMode;
	private readonly StrategyParam<decimal> _stopLevel;

	/// <summary>
	/// Initializes a new instance of <see cref="BinaryOptionSymbolScannerStrategy"/>.
	/// </summary>
	public BinaryOptionSymbolScannerStrategy()
	{
		_symbols = Param(nameof(Symbols), string.Empty)
			.SetDisplay("Symbols", "Comma or semicolon separated identifiers to inspect", "General");

		_profitCalcMode = Param(nameof(ProfitCalcMode), 2m)
			.SetDisplay("Profit Calc Mode", "Required profit calculation mode", "Filters");

		_stopLevel = Param(nameof(StopLevel), 0m)
			.SetDisplay("Stop Level", "Required stop level", "Filters");
	}

	/// <summary>
	/// Symbols to scan for binary option metadata.
	/// </summary>
	public string Symbols
	{
		get => _symbols.Value;
		set => _symbols.Value = value;
	}

	/// <summary>
	/// Profit calculation mode that indicates binary options.
	/// </summary>
	public decimal ProfitCalcMode
	{
		get => _profitCalcMode.Value;
		set => _profitCalcMode.Value = value;
	}

	/// <summary>
	/// Required stop level for binary options.
	/// </summary>
	public decimal StopLevel
	{
		get => _stopLevel.Value;
		set => _stopLevel.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield break;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Parse identifiers before performing metadata lookup.
		var securityIds = ParseSymbols(Symbols).ToArray();

		if (securityIds.Length == 0)
		{
			// Fallback to the main strategy security if no list is provided.
			if (Security is null)
			{
				AddWarningLog("No symbols supplied and main Security is not set.");
				return;
			}

			InspectSecurity(Security);
			return;
		}

		foreach (var securityId in securityIds)
		{
			var security = SecurityProvider.LookupById(securityId);
			if (security is null)
			{
				AddWarningLog("Security '{0}' not found in provider.", securityId);
				continue;
			}

			InspectSecurity(security);
		}
	}

	private static IEnumerable<string> ParseSymbols(string symbols)
	{
		if (string.IsNullOrWhiteSpace(symbols))
			yield break;

		var separators = new[] { ',', ';', '\n', '\r', '\t', ' ' };
		foreach (var token in symbols.Split(separators, StringSplitOptions.RemoveEmptyEntries))
			yield return token.Trim();
	}

	private void InspectSecurity(Security security)
	{
		// Read metadata from the security extension info dictionary.
		if (!TryGetDecimal(security, "ProfitCalcMode", out var profitCalcMode))
		{
			AddDebugLog("Security '{0}' is missing ProfitCalcMode metadata.", security.Id);
			return;
		}

		if (!TryGetDecimal(security, "StopLevel", out var stopLevel))
		{
			AddDebugLog("Security '{0}' is missing StopLevel metadata.", security.Id);
			return;
		}

		// Apply filters matching the MQL strategy criteria.
		if (profitCalcMode != ProfitCalcMode || stopLevel != StopLevel)
			return;

		AddInfoLog("Binary option symbol detected: {0}. ProfitCalcMode={1}, StopLevel={2}", security.Id, profitCalcMode, stopLevel);
	}

	private static bool TryGetDecimal(Security security, string key, out decimal value)
	{
		value = default;

		var info = security.ExtensionInfo;
		if (info is null)
			return false;

		if (!info.TryGetValue(key, out var raw))
			return false;

		return TryConvertToDecimal(raw, out value);
	}

	private static bool TryConvertToDecimal(object value, out decimal result)
	{
		switch (value)
		{
			case decimal dec:
				result = dec;
				return true;
			case double dbl:
				result = (decimal)dbl;
				return true;
			case float fl:
				result = (decimal)fl;
				return true;
			case int i:
				result = i;
				return true;
			case long l:
				result = l;
				return true;
			case string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
				result = parsed;
				return true;
			default:
				result = default;
				return false;
		}
	}
}
