using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that logs every new Level1 update for a configurable Market Watch list.
/// It replicates the MetaTrader script by printing the bid and spread for each observed symbol.
/// </summary>
public class OnTickMarketWatchStrategy : Strategy
{
	private readonly StrategyParam<string> _symbolsList;

	private readonly List<Security> _watchedSecurities = new();
	private readonly Dictionary<Security, int> _securityIndexes = new();

	private bool _watchListNeedsResolve = true;

	/// <summary>
	/// Additional securities to monitor, provided as a comma-separated list of security identifiers.
	/// </summary>
	public string SymbolsList
	{
		get => _symbolsList.Value;
		set
		{
			_symbolsList.Value = value;
			_watchListNeedsResolve = true;
		}
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public OnTickMarketWatchStrategy()
	{
		_symbolsList = Param(nameof(SymbolsList), string.Empty)
			.SetDisplay("Additional symbols", "Comma-separated list of extra securities to monitor", "Data");
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

		_watchedSecurities.Clear();
		_securityIndexes.Clear();
		_watchListNeedsResolve = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		EnsureWatchListResolved();

		if (_watchedSecurities.Count == 0)
			throw new InvalidOperationException("No securities specified for monitoring.");

		foreach (var security in _watchedSecurities)
		{
			// Subscribe to Level1 updates for each monitored security.
			SubscribeLevel1(security)
				.Bind(message => ProcessLevel1(security, message))
				.Start();
		}
	}

	private void ProcessLevel1(Security security, Level1ChangeMessage message)
	{
		// Skip updates that do not contain any meaningful price change.
		if (!message.IsContainsField(Level1Fields.LastTradePrice) &&
			!message.IsContainsField(Level1Fields.BestBidPrice) &&
			!message.IsContainsField(Level1Fields.BestAskPrice))
		{
			return;
		}

		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice) ??
			message.TryGetDecimal(Level1Fields.LastTradePrice);
		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);
		var spread = bid.HasValue && ask.HasValue ? ask.Value - bid.Value : (decimal?)null;

		if (!bid.HasValue)
			return;

		var index = _securityIndexes.TryGetValue(security, out var idx) ? idx : -1;
		var spreadText = spread?.ToString(CultureInfo.InvariantCulture) ?? "n/a";
		var bidText = bid.Value.ToString(CultureInfo.InvariantCulture);

		// Log the new tick information to mimic the MetaTrader script behaviour.
		LogInfo($"New tick on the symbol {security.Id} index in the list={index} bid={bidText} spread={spreadText}");
	}

	private void EnsureWatchListResolved()
	{
		if (!_watchListNeedsResolve)
			return;

		_watchedSecurities.Clear();
		_securityIndexes.Clear();

		var nextIndex = 0;

		void AddSecurity(Security sec)
		{
			if (sec == null)
				return;

			if (_securityIndexes.ContainsKey(sec))
				return;

			_watchedSecurities.Add(sec);
			_securityIndexes.Add(sec, nextIndex++);
		}

		// Always include the primary strategy security if it is set.
		AddSecurity(Security);

		if (string.IsNullOrWhiteSpace(SymbolsList))
		{
			_watchListNeedsResolve = false;
			return;
		}

		var provider = SecurityProvider;
		if (provider == null)
			return;

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
				// Store the resolved security together with its Market Watch index.
				AddSecurity(security);
			}
			else
			{
				LogWarning($"Security '{symbol}' not found in the security provider.");
			}
		}

		_watchListNeedsResolve = false;
	}
}
