using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supervises the account positions of a basket of instruments and closes them when their combined floating
/// profit, as the connector reports it, reaches the profit target or drops below the loss limit.
/// This utility never opens positions.
/// </summary>
public class MultiPairCloserStrategy : Strategy
{
	private const string _profitTargetReason = "reached the profit target";
	private const string _maxLossReason = "fell below the loss limit";

	private readonly StrategyParam<string> _watchedSymbols;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<int> _minAgeSeconds;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<Security> _watched = [];
	private readonly Dictionary<string, DateTime?> _firstSeen = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Order> _exitOrders = new(StringComparer.OrdinalIgnoreCase);
	private IConnector _positionSource;
	private DateTime? _startTime;

	/// <summary>
	/// Comma-separated identifiers of the securities to supervise. Empty means the assigned <see cref="Strategy.Security"/>.
	/// </summary>
	public string WatchedSymbols
	{
		get => _watchedSymbols.Value;
		set => _watchedSymbols.Value = value;
	}

	/// <summary>
	/// Combined floating profit, in portfolio currency, that closes every watched position.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Maximum acceptable combined floating loss, in portfolio currency, before the basket is force-closed.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Slippage allowed by the original script. Exits are market orders, so the value is only logged.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Minimum lifetime of a position, in seconds, before the strategy may close it.
	/// </summary>
	public int MinAgeSeconds
	{
		get => _minAgeSeconds.Value;
		set => _minAgeSeconds.Value = value;
	}

