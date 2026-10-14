using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monthly purchase strategy with dynamic contract size.
/// From StartDate on, buys on the candle whose day of month equals BuyDay. The size is PercentOfEquity of the
/// current portfolio value divided by the close price. Positions are never closed; the equity drawdown is only tracked.
/// </summary>
public class MonthlyPurchaseWithDynamicContractSizeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<decimal> _percentOfEquity;
	private readonly StrategyParam<int> _buyDay;

	private DateTime? _lastBuyDate;
	private decimal _peakEquity;
	private decimal _maxDrawdown;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Purchases are allowed from this date on.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Fraction of equity spent on each purchase.
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
	/// Largest equity drawdown seen so far, for information only.
	/// </summary>
	public decimal MaxDrawdown => _maxDrawdown;

	/// <summary>
	/// Constructor.
	/// </summary>
	public MonthlyPurchaseWithDynamicContractSizeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Purchases are allowed from this date on", "General");

		_percentOfEquity = Param(nameof(PercentOfEquity), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Percent of Equity", "Fraction of equity spent on each purchase", "Trading");

		_buyDay = Param(nameof(BuyDay), 1)
			.SetRange(1, 31)
			.SetDisplay("Buy Day", "Day of the month to buy", "Trading");
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
		_lastBuyDate = null;
		_peakEquity = 0m;
		_maxDrawdown = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastBuyDate = null;
		_peakEquity = 0m;
		_maxDrawdown = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity > _peakEquity)
			_peakEquity = equity;
		if (_peakEquity > 0m)
			_maxDrawdown = Math.Max(_maxDrawdown, (_peakEquity - equity) / _peakEquity);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var openTime = candle.OpenTime;
		if (openTime < StartDate.UtcDateTime || openTime.Day != BuyDay || _lastBuyDate == openTime.Date)
			return;

		var close = candle.ClosePrice;
		if (close <= 0m || equity <= 0m)
			return;

		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var volume = Math.Floor(equity * PercentOfEquity / close / step) * step;
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_lastBuyDate = openTime.Date;
	}
}
