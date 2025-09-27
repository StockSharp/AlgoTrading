using System;
using System.Collections.Generic;


using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that synchronizes the security of linked strategies whenever the main symbol changes.
/// </summary>
public class SymbolSyncStrategy : Strategy
{
	private readonly StrategyParam<int> _chartLimit;
	private readonly StrategyParam<string> _syncSecurityId;

	private readonly List<Strategy> _linkedStrategies = new();

	private Security _initialSecurity;

	public SymbolSyncStrategy()
	{
		_chartLimit = Param(nameof(ChartLimit), 10)
			.SetNotNegative()
			.SetDisplay("Chart limit", "Maximum number of linked strategies that can be synchronized.", "General")
			.SetCanOptimize(false);

		_syncSecurityId = Param(nameof(SyncSecurityId), string.Empty)
			.SetDisplay("Sync security ID", "Identifier of the security propagated to linked strategies.", "General")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Maximum number of linked strategies that can follow the symbol changes.
	/// </summary>
	public int ChartLimit
	{
		get => _chartLimit.Value;
		set => _chartLimit.Value = value;
	}

	/// <summary>
	/// Identifier of the security that must be mirrored by linked strategies.
	/// </summary>
	public string SyncSecurityId
	{
		get => _syncSecurityId.Value;
		set => _syncSecurityId.Value = value ?? string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialSecurity = Security;

		if (SyncSecurityId.IsEmpty() && Security != null)
			SyncSecurityId = Security.Id;

		SyncSymbols();
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		base.OnStopped(time);

		_initialSecurity = null;
	}

	/// <summary>
	/// Registers an additional strategy that must mirror the current security.
	/// </summary>
	/// <param name="strategy">Strategy that will receive symbol updates.</param>
	/// <returns><c>true</c> when the strategy is registered; otherwise <c>false</c>.</returns>
	public bool RegisterLinkedStrategy(Strategy strategy)
	{
		if (strategy == null)
			throw new ArgumentNullException(nameof(strategy));

		if (_linkedStrategies.Contains(strategy))
			return false;

		var limit = Math.Max(ChartLimit, 0);
		if (_linkedStrategies.Count >= limit)
		{
			LogWarning($"Chart limit of {limit} reached. Strategy '{strategy.Name}' cannot be synchronized.");
			return false;
		}

		_linkedStrategies.Add(strategy);
		ApplySymbol(strategy);
		return true;
	}

	/// <summary>
	/// Removes a strategy from the synchronization list.
	/// </summary>
	/// <param name="strategy">Strategy previously added with <see cref="RegisterLinkedStrategy"/>.</param>
	/// <returns><c>true</c> when the strategy was removed.</returns>
	public bool UnregisterLinkedStrategy(Strategy strategy)
	{
		if (strategy == null)
			throw new ArgumentNullException(nameof(strategy));

		return _linkedStrategies.Remove(strategy);
	}

	/// <summary>
	/// Restores the initial security captured when the strategy started.
	/// </summary>
	public void ResetToInitialSecurity()
	{
		if (_initialSecurity == null)
			return;

		ChangeSyncSecurity(_initialSecurity);
	}

	/// <summary>
	/// Changes the synchronization security using a resolved <see cref="Security"/> instance.
	/// </summary>
	/// <param name="security">Security that should be mirrored by linked strategies.</param>
	/// <returns><c>true</c> when the identifier changed.</returns>
	public bool ChangeSyncSecurity(Security security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (Security != security)
			Security = security;

		if (SyncSecurityId.EqualsIgnoreCase(security.Id))
		{
			SyncSymbols();
			return false;
		}

		SyncSecurityId = security.Id;
		SyncSymbols();
		return true;
	}

	/// <summary>
	/// Changes the synchronization security by resolving the identifier through <see cref="Strategy.SecurityProvider"/>.
	/// </summary>
	/// <param name="securityId">Identifier of the security to use for synchronization.</param>
	/// <returns><c>true</c> when the identifier resolved to a new security.</returns>
	public bool ChangeSyncSecurity(string securityId)
	{
		if (securityId.IsEmpty())
			throw new ArgumentNullException(nameof(securityId));

		if (SecurityProvider != null)
		{
			var resolved = SecurityProvider.LookupById(securityId);
			if (resolved != null)
				return ChangeSyncSecurity(resolved);

			LogWarning($"Security '{securityId}' not found by the security provider.");
		}

		SyncSecurityId = securityId;
		SyncSymbols();
		return false;
	}

	/// <summary>
	/// Synchronizes the security across every registered strategy.
	/// </summary>
	/// <returns><c>true</c> when a security was resolved and propagated.</returns>
	public bool SyncSymbols()
	{
		var security = ResolveSecurity();
		if (security == null)
		{
			LogWarning("No synchronization security resolved. Linked strategies keep their current assignments.");
			return false;
		}

		if (Security != security)
			Security = security;

		foreach (var strategy in _linkedStrategies)
			ApplySymbol(strategy);

		return true;
	}

	private void ApplySymbol(Strategy strategy)
	{
		if (strategy == null)
			return;

		var security = ResolveSecurity();
		if (security == null)
			return;

		if (strategy.Security == security)
			return;

		strategy.Security = security;
	}

	private Security ResolveSecurity()
	{
		if (Security != null && (SyncSecurityId.IsEmpty() || SyncSecurityId.EqualsIgnoreCase(Security.Id)))
			return Security;

		if (!SyncSecurityId.IsEmpty() && SecurityProvider != null)
		{
			var resolved = SecurityProvider.LookupById(SyncSecurityId);
			if (resolved != null)
				return resolved;
		}

		return Security;
	}
}
