using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buys on the first Friday candle within the specified date range and closes after a number of bars.
/// </summary>
public class GoldFridayAnomalyStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<int> _holdBars;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _lastEntryDate;
	private int _barsSinceEntry;

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Number of bars to hold position after entry.
	/// </summary>
	public int HoldBars
	{
		get => _holdBars.Value;
		set => _holdBars.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GoldFridayAnomalyStrategy"/>.
	/// </summary>
	public GoldFridayAnomalyStrategy()
	{
		_startDate = Param(nameof(StartDate), new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Backtest start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "Backtest end date", "General");

		_holdBars = Param(nameof(HoldBars), 4)
			.SetDisplay("Hold Bars", "Number of bars to hold position", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;

		if (time < StartDate || time > EndDate)
			return;

		if (time.DayOfWeek == DayOfWeek.Friday &&
			time.Date != _lastEntryDate &&
			Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_lastEntryDate = time.Date;
			_barsSinceEntry = 0;
			return;
		}

		if (Position > 0)
		{
			_barsSinceEntry++;

			if (_barsSinceEntry >= HoldBars)
				ClosePosition();
		}
	}
}
