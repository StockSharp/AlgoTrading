using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Turn of the Month strategy by Honestcowboy.
/// Goes long near month end and exits early in the new month.
/// Also uses weekly turn pattern: buy on Thursday/Friday, sell on Monday/Tuesday.
/// </summary>
public class TurnOfTheMonthHonestcowboyStrategy : Strategy
{
	private readonly StrategyParam<int> _daysBeforeEnd;
	private readonly StrategyParam<int> _daysAfterStart;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevDay;

	public int DaysBeforeEnd { get => _daysBeforeEnd.Value; set => _daysBeforeEnd.Value = value; }
	public int DaysAfterStart { get => _daysAfterStart.Value; set => _daysAfterStart.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurnOfTheMonthHonestcowboyStrategy()
	{
		_daysBeforeEnd = Param(nameof(DaysBeforeEnd), 5)
			.SetDisplay("Days Before End", "Days before month end to begin entries", "Strategy");

		_daysAfterStart = Param(nameof(DaysAfterStart), 5)
			.SetDisplay("Days After Start", "Days after new month to exit", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Strategy");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDay = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dom = candle.OpenTime.Day;
		var daysInMonth = DateTime.DaysInMonth(candle.OpenTime.Year, candle.OpenTime.Month);

		// Only trigger once per day change
		if (dom == _prevDay)
			return;
		_prevDay = dom;

		// Monthly turn: buy near end of month
		var entryBar = daysInMonth - DaysBeforeEnd;
		var monthlyLong = dom >= entryBar;
		var monthlyClose = dom <= DaysAfterStart;

		// Weekly turn: buy Thu/Fri, sell Mon/Tue
		var dow = candle.OpenTime.DayOfWeek;
		var weeklyLong = dow == DayOfWeek.Thursday || dow == DayOfWeek.Friday;
		var weeklyClose = dow == DayOfWeek.Monday || dow == DayOfWeek.Tuesday;

		// Combine: buy if any long condition, sell if any close condition
		if ((monthlyLong || weeklyLong) && Position <= 0)
		{
			BuyMarket();
		}
		else if ((monthlyClose || weeklyClose) && Position > 0)
		{
			SellMarket();
		}
	}
}
