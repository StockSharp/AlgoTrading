namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that mirrors the Refresh28Charts utility by downloading a minimum amount of history for multiple symbols and time frames.
/// </summary>
public class Refresh28ChartsV3Strategy : Strategy
{
	private sealed class CandleRequestState
	{
		public Security Security { get; init; } = default!;
		public DataType CandleType { get; init; } = default!;
		public int RequiredBars { get; init; }
		public int ReceivedBars { get; set; }
		public bool IsCompleted => ReceivedBars >= RequiredBars;
	}

	private static readonly string[] DefaultSymbols =
	[
		"EURGBP", "GBPAUD", "AUDNZD", "NZDUSD", "USDCAD", "CADCHF", "CHFJPY",
		"EURAUD", "GBPNZD", "AUDUSD", "NZDCAD", "USDCHF", "CADJPY",
		"EURNZD", "GBPUSD", "AUDCAD", "NZDCHF", "USDJPY",
		"EURUSD", "GBPCAD", "AUDCHF", "NZDJPY",
		"EURCAD", "GBPCHF", "AUDJPY",
		"EURCHF", "GBPJPY",
		"EURJPY"
	];

	private static readonly TimeSpan[] TimeFrames =
	[
		TimeSpan.FromMinutes(1),
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(15),
		TimeSpan.FromMinutes(30),
		TimeSpan.FromHours(1),
		TimeSpan.FromHours(4),
		TimeSpan.FromDays(1),
		TimeSpan.FromDays(7),
		TimeSpan.FromDays(30)
	];

	private readonly StrategyParam<int> _barsToRefresh;
	private readonly StrategyParam<string> _symbols;

	private readonly Dictionary<(string symbol, TimeSpan frame), CandleRequestState> _states = new();
	private int _completedRequests;
	private bool _completionLogged;

	/// <summary>
	/// Number of final candles that must be loaded for every symbol and time frame.
	/// </summary>
	public int BarsToRefresh
	{
		get => _barsToRefresh.Value;
		set => _barsToRefresh.Value = value;
	}

	/// <summary>
	/// Comma separated list of security identifiers that should be refreshed.
	/// </summary>
	public string Symbols
	{
		get => _symbols.Value;
		set => _symbols.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters and default market basket.
	/// </summary>
	public Refresh28ChartsV3Strategy()
	{
		_barsToRefresh = Param(nameof(BarsToRefresh), 50)
			.SetDisplay("Bars To Refresh", "Minimal amount of history that needs to be cached", "History")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_symbols = Param(nameof(Symbols), string.Join(',', DefaultSymbols))
			.SetDisplay("Symbols", "Comma separated list of securities to download", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_states.Clear();
		_completedRequests = 0;
		_completionLogged = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var securities = ResolveSecurities().ToArray();
		if (securities.Length == 0)
			throw new InvalidOperationException("No securities resolved. Configure Symbols or assign Strategy.Security.");

		foreach (var security in securities)
		{
			foreach (var frame in TimeFrames)
			{
				var key = (security.Id, frame);
				var dataType = frame.TimeFrame();

				_states[key] = new CandleRequestState
				{
					Security = security,
					CandleType = dataType,
					RequiredBars = BarsToRefresh,
					ReceivedBars = 0
				};

				SubscribeCandles(dataType, true, security)
					.Bind(candle => ProcessCandle(candle, security, frame))
					.Start();
			}
		}

		LogInfo("Started refresh for {0} securities across {1} time frames ({2} subscriptions).", securities.Length, TimeFrames.Length, _states.Count);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var security in ResolveSecurities())
		{
			foreach (var frame in TimeFrames)
				yield return (security, frame.TimeFrame());
		}
	}

	private IEnumerable<Security> ResolveSecurities()
	{
		var raw = Symbols ?? string.Empty;
		var tokens = raw.Split(new[] { ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(t => t.Trim())
			.Where(t => !string.IsNullOrEmpty(t))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (tokens.Length == 0)
		{
			if (Security != null)
				yield return Security;

			yield break;
		}

		foreach (var token in tokens)
		{
			var security = SecurityProvider?.LookupById(token);

			if (security == null && Security != null)
			{
				if (Security.Id.EqualsIgnoreCase(token) ||
					Security.Code.EqualsIgnoreCase(token))
				{
					security = Security;
				}
			}

			if (security == null)
				throw new InvalidOperationException($"Security '{token}' was not found.");

			yield return security;
		}
	}

	private void ProcessCandle(ICandleMessage candle, Security security, TimeSpan frame)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_states.TryGetValue((security.Id, frame), out var state))
			return;

		if (state.IsCompleted)
			return;

		state.ReceivedBars++;

		if (state.IsCompleted)
		{
			_completedRequests++;
			LogInfo("History ready for {0} on {1}: {2} bars loaded.", security.Id, FormatTimeFrame(frame), state.ReceivedBars);
		}
		else if (state.ReceivedBars % Math.Max(1, BarsToRefresh / 5) == 0)
		{
			LogInfo("Progress {0}/{1} for {2} on {3}.", state.ReceivedBars, state.RequiredBars, security.Id, FormatTimeFrame(frame));
		}

		if (!_completionLogged && _completedRequests == _states.Count)
		{
			_completionLogged = true;
			LogInfo("All {0} symbol/time frame combinations reached {1} bars.", _states.Count, BarsToRefresh);
		}
	}

	private static string FormatTimeFrame(TimeSpan frame)
	{
		if (frame.TotalDays >= 30)
			return "1 month";

		if (frame.TotalDays >= 7)
			return "1 week";

		if (frame.TotalDays >= 1)
			return frame.TotalDays == 1 ? "1 day" : $"{frame.TotalDays:0} days";

		if (frame.TotalHours >= 1)
			return frame.TotalHours == 1 ? "1 hour" : $"{frame.TotalHours:0} hours";

		return frame.TotalMinutes == 1 ? "1 minute" : $"{frame.TotalMinutes:0} minutes";
	}
}
