using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Equal-weight crypto basket strategy that rebalances the primary and secondary instruments on a weekly schedule.
/// </summary>
public class CryptoRebalancingPremiumStrategy : Strategy
{
	private readonly StrategyParam<string> _secondarySecurityId;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;

	private Security _secondarySecurity = null!;
	private decimal _latestPrimaryPrice;
	private decimal _latestSecondaryPrice;
	private DateTime _lastRebalanceTime;

	/// <summary>
	/// Secondary crypto security identifier.
	/// </summary>
	public string SecondarySecurityId
	{
		get => _secondarySecurityId.Value;
		set => _secondarySecurityId.Value = value;
	}

	/// <summary>
	/// Minimum trade amount in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minTradeUsd.Value;
		set => _minTradeUsd.Value = value;
	}

	/// <summary>
	/// Candle type used for both instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CryptoRebalancingPremiumStrategy()
	{
		_secondarySecurityId = Param(nameof(SecondarySecurityId), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Second Security Id", "Identifier of the secondary crypto security", "General");

		_minTradeUsd = Param(nameof(MinTradeUsd), 200m)
			.SetRange(10m, 10000m)
			.SetDisplay("Min Trade USD", "Minimum dollar amount per trade", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!SecondarySecurityId.IsEmpty())
			yield return (new Security { Id = SecondarySecurityId }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_secondarySecurity = null!;
		_latestPrimaryPrice = 0m;
		_latestSecondaryPrice = 0m;
		_lastRebalanceTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary crypto security is not specified.");

		if (SecondarySecurityId.IsEmpty())
			throw new InvalidOperationException("Secondary crypto security identifier is not specified.");

		_secondarySecurity = this.LookupById(SecondarySecurityId) ?? new Security { Id = SecondarySecurityId };

		var primarySubscription = SubscribeCandles(CandleType, security: Security);
		var secondarySubscription = SubscribeCandles(CandleType, security: _secondarySecurity);

		primarySubscription
			.Bind(candle => ProcessCandle(candle, Security))
			.Start();

		secondarySubscription
			.Bind(candle => ProcessCandle(candle, _secondarySecurity))
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawCandles(area, secondarySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (security == Security)
			_latestPrimaryPrice = candle.ClosePrice;
		else if (security == _secondarySecurity)
			_latestSecondaryPrice = candle.ClosePrice;

		if (_latestPrimaryPrice <= 0m || _latestSecondaryPrice <= 0m)
			return;

		if (candle.OpenTime == _lastRebalanceTime)
			return;

		if (candle.OpenTime.DayOfWeek != DayOfWeek.Monday || candle.OpenTime.Hour != 0)
			return;

		_lastRebalanceTime = candle.OpenTime;
		Rebalance();
	}

	private void Rebalance()
	{
		RebalanceSecurity(Security, 1m);
		RebalanceSecurity(_secondarySecurity, 1m);
	}

	private void RebalanceSecurity(Security security, decimal targetVolume)
	{
		var price = security == Security ? _latestPrimaryPrice : _latestSecondaryPrice;
		if (price <= 0m)
			return;

		var diff = targetVolume - GetPositionValue(security, Portfolio).GetValueOrDefault();

		if (Math.Abs(diff) * price < MinTradeUsd)
			return;

		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = diff > 0 ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "RebalPrem"
		});
	}
}
