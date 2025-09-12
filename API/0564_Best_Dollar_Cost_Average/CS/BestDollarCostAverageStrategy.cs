using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Best Dollar Cost Average Strategy - invests a fixed amount at regular intervals.
/// </summary>
public class BestDollarCostAverageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _amountInvested;
	private readonly StrategyParam<DcaInterval> _interval;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private DateTimeOffset _nextBuyTime;
	private decimal _totalSpent;
	private decimal _totalQuantity;
	private decimal _lastPrice;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Amount invested each period in currency.
	/// </summary>
	public decimal AmountInvested
	{
		get => _amountInvested.Value;
		set => _amountInvested.Value = value;
	}

	/// <summary>
	/// Interval for dollar cost averaging.
	/// </summary>
	public DcaInterval Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
	}

	/// <summary>
	/// Start date of accumulation.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date of accumulation.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Period options for DCA.
	/// </summary>
	public enum DcaInterval
	{
		Daily,
		Weekly,
		Monthly,
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BestDollarCostAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_amountInvested = Param(nameof(AmountInvested), 100m)
			.SetRange(0.01m, 1000000m)
			.SetDisplay("Amount", "Amount invested each period", "DCA");

		_interval = Param(nameof(Interval), DcaInterval.Weekly)
			.SetDisplay("Interval", "Investment interval", "DCA");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2018, 1, 1)))
			.SetDisplay("Start Date", "Start date of accumulation", "DCA");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2020, 1, 28)))
			.SetDisplay("End Date", "End date of accumulation", "DCA");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_nextBuyTime = StartDate;

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

		if (candle.OpenTime < StartDate || candle.OpenTime > EndDate)
		{
			_lastPrice = candle.ClosePrice;
			return;
		}

		if (candle.OpenTime >= _nextBuyTime && candle.OpenTime <= EndDate && IsOnline)
		{
			var price = candle.ClosePrice;
			var volume = AmountInvested / price;

			RegisterOrder(CreateOrder(Sides.Buy, price, volume));

			_totalSpent += AmountInvested;
			_totalQuantity += volume;

			_nextBuyTime = GetNextBuyTime(_nextBuyTime);
		}

		_lastPrice = candle.ClosePrice;
	}

	private DateTimeOffset GetNextBuyTime(DateTimeOffset current)
	{
		return Interval switch
		{
			DcaInterval.Daily => current + TimeSpan.FromDays(1),
			DcaInterval.Weekly => current + TimeSpan.FromDays(7),
			DcaInterval.Monthly => current.AddMonths(1),
			_ => current,
		};
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		var portfolioValue = _totalQuantity * _lastPrice;
		var profit = portfolioValue - _totalSpent;
		var percent = _totalSpent == 0 ? 0 : profit / _totalSpent * 100m;

		LogInfo($"Spent: {_totalSpent:0.##}, Qty: {_totalQuantity:0.######}, Value: {portfolioValue:0.##}, PnL: {profit:0.##} ({percent:0.##}%)");

		base.OnStopped();
	}
}
