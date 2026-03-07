using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// January barometer strategy that rotates between the primary instrument and a benchmark proxy based on the primary January return.
/// </summary>
public class JanuaryBarometerStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<decimal> _minUsd;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private decimal _januaryOpen;
	private int _decisionYear;

	/// <summary>
	/// Benchmark proxy identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
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

	public JanuaryBarometerStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Defensive benchmark proxy", "General");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Min trade USD", "Minimum order value", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!Security2Id.IsEmpty())
			yield return (new Security { Id = Security2Id }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_latestPrices.Clear();
		_januaryOpen = 0m;
		_decisionYear = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Benchmark security identifier is not specified.");

		var primarySubscription = SubscribeCandles(CandleType, security: Security);

		primarySubscription
			.Bind(candle => ProcessCandle(candle, Security))
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrices[security] = candle.ClosePrice;

		if (security != Security)
			return;

		var day = candle.OpenTime.Date;

		if (day.Month == 1 && _januaryOpen == 0m)
			_januaryOpen = candle.OpenPrice;

		if (day.Month == 2 && _decisionYear != day.Year && _januaryOpen > 0m)
		{
			_decisionYear = day.Year;
			var januaryReturn = (candle.ClosePrice - _januaryOpen) / _januaryOpen;
			Rebalance(januaryReturn > 0m);
		}
	}

	private void Rebalance(bool bullish)
	{
		Move(Security, bullish ? 1m : -1m);
	}

	private void Move(Security security, decimal weight)
	{
		var price = GetLatestPrice(security);
		if (price <= 0m)
			return;

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var target = weight * portfolioValue / price;
		var diff = target - GetPositionValue(security, Portfolio).GetValueOrDefault();

		if (Math.Abs(diff) * price < MinTradeUsd)
			return;

		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = diff > 0 ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "JanBar"
		});
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}
}
