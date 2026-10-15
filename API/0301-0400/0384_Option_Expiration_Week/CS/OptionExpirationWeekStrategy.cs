// OptionExpirationWeekStrategy.cs — candle-triggered
// Long ETF only during option‑expiration week (ending 3rd Friday).
// Trigger: daily candle close.
// Date: 2 Aug 2025

using System;
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
/// Goes long the specified ETF only during option‑expiration week.
/// </summary>
public class OptionExpirationWeekStrategy : Strategy
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
	private int _enteredMonthKey;
	private int _exitedMonthKey;

	public OptionExpirationWeekStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		if (Security == null)
			throw new InvalidOperationException("ETF not set.");

		return [(Security, CandleType)];
	}

	
	protected override void OnReseted()
	{
		base.OnReseted();

		_latestPrices.Clear();
		_enteredMonthKey = 0;
		_exitedMonthKey = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("ETF cannot be null.");

		SubscribeCandles(CandleType, true, Security)
			.Bind(c => ProcessCandle(c, Security))
			.Start();
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
		var monthKey = (d.Year * 100) + d.Month;
		bool inExp = IsOptionExpWeek(d);

		if (inExp && Position == 0 && _enteredMonthKey != monthKey)
		{
			BuyMarket();
			_enteredMonthKey = monthKey;
			_exitedMonthKey = 0;
		}
		else if (!inExp && Position > 0 && _enteredMonthKey == monthKey && _exitedMonthKey != monthKey)
		{
			SellMarket(Position);
			_exitedMonthKey = monthKey;
		}
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	private bool IsOptionExpWeek(DateTime d)
	{
		// find third Friday
		var third = new DateTime(d.Year, d.Month, 1);
		while (third.DayOfWeek != DayOfWeek.Friday)
			third = third.AddDays(1);
		third = third.AddDays(14);
		return d >= third.AddDays(-4) && d <= third;
	}
}
