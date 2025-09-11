using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buys a percentage of equity on a specific day each month.
/// </summary>
public class MonthlyPurchaseStrategyWithDynamicContractSizeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<decimal> _percentOfEquity;
	private readonly StrategyParam<int> _buyDay;

	private decimal? _highestEquity;
	private DateTimeOffset? _lastBuyTime;

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start date after which purchases are allowed.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Percentage of equity used for each purchase.
	/// </summary>
	public decimal PercentOfEquity
	{
		get => _percentOfEquity.Value;
		set => _percentOfEquity.Value = value;
	}

	/// <summary>
	/// Day of the month to buy.
	/// </summary>
	public int BuyDay
	{
		get => _buyDay.Value;
		set => _buyDay.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MonthlyPurchaseStrategyWithDynamicContractSizeStrategy"/>.
	/// </summary>
	public MonthlyPurchaseStrategyWithDynamicContractSizeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Purchases start from this date", "Strategy");

		_percentOfEquity = Param(nameof(PercentOfEquity), 0.03m)
			.SetRange(0.01m, 10m)
			.SetDisplay("Percent of Equity", "Percentage of equity per purchase", "Strategy");

		_buyDay = Param(nameof(BuyDay), 1)
			.SetRange(1, 31)
			.SetDisplay("Buy Day", "Day of month to buy", "Strategy");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highestEquity = null;
		_lastBuyTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var equity = Portfolio.CurrentValue;
		if (_highestEquity == null || equity > _highestEquity)
			_highestEquity = equity;

		var isAfterStart = candle.OpenTime >= StartDate;
		if (!isAfterStart)
			return;

		var currentDay = candle.OpenTime.Day;
		var lastMonth = _lastBuyTime?.Month;
		var lastYear = _lastBuyTime?.Year;

		if (currentDay == BuyDay && (lastMonth != candle.OpenTime.Month || lastYear != candle.OpenTime.Year))
		{
			var contracts = (int)(equity * PercentOfEquity / candle.ClosePrice);
			if (contracts > 0)
			{
				BuyMarket(contracts);
				_lastBuyTime = candle.OpenTime;
			}
		}
	}
}
