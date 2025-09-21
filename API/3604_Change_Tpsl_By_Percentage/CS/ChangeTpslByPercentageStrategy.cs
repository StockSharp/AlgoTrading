using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk management strategy that closes positions when floating profit or loss
/// reaches levels derived from account balance percentages and leverage.
/// Replicates the MetaTrader script that continuously adjusts take-profit and
/// stop-loss distances as the account balance or margin changes.
/// </summary>
public class ChangeTpslByPercentageStrategy : Strategy
{
	private readonly StrategyParam<decimal> _percentageProfit;
	private readonly StrategyParam<decimal> _percentageStopLoss;
	private readonly StrategyParam<decimal> _symbolLeverage;

	private decimal? _currentBid;
	private decimal? _currentAsk;

	/// <summary>
	/// Profit percentage relative to the account balance.
	/// </summary>
	public decimal PercentageProfit
	{
		get => _percentageProfit.Value;
		set => _percentageProfit.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage relative to the account balance.
	/// </summary>
	public decimal PercentageStopLoss
	{
		get => _percentageStopLoss.Value;
		set => _percentageStopLoss.Value = value;
	}

	/// <summary>
	/// Symbol leverage factor used for translating percentage into price distance.
	/// </summary>
	public decimal SymbolLeverage
	{
		get => _symbolLeverage.Value;
		set => _symbolLeverage.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChangeTpslByPercentageStrategy"/> class.
	/// </summary>
	public ChangeTpslByPercentageStrategy()
	{
		_percentageProfit = Param(nameof(PercentageProfit), 40m)
			.SetNotNegative()
			.SetDisplay("Profit Percentage", "Target profit as percentage of account balance", "Risk Management");

		_percentageStopLoss = Param(nameof(PercentageStopLoss), 90m)
			.SetNotNegative()
			.SetDisplay("Stop-loss Percentage", "Allowed loss as percentage of account balance", "Risk Management");

		_symbolLeverage = Param(nameof(SymbolLeverage), 0.5m)
			.SetNotNegative()
			.SetDisplay("Symbol Leverage", "Leverage factor (e.g. 1:200 => 0.5)", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentBid = null;
		_currentAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_currentBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_currentAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var positionVolume = Position.Abs();
		if (positionVolume <= 0m)
		return;

		var entryPriceNullable = Position.AveragePrice;
		if (entryPriceNullable is not decimal entryPrice || entryPrice <= 0m)
		return;

		var portfolio = Portfolio;
		var balance = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
		return;

		var margin = GetMarginValue(portfolio);
		if (margin <= 0m)
		return;

		var profitTarget = balance * PercentageProfit / 100m;
		var stopTarget = balance * PercentageStopLoss / 100m;

		if (profitTarget <= 0m && stopTarget <= 0m)
		return;

		var profitFactor = profitTarget > 0m ? SymbolLeverage * (profitTarget / margin) / 100m : 0m;
		var stopFactor = stopTarget > 0m ? SymbolLeverage * (stopTarget / margin) / 100m : 0m;

		var longTake = entryPrice * (1m + profitFactor);
		var longStop = entryPrice * (1m - stopFactor);
		var shortTake = entryPrice * (1m - profitFactor);
		var shortStop = entryPrice * (1m + stopFactor);

		if (longStop < 0m)
		longStop = 0m;

		if (shortTake < 0m)
		shortTake = 0m;

		if (Position > 0m)
		{
			if (_currentBid is not decimal bidPrice || bidPrice <= 0m)
			return;

			if (profitTarget > 0m && bidPrice >= longTake)
			{
				SellMarket(positionVolume);
				return;
			}

			if (stopTarget > 0m && bidPrice <= longStop)
			{
				SellMarket(positionVolume);
			}
		}
		else if (Position < 0m)
		{
			if (_currentAsk is not decimal askPrice || askPrice <= 0m)
			return;

			var absoluteVolume = positionVolume;

			if (profitTarget > 0m && askPrice <= shortTake)
			{
				BuyMarket(absoluteVolume);
				return;
			}

			if (stopTarget > 0m && askPrice >= shortStop)
			{
				BuyMarket(absoluteVolume);
			}
		}
	}

	private static decimal GetMarginValue(Portfolio? portfolio)
	{
		if (portfolio == null)
		return 0m;

		if (portfolio.BlockedValue is decimal blocked && blocked > 0m)
		return blocked;

		var margin = TryGetPortfolioMetric(portfolio, "Margin");
		return margin.HasValue && margin.Value > 0m ? margin.Value : 0m;
	}

	private static decimal? TryGetPortfolioMetric(Portfolio portfolio, string propertyName)
	{
		var property = portfolio.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
		if (property == null)
		return null;

		var value = property.GetValue(portfolio);

		return value switch
		{
			decimal decimalValue => decimalValue,
			double doubleValue => (decimal)doubleValue,
			float floatValue => (decimal)floatValue,
			int intValue => intValue,
			long longValue => longValue,
			_ => null,
		};
	}
}
