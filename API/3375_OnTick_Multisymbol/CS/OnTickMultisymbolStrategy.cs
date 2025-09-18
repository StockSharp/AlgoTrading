namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Logs trade ticks for multiple symbols similar to the MetaTrader OnTick(string symbol) handler.
/// </summary>
public class OnTickMultisymbolStrategy : Strategy
{
	private readonly StrategyParam<string> _symbols;

	private readonly List<Security> _resolvedSecurities = new();
	private readonly HashSet<Security> _workingSet = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="OnTickMultisymbolStrategy"/> class.
	/// </summary>
	public OnTickMultisymbolStrategy()
	{
		_symbols = Param(nameof(Symbols), "EURUSD,GBPUSD,USDJPY,USDCHF")
			.SetDisplay("Symbols", "Comma-separated identifiers monitored for tick events.", "General");
	}

	/// <summary>
	/// Gets or sets the comma-separated list of security identifiers to observe.
	/// </summary>
	public string Symbols
	{
		get => _symbols.Value;
		set => _symbols.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var returned = new HashSet<Security>();

		if (Security != null && returned.Add(Security))
			yield return (Security, DataType.Ticks);

		foreach (var security in _resolvedSecurities)
		{
			if (security != null && returned.Add(security))
				yield return (security, DataType.Ticks);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_resolvedSecurities.Clear();
		_workingSet.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResolveSymbols();

		foreach (var security in _workingSet)
		{
			var localSecurity = security;

			SubscribeTrades(localSecurity)
				.Bind(trade => OnTrade(localSecurity, trade))
				.Start();
		}
	}

	private void ResolveSymbols()
	{
		_resolvedSecurities.Clear();
		_workingSet.Clear();

		if (Security != null)
		{
			_resolvedSecurities.Add(Security);
			_workingSet.Add(Security);
		}

		var provider = SecurityProvider;
		var text = Symbols;

		if (provider == null)
		{
			if (_resolvedSecurities.Count == 0)
				LogWarning("SecurityProvider is not assigned. Only the primary Security will be monitored.");

			return;
		}

		if (string.IsNullOrWhiteSpace(text))
			return;

		var parts = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var part in parts)
		{
			var symbol = part.Trim();
			if (symbol.Length == 0)
				continue;

			if (string.Equals(symbol, "MARKET_WATCH", StringComparison.OrdinalIgnoreCase))
			{
				LogWarning("MARKET_WATCH token is not supported automatically. Provide explicit symbols in the Symbols parameter.");
				continue;
			}

			var security = provider.LookupById(symbol);
			if (security == null)
			{
				LogWarning($"Security '{symbol}' was not found via SecurityProvider. Skipping.");
				continue;
			}

			if (_workingSet.Add(security))
				_resolvedSecurities.Add(security);
		}

		if (_workingSet.Count == 0)
		{
			LogWarning("No symbols were resolved. The strategy will stay idle.");
		}
	}

	private void OnTrade(Security security, ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price)
			return;

		var volume = trade.Volume ?? 0m;
		var time = trade.ServerTime != default ? trade.ServerTime : trade.LocalTime;

		LogInfo($"Tick received for {security.Id} at {time:O}: price={price}, volume={volume}.");
	}
}
