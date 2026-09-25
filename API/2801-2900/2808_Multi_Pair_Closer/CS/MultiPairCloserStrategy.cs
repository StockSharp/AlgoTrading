using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supervises an existing basket and closes it when combined floating PnL reaches a configured boundary.
/// This utility never opens positions.
/// </summary>
public class MultiPairCloserStrategy : Strategy
{
	private readonly StrategyParam<string> _watchedSymbols;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<int> _minAgeSeconds;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<string, Security> _watched = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, DateTimeOffset> _firstSeen = new(StringComparer.OrdinalIgnoreCase);

	public string WatchedSymbols { get => _watchedSymbols.Value; set => _watchedSymbols.Value = value; }
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }
	public decimal MaxLoss { get => _maxLoss.Value; set => _maxLoss.Value = value; }
	public int Slippage { get => _slippage.Value; set => _slippage.Value = value; }
	public int MinAgeSeconds { get => _minAgeSeconds.Value; set => _minAgeSeconds.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiPairCloserStrategy()
	{
		_watchedSymbols = Param(nameof(WatchedSymbols), "GBPUSD,USDCAD,USDCHF,USDSEK")
			.SetDisplay("Watched Symbols", "Comma-separated basket to supervise. Empty means the assigned Security.", "Basket");
		_profitTarget = Param(nameof(ProfitTarget), 60m).SetNotNegative();
		_maxLoss = Param(nameof(MaxLoss), 60m).SetNotNegative();
		_slippage = Param(nameof(Slippage), 10).SetNotNegative();
		_minAgeSeconds = Param(nameof(MinAgeSeconds), 60).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> ResolveWatched().Select(sec => (sec, CandleType));

	protected override void OnReseted()
	{
		base.OnReseted();
		_watched.Clear();
		_firstSeen.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_watched.Clear();
		foreach (var security in ResolveWatched())
		{
			_watched[security.Id] = security;
			SubscribeCandles(CandleType, security: security)
				.Bind(candle =>
				{
					if (candle.State == CandleStates.Finished)
						EvaluateBasket(candle.CloseTime);
				})
				.Start();
		}
	}

	private Security[] ResolveWatched()
	{
		var ids = (WatchedSymbols ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (ids.Length == 0)
			return Security is null ? [] : [Security];

		return ids
			.Select(id => this.LookupById(id) ?? new Security { Id = id })
			.ToArray();
	}

	private void EvaluateBasket(DateTimeOffset time)
	{
		var positions = ((IPositionProvider)this).Positions
			.Where(p => p.Security is not null && p.Portfolio == Portfolio)
			.ToDictionary(p => p.Security.Id, StringComparer.OrdinalIgnoreCase);

		var totalPnl = 0m;
		var hasPosition = false;

		foreach (var (id, security) in _watched)
		{
			var value = GetPositionValue(security, Portfolio) ?? 0m;
			if (value == 0m)
			{
				_firstSeen.Remove(id);
				continue;
			}

			hasPosition = true;
			_firstSeen.TryAdd(id, time);

			if (positions.TryGetValue(id, out var position))
				totalPnl += position.UnrealizedPnL ?? 0m;
		}

		if (!hasPosition)
			return;

		var shouldClose =
			(ProfitTarget >= 0m && totalPnl >= ProfitTarget) ||
			(MaxLoss > 0m && totalPnl <= -MaxLoss);

		if (!shouldClose)
			return;

		foreach (var (id, security) in _watched)
		{
			var value = GetPositionValue(security, Portfolio) ?? 0m;
			if (value == 0m)
				continue;

			if (!_firstSeen.TryGetValue(id, out var firstSeen))
				_firstSeen[id] = firstSeen = time;

			if (MinAgeSeconds > 0 && (time - firstSeen).TotalSeconds < MinAgeSeconds)
				continue;

			RegisterOrder(new Order
			{
				Security = security,
				Portfolio = Portfolio,
				Type = OrderTypes.Market,
				Side = value > 0m ? Sides.Sell : Sides.Buy,
				Volume = Math.Abs(value),
				Comment = $"MultiPairCloser slippage={Slippage}",
			});
		}
	}
}
