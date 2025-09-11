using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Turn of the Month Strategy by Honestcowboy.
/// </summary>
public class TurnOfTheMonthStrategyHonestcowboyStrategy : Strategy
{
	private readonly StrategyParam<int> _daysBeforeEnd;
	private readonly StrategyParam<int> _daysAfterStart;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Days before month end to start entry window.
	/// </summary>
	public int DaysBeforeEnd { get => _daysBeforeEnd.Value; set => _daysBeforeEnd.Value = value; }

	/// <summary>
	/// Days after month start to close position.
	/// </summary>
	public int DaysAfterStart { get => _daysAfterStart.Value; set => _daysAfterStart.Value = value; }

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TurnOfTheMonthStrategyHonestcowboyStrategy"/>.
	/// </summary>
	public TurnOfTheMonthStrategyHonestcowboyStrategy()
	{
		_daysBeforeEnd = Param(nameof(DaysBeforeEnd), 2)
			.SetDisplay("Days Before End", "Days before month end to begin entries", "Strategy");

		_daysAfterStart = Param(nameof(DaysAfterStart), 3)
			.SetDisplay("Days After Start", "Days after new month to exit", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Strategy");
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
			.WhenTicked(ProcessCandle)
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dom = candle.OpenTime.Day;
		var dow = candle.OpenTime.DayOfWeek;
		var daysInMonth = DateTime.DaysInMonth(candle.OpenTime.Year, candle.OpenTime.Month);
		var entryBar = daysInMonth - DaysBeforeEnd;

		bool longCondition = dom >= entryBar
			|| (dow == DayOfWeek.Thursday && dom == entryBar - 1)
			|| (dow == DayOfWeek.Thursday && dom == entryBar - 2)
			|| (dow == DayOfWeek.Thursday && dom == entryBar - 3);

		bool closeCondition = (
				dom >= DaysAfterStart
				|| (dow == DayOfWeek.Thursday && dom == DaysAfterStart - 1)
				|| (dow == DayOfWeek.Thursday && dom == DaysAfterStart - 2)
				|| (dow == DayOfWeek.Thursday && dom == DaysAfterStart - 3))
			&& dom < entryBar - 10;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (closeCondition && Position > 0)
		{
			ClosePosition();
		}
	}
}

