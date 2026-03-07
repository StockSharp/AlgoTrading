// PaydayAnomalyStrategy.cs
// -----------------------------------------------------------------------------
// Holds market ETF only during days -2..+3 around typical U.S. payday
// (assume salary hits 1st business day of month). Long ETF from two trading
// days before month‑end through third trading day of new month.
// Trigger: daily candle close.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
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
/// Payday anomaly strategy.
/// Holds market ETF during the payday window.
/// </summary>
public class PaydayAnomalyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _last = DateTime.MinValue;
	private int _enteredMonthKey;
	private int _exitedMonthKey;

	public PaydayAnomalyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		if (Security == null)
			throw new InvalidOperationException("Security not set");
		yield return (Security, CandleType);
	}

	
	protected override void OnReseted()
	{
		base.OnReseted();

		_latestPrices.Clear();
		_last = default;
		_enteredMonthKey = 0;
		_exitedMonthKey = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		if (Security == null)
			throw new InvalidOperationException("Security not set");
		base.OnStarted2(time);
		SubscribeCandles(CandleType, true, Security).Bind(c => ProcessCandle(c, Security)).Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for this security
		_latestPrices[security] = candle.ClosePrice;

		OnDaily(candle.OpenTime.Date);
	}

	private void OnDaily(DateTime d)
	{
		if (d == _last)
			return;
		_last = d;

		var monthKey = (d.Year * 100) + d.Month;
		int tdMonthEnd = TradingDaysLeftInMonth(d);
		int tdMonthStart = TradingDayNumber(d);
		bool inWindow = tdMonthEnd <= 2 || tdMonthStart <= 3;

		if (inWindow && Position == 0 && _enteredMonthKey != monthKey)
		{
			BuyMarket();
			_enteredMonthKey = monthKey;
			_exitedMonthKey = 0;
		}
		else if (!inWindow && Position > 0 && _enteredMonthKey == monthKey && _exitedMonthKey != monthKey)
		{
			SellMarket(Position);
			_exitedMonthKey = monthKey;
		}
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	private int TradingDaysLeftInMonth(DateTime d)
	{
		int cnt = 0;
		var cur = d;
		while (cur.Month == d.Month)
		{ 
			// Simple approximation: assume weekdays are trading days
			if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday) 
				cnt++; 
			cur = cur.AddDays(1); 
		}
		return cnt - 1;
	}

	private int TradingDayNumber(DateTime d)
	{
		int num = 0;
		var cur = new DateTime(d.Year, d.Month, 1);
		while (cur <= d)
		{ 
			// Simple approximation: assume weekdays are trading days
			if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday) 
				num++; 
			cur = cur.AddDays(1); 
		}
		return num;
	}
}