	/// <summary>
	/// Candle type whose finished candles trigger the profit evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiPairCloserStrategy"/>.
	/// </summary>
	public MultiPairCloserStrategy()
	{
		_watchedSymbols = Param(nameof(WatchedSymbols), "GBPUSD,USDCAD,USDCHF,USDSEK")
			.SetDisplay("Watched Symbols", "Comma-separated security identifiers to supervise; empty means the assigned security", "Basket");

		_profitTarget = Param(nameof(ProfitTarget), 60m)
			.SetNotNegative()
			.SetDisplay("Profit Target", "Combined floating profit in portfolio currency that closes every watched position", "Risk");

		_maxLoss = Param(nameof(MaxLoss), 60m)
			.SetNotNegative()
			.SetDisplay("Max Loss", "Maximum acceptable combined floating loss in portfolio currency before the basket is force-closed", "Risk");

		_slippage = Param(nameof(Slippage), 10)
			.SetNotNegative()
			.SetDisplay("Slippage", "Slippage allowed by the original script; exits are market orders, so it is only logged", "Execution");

		_minAgeSeconds = Param(nameof(MinAgeSeconds), 60)
			.SetNotNegative()
			.SetDisplay("Min Age (s)", "Minimum lifetime of a position before the strategy may close it", "Execution");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Every finished candle of this type triggers a profit evaluation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> ResolveWatched(false).Select(security => (security, CandleType));

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		DetachPositionSource();
		_watched.Clear();
		_firstSeen.Clear();
		_exitOrders.Clear();
		_startTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_watched.Clear();
		_watched.AddRange(ResolveWatched(true));
		_firstSeen.Clear();
		_exitOrders.Clear();

		// The start is taken on the market clock, which a backtest sets only once the replay begins.
		_startTime = GetMarketTime();

		foreach (var security in _watched)
		{
			if (GetReportedPosition(security).volume != 0m)
				_firstSeen[security.Id] = _startTime;
		}

		_positionSource = Connector;
		_positionSource.PositionChanged += OnReportedPositionChanged;

		foreach (var security in _watched)
		{
			SubscribeCandles(CandleType, security: security)
				.Bind(ProcessCandle)
				.Start();
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		DetachPositionSource();

		base.OnStopped();
	}

	private void DetachPositionSource()
	{
		if (_positionSource is null)
			return;

		_positionSource.PositionChanged -= OnReportedPositionChanged;
		_positionSource = null;
	}

	private List<Security> ResolveWatched(bool strict)
	{
		var ids = (WatchedSymbols ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var securities = new List<Security>();

		if (ids.Length == 0)
		{
			if (Security is not null)
				securities.Add(Security);
			else if (strict)
				throw new InvalidOperationException("WatchedSymbols is empty and no Security is assigned.");

			return securities;
		}

		if (Connector is null)
			return securities;

		foreach (var id in ids)
		{
			var security = this.LookupById(id);

			if (security is not null)
				securities.Add(security);
			else if (strict)
				throw new InvalidOperationException($"Security '{id}' is not available through the connector.");
		}

		return securities;
	}

	private void OnReportedPositionChanged(Position position)
	{
		var security = FindWatched(position);

		if (security is null)
			return;

		if (GetReportedPosition(security).volume == 0m)
			_firstSeen.Remove(security.Id);
		else if (!_firstSeen.ContainsKey(security.Id))
			_firstSeen[security.Id] = GetMarketTime() ?? _startTime;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		EvaluateBasket();
	}

	private void EvaluateBasket()
	{
		var now = CurrentTime;
		_startTime ??= now;

		var summary = new List<string>(_watched.Count + 1);
		var open = new List<(Security security, decimal volume)>();
		decimal? total = 0m;

		foreach (var security in _watched)
		{
			var (volume, profit) = GetReportedPosition(security);

			if (volume == 0m)
			{
				summary.Add($"{security.Id}: {Format(0m)}");
				continue;
			}

			open.Add((security, volume));
			total += profit;
			summary.Add($"{security.Id}: {(profit is decimal value ? Format(value) : "n/a")}");
		}

		summary.Add($"Basket: {(total is decimal sum ? Format(sum) : "n/a")}");
		LogInfo(string.Join("; ", summary));

		// A position without a reported floating profit leaves the basket result unknown, so nothing is decided.
		if (open.Count == 0 || total is not decimal basket)
			return;

		string reason;

		if (basket >= ProfitTarget)
			reason = _profitTargetReason;
		else if (basket < -MaxLoss)
			reason = _maxLossReason;
		else
			return;

		foreach (var (security, volume) in open)
		{
			if (_exitOrders.TryGetValue(security.Id, out var pending) && !pending.State.IsFinal())
				continue;

			var firstSeen = (_firstSeen.TryGetValue(security.Id, out var seen) ? seen : null) ?? _startTime.Value;

			if ((now - firstSeen).TotalSeconds < MinAgeSeconds)
				continue;

			var quantity = Math.Abs(volume);
			LogInfo($"Closing {security.Id}: basket {Format(basket)} {reason}, {(volume > 0m ? "sell" : "buy")} {Format(quantity)} at market (slippage {Slippage}).");

			_exitOrders[security.Id] = volume > 0m
				? SellMarket(quantity, security)
				: BuyMarket(quantity, security);
		}
	}

	private Security FindWatched(Position position)
		=> IsAccountPosition(position)
			? _watched.FirstOrDefault(security => security.Id.EqualsIgnoreCase(position.Security.Id))
			: null;

	// Floating profit is summed as the connector reports it; one missing value makes the whole sum unknown.
	private (decimal volume, decimal? profit) GetReportedPosition(Security security)
	{
		var volume = 0m;
		decimal? profit = 0m;

		foreach (var position in Connector.Positions)
		{
			if (!IsAccountPosition(position) || !position.Security.Id.EqualsIgnoreCase(security.Id))
				continue;

			if (position.CurrentValue is not decimal value || value == 0m)
				continue;

			volume += value;
			profit += position.UnrealizedPnL;
		}

		return (volume, profit);
	}

	// Rows a connector keeps per strategy repeat part of the account row, so only account rows are read.
	private bool IsAccountPosition(Position position)
		=> position.Security is not null
			&& position.StrategyId.IsEmpty()
			&& Portfolio is not null
			&& position.PortfolioName.EqualsIgnoreCase(Portfolio.Name);

	private DateTime? GetMarketTime()
	{
		var now = CurrentTime;
		return now == default ? null : now;
	}

	private static string Format(decimal value)
		=> value.ToString(CultureInfo.InvariantCulture);
}
