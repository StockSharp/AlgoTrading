using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors a basket of securities and closes every open position once
/// the aggregated floating profit reaches the configured target or hits the allowed loss.
/// </summary>
public class MultiPairCloserStrategy : Strategy
{
	private sealed class WatchedSecurityState
	{
		public Security Security { get; init; } = default!;
		public decimal LastProfit { get; set; }
		public DateTimeOffset? FirstOpenTime { get; set; }
	}

	private readonly Dictionary<Security, WatchedSecurityState> _states = [];

	private readonly StrategyParam<string> _watchedSymbols;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<int> _minAgeSeconds;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Comma separated list of security identifiers to supervise.
	/// </summary>
	public string WatchedSymbols
	{
		get => _watchedSymbols.Value;
		set => _watchedSymbols.Value = value;
	}

	/// <summary>
	/// Profit target in portfolio currency.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Maximum tolerated loss in portfolio currency.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Allowed slippage in price units used during emergency exits.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Minimum age of an open position before liquidation is permitted.
	/// </summary>
	public int MinAgeSeconds
	{
		get => _minAgeSeconds.Value;
		set => _minAgeSeconds.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger periodic profit checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults matching the MQL implementation.
	/// </summary>
	public MultiPairCloserStrategy()
	{
		_watchedSymbols = Param(nameof(WatchedSymbols), "GBPUSD,USDCAD,USDCHF,USDSEK")
			.SetDisplay("Watched Symbols", "Comma separated list of securities to monitor", "General");

		_profitTarget = Param(nameof(ProfitTarget), 60m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Profit Target", "Net profit required to close every watched position", "Risk Management");

		_maxLoss = Param(nameof(MaxLoss), 60m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Maximum Loss", "Maximum drawdown tolerated before closing positions", "Risk Management");

		_slippage = Param(nameof(Slippage), 10)
			.SetGreaterOrEqual(0)
			.SetDisplay("Slippage", "Accepted slippage (price steps) when liquidating positions", "Execution");

		_minAgeSeconds = Param(nameof(MinAgeSeconds), 60)
			.SetGreaterOrEqual(0)
			.SetDisplay("Minimum Age (s)", "Minimal holding time before forced exit is allowed", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for periodic supervision", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_states.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Resolve the list of instruments that should be supervised.
		var securities = ResolveWatchedSecurities().ToList();
		if (securities.Count == 0)
		{
			throw new InvalidOperationException("Unable to resolve watched securities. Set WatchedSymbols or Security.");
		}

		// Subscribe to candles for each instrument to trigger periodic checks.
		foreach (var security in securities)
		{
			_states[security] = new WatchedSecurityState
			{
				Security = security
			};

			SubscribeCandles(CandleType, true, security)
				.Bind(candle => ProcessCandle(candle, security))
				.Start();
		}

		RefreshPositionStates(time);
		LogInfo("Multi-pair closer started.");
	}

	private IEnumerable<Security> ResolveWatchedSecurities()
	{
		// Convert the comma separated list into distinct Security objects.
		var result = new HashSet<Security>();
		var raw = WatchedSymbols ?? string.Empty;
		var tokens = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(t => t.Trim())
			.Where(t => !string.IsNullOrEmpty(t))
			.ToArray();

		if (tokens.Length == 0)
		{
			if (Security != null)
			{
				result.Add(Security);
			}

			return result;
		}

		foreach (var token in tokens)
		{
			var security = SecurityProvider?.LookupById(token);

			if (security == null && Security != null)
			{
				if (string.Equals(Security.Id, token, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Security.Code, token, StringComparison.OrdinalIgnoreCase))
				{
					security = Security;
				}
			}

			if (security == null)
			{
				throw new InvalidOperationException($"Security '{token}' was not found.");
			}

			result.Add(security);
		}

		return result;
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var time = candle.CloseTime;

		RefreshPositionStates(time);
		EvaluateCloseConditions(time);
	}

	private void RefreshPositionStates(DateTimeOffset time)
	{
		// Track position start times to honour the minimum holding period.
		foreach (var pair in _states)
		{
			var volume = GetPositionValue(pair.Key, Portfolio) ?? 0m;
			var state = pair.Value;

			if (volume != 0m)
			{
				if (!state.FirstOpenTime.HasValue)
				{
					state.FirstOpenTime = time;
				}
			}
			else
			{
				state.FirstOpenTime = null;
			}
		}
	}

	private void EvaluateCloseConditions(DateTimeOffset time)
	{
		// Recalculate basket profit and determine whether protective actions are required.
		var netProfit = CalculateNetProfit();
		LogProfitState(netProfit);

		if (ProfitTarget > 0m && netProfit >= ProfitTarget)
		{
			LogInfo("Profit target reached. Closing watched positions.");
			CloseWatchedPositions(time);
		}
		else if (MaxLoss > 0m && netProfit <= -MaxLoss)
		{
			LogInfo("Maximum loss reached. Closing watched positions.");
			CloseWatchedPositions(time);
		}
	}

	private decimal CalculateNetProfit()
	{
		// Sum the per-security PnL published by StockSharp for all watched instruments.
		var net = 0m;

		foreach (var pair in _states)
		{
			var position = Positions.FirstOrDefault(p => p.Security == pair.Key);
			var profit = position?.PnL ?? 0m;

			pair.Value.LastProfit = profit;
			net += profit;
		}

		return net;
	}

	private void LogProfitState(decimal netProfit)
	{
		// Emulate the MT4 comment output with a per-symbol profit breakdown.
		var sb = new StringBuilder();

		foreach (var pair in _states)
		{
			sb.AppendLine($"{pair.Key.Id} : {pair.Value.LastProfit:F2}");
		}

		sb.AppendLine($"Net : {netProfit:F2}");
		LogInfo(sb.ToString());
	}

	private void CloseWatchedPositions(DateTimeOffset time)
	{
		// Flatten every eligible position using market orders.
		foreach (var pair in _states)
		{
			var volume = GetPositionValue(pair.Key, Portfolio) ?? 0m;
			if (volume == 0m)
			{
				continue;
			}

			if (!CanClose(pair.Value, time))
			{
				continue;
			}

			if (volume > 0m)
			{
				SellMarket(volume, pair.Key);
			}
			else
			{
				BuyMarket(Math.Abs(volume), pair.Key);
			}
		}
	}

	private bool CanClose(WatchedSecurityState state, DateTimeOffset time)
	{
		// Verify that the minimum holding time requirement is satisfied.
		if (MinAgeSeconds <= 0)
		{
			return true;
		}

		return state.FirstOpenTime.HasValue && (time - state.FirstOpenTime.Value).TotalSeconds >= MinAgeSeconds;
	}
}
