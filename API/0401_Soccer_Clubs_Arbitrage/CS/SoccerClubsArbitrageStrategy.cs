// SoccerClubsArbitrageStrategy.cs
// -----------------------------------------------------------------------------
// Two share classes of the same soccer club (pair length = 2).
// Long cheaper share, short expensive when relative premium > EntryThresh;
// exit when premium shrinks below ExitThresh.
// Triggered by daily candle of the first ticker — no Schedule used.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Arbitrage strategy for two share classes of the same soccer club.
/// </summary>
public class SoccerClubsArbitrageStrategy : Strategy
{
	#region Params
	private readonly StrategyParam<IEnumerable<Security>> _pair;
	private readonly StrategyParam<decimal> _entry;
	private readonly StrategyParam<decimal> _exit;
	private readonly StrategyParam<decimal> _minUsd;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Securities pair used for arbitrage.
	/// </summary>
	public IEnumerable<Security> Pair
	{
		get => _pair.Value;
		set => _pair.Value = value;
	}

	/// <summary>
	/// Premium threshold to enter a position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entry.Value;
		set => _entry.Value = value;
	}

	/// <summary>
	/// Premium threshold to exit a position.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exit.Value;
		set => _exit.Value = value;
	}

	/// <summary>
	/// Minimum trade value in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	#endregion

	private Security _a, _b;
	private readonly Dictionary<Security, decimal> _latestPrices = [];

	public SoccerClubsArbitrageStrategy()
	{
		_pair = Param<IEnumerable<Security>>(nameof(Pair), [])
			.SetDisplay("Pair", "Securities pair to arbitrage", "General");

		_entry = Param(nameof(EntryThreshold), 0.05m)
			.SetDisplay("Entry Threshold", "Premium difference to open position", "Parameters");

		_exit = Param(nameof(ExitThreshold), 0.01m)
			.SetDisplay("Exit Threshold", "Premium difference to close position", "Parameters");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
			.SetDisplay("Min Trade USD", "Minimum notional value for orders", "Risk Management");
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		if (Pair.Count() != 2)
			throw new InvalidOperationException("Pair must contain exactly two tickers.");

		_a = Pair.ElementAt(0);
		_b = Pair.ElementAt(1);
		yield return (_a, CandleType);
		yield return (_b, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_latestPrices.Clear();
		_a = null;
		_b = null;

	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (Pair == null || !Pair.Any())
			throw new InvalidOperationException("Pair must contain securities.");

		base.OnStarted(time);

		// Subscribe to both securities to get price updates
		foreach (var (s, tf) in GetWorkingSecurities())
		{
			SubscribeCandles(tf, true, s)
				.Bind(c => ProcessCandle(c, s))
				.Start();
		}

		// Use first ticker's candle as daily trigger
		SubscribeCandles(CandleType, true, _a)
			.Bind(c => TriggerDaily())
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for this security
		_latestPrices[security] = candle.ClosePrice;
	}

	private void TriggerDaily()
	{
		OnDaily();
	}

	private void OnDaily()
	{
		var pxA = GetLatestPrice(_a);
		var pxB = GetLatestPrice(_b);
		if (pxA == 0 || pxB == 0)
			return;

		var premium = pxA / pxB - 1m;

		if (Math.Abs(premium) < ExitThreshold)
		{
			Hedge(0);
			return;
		}

		if (premium > EntryThreshold)
			Hedge(-1);       // A overpriced → short A, long B
		else if (premium < -EntryThreshold)
			Hedge(+1);       // B overpriced → long A, short B
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	// dir = +1 ⇒ long A / short B ; dir = –1 ⇒ short A / long B ; dir = 0 ⇒ flat
	private void Hedge(int dir)
	{
		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		decimal half = portfolioValue / 2;
		var priceA = GetLatestPrice(_a);
		var priceB = GetLatestPrice(_b);
		if (priceA > 0)
			Move(_a, dir * half / priceA);
		if (priceB > 0)
			Move(_b, -dir * half / priceB);
	}

	private void Move(Security s, decimal tgtQty)
	{
		var diff = tgtQty - PositionBy(s);
		var price = GetLatestPrice(s);
		if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
			return;

		RegisterOrder(new Order
		{
			Security = s,
			Portfolio = Portfolio,
			Side = diff > 0 ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "SoccerArb"
		});
	}

	private decimal PositionBy(Security s) =>
		GetPositionValue(s, Portfolio) ?? 0m;
}