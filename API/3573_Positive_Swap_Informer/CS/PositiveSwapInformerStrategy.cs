using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that scans a configurable list of securities and reports those with a positive swap.
/// It mirrors the MetaTrader "Swap Informer" script by producing periodic log entries containing the findings.
/// </summary>
public class PositiveSwapInformerStrategy : Strategy
{
	private static readonly HashSet<string> _longSwapKeys = new(new[]
	{
		"SwapBuy",
		"SwapLong",
		"SwapBuyPoints",
		"SwapLongPoints",
	}, StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> _shortSwapKeys = new(new[]
	{
		"SwapSell",
		"SwapShort",
		"SwapSellPoints",
		"SwapShortPoints",
	}, StringComparer.OrdinalIgnoreCase);

	private readonly StrategyParam<string> _symbolsList;
	private readonly StrategyParam<TimeSpan> _refreshInterval;
	private readonly StrategyParam<bool> _includePrimarySecurity;

	private readonly List<Security> _watchedSecurities = new();

	private readonly object _sync = new();

	private Timer? _timer;
	private bool _needsResolve = true;

	/// <summary>
	/// Comma separated list of securities that should be inspected for a positive swap.
	/// </summary>
	public string SymbolsList
	{
		get => _symbolsList.Value;
		set
		{
			_symbolsList.Value = value;
			_needsResolve = true;
		}
	}

	/// <summary>
	/// Interval that controls how often the strategy recomputes the swap report.
	/// </summary>
	public TimeSpan RefreshInterval
	{
		get => _refreshInterval.Value;
		set => _refreshInterval.Value = value;
	}

	/// <summary>
	/// When enabled, the primary <see cref="Strategy.Security"/> is automatically included in the scan.
	/// </summary>
	public bool IncludePrimarySecurity
	{
		get => _includePrimarySecurity.Value;
		set
		{
			_includePrimarySecurity.Value = value;
			_needsResolve = true;
		}
	}

	/// <summary>
	/// Initializes strategy parameters with descriptive UI metadata.
	/// </summary>
	public PositiveSwapInformerStrategy()
	{
		_symbolsList = Param(nameof(SymbolsList), string.Empty)
			.SetDisplay("Extra symbols", "Comma separated list of security identifiers to inspect in addition to the primary symbol.", "General");

		_refreshInterval = Param(nameof(RefreshInterval), TimeSpan.FromSeconds(10))
			.SetDisplay("Refresh interval", "How often the strategy recalculates the positive swap report.", "General");

		_includePrimarySecurity = Param(nameof(IncludePrimarySecurity), true)
			.SetDisplay("Include primary symbol", "Automatically add the strategy security to the scan list when it is assigned.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		EnsureWatchListResolved();

		foreach (var security in _watchedSecurities)
			yield return (security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		lock (_sync)
		{
			_timer?.Dispose();
			_timer = null;
			_watchedSecurities.Clear();
			_needsResolve = true;
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		EnsureWatchListResolved();

		if (_watchedSecurities.Count == 0)
			throw new InvalidOperationException("No securities specified for swap scanning.");

		foreach (var security in _watchedSecurities)
		{
			// Request level1 data so that swap values become available via extension info when supported by the connector.
			SubscribeLevel1(security).Start();
		}

		StartTimer();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		StopTimer();
	}

	private void StartTimer()
	{
		lock (_sync)
		{
			_timer?.Dispose();

			var interval = RefreshInterval;
			if (interval <= TimeSpan.Zero)
			{
				LogWarning("Refresh interval must be positive. Falling back to one second.");
				interval = TimeSpan.FromSeconds(1);
			}

			_timer = new Timer(OnTimer, null, TimeSpan.Zero, interval);
		}
	}

	private void StopTimer()
	{
		lock (_sync)
		{
			var timer = _timer;
			_timer = null;
			timer?.Dispose();
		}
	}

	private void EnsureWatchListResolved()
	{
		if (!_needsResolve)
			return;

		lock (_sync)
		{
			_watchedSecurities.Clear();

			void AddSecurity(Security sec)
			{
				if (sec == null)
					return;

				if (_watchedSecurities.Contains(sec))
					return;

				_watchedSecurities.Add(sec);
			}

			if (IncludePrimarySecurity)
			{
				AddSecurity(Security);
			}

			if (!string.IsNullOrWhiteSpace(SymbolsList))
			{
				var provider = SecurityProvider;
				if (provider == null)
				{
					LogWarning("Security provider is not assigned. Only the primary security will be scanned.");
				}
				else
				{
					var separators = new[] { ',', ';', '\n', '\r', '\t', ' ' };
					var tokens = SymbolsList.Split(separators, StringSplitOptions.RemoveEmptyEntries);

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
							LogWarning($"Security '{symbol}' not found in the security provider.");
						}
					}
				}
			}

			_needsResolve = false;
		}
	}

	private void OnTimer(object state)
	{
		Security[] securities;
		lock (_sync)
		{
			securities = _watchedSecurities.ToArray();
		}

		if (securities.Length == 0)
		{
			LogWarning("Swap informer timer ticked without configured securities.");
			return;
		}

		var builder = new StringBuilder();

		foreach (var security in securities)
		{
			var (longSwap, shortSwap) = ResolveSwapValues(security);

			if (longSwap.HasValue && longSwap.Value > 0)
			{
				builder.Append(security.Id);
				builder.Append(':');
				builder.Append(' ');
				builder.Append("Swap Long (Buy) = ");
				builder.AppendLine(longSwap.Value.ToString("0.########", CultureInfo.InvariantCulture));
			}

			if (shortSwap.HasValue && shortSwap.Value > 0)
			{
				builder.Append(security.Id);
				builder.Append(':');
				builder.Append(' ');
				builder.Append("Swap Short (Sell) = ");
				builder.AppendLine(shortSwap.Value.ToString("0.########", CultureInfo.InvariantCulture));
			}
		}

		if (builder.Length == 0)
		{
			LogInfo("Positive swap report: no symbols with a positive swap were found.");
		}
		else
		{
			builder.Insert(0, "Positive swap report:" + Environment.NewLine);
			LogInfo(builder.ToString());
		}
	}

	private static (decimal? longSwap, decimal? shortSwap) ResolveSwapValues(Security security)
	{
		var extensionInfo = security.ExtensionInfo;
		var longSwap = TryExtractFromExtensionInfo(extensionInfo, _longSwapKeys);
		var shortSwap = TryExtractFromExtensionInfo(extensionInfo, _shortSwapKeys);

		return (longSwap, shortSwap);
	}

	private static decimal? TryExtractFromExtensionInfo(IDictionary<object, object> extensionInfo, HashSet<string> aliases)
	{
		if (extensionInfo == null)
			return null;

		foreach (var pair in extensionInfo)
		{
			var keyText = Convert.ToString(pair.Key, CultureInfo.InvariantCulture);
			if (keyText == null)
				continue;

			if (!aliases.Contains(keyText))
				continue;

			if (TryConvertToDecimal(pair.Value, out var value))
				return value;
		}

		return null;
	}

	private static bool TryConvertToDecimal(object value, out decimal result)
	{
		switch (value)
		{
			case null:
				result = default;
				return false;
			case decimal dec:
				result = dec;
				return true;
			case double dbl:
				result = (decimal)dbl;
				return true;
			case float fl:
				result = (decimal)fl;
				return true;
			case int intValue:
				result = intValue;
				return true;
			case long longValue:
				result = longValue;
				return true;
			case string text when decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
				result = parsed;
				return true;
			default:
				result = default;
				return false;
		}
	}
}
