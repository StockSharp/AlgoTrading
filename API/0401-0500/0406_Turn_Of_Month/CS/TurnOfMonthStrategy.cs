// TurnOfMonthStrategy.cs
// -----------------------------------------------------------------------------
// Holds positions only around turn-of-the-month window.
// Default: long from N=1 trading day before month-end close
//          until D=3 trading day of new month close.
// Trigger: candle close timing.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Turn-of-the-month effect strategy for index ETFs.
/// </summary>
public class TurnOfMonthStrategy : Strategy
{
	private readonly StrategyParam<int> _prior;
	private readonly StrategyParam<int> _after;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Number of trading days before month-end to enter.
	/// </summary>
	public int DaysPrior
	{
		get => _prior.Value;
		set => _prior.Value = value;
	}

	/// <summary>
	/// Number of trading days into new month to exit.
	/// </summary>
	public int DaysAfter
	{
		get => _after.Value;
		set => _after.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private int _cooldownRemaining;

	public TurnOfMonthStrategy()
	{
		_prior = Param(nameof(DaysPrior), 2)
			.SetDisplay("Days Prior", "Trading days before month end", "Parameters");

		_after = Param(nameof(DaysAfter), 4)
			.SetDisplay("Days After", "Trading days into new month", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var d = candle.OpenTime.Date;
		var tdLeft = TradingDaysLeftInMonth(d);
		var tdNum = TradingDayNumber(d);

		var inWindow = (tdLeft <= DaysPrior) || (tdNum <= DaysAfter);

		if (inWindow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (!inWindow && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}

	private static int TradingDaysLeftInMonth(DateTime d)
	{
		var cnt = 0;
		var cur = d;
		while (cur.Month == d.Month)
		{
			if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday)
				cnt++;
			cur = cur.AddDays(1);
		}
		return cnt - 1;
	}

	private static int TradingDayNumber(DateTime d)
	{
		var n = 0;
		var cur = new DateTime(d.Year, d.Month, 1);
		while (cur <= d)
		{
			if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday)
				n++;
			cur = cur.AddDays(1);
		}
		return n;
	}
}
