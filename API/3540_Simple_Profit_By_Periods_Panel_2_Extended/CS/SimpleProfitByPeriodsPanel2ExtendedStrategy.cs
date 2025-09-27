namespace StockSharp.Samples.Strategies;

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

using System.Globalization;

/// <summary>
/// Displays daily, weekly, and monthly realized results in the strategy comment.
/// Mirrors the Simple Profit By Periods panel that periodically scans trade history in MetaTrader.
/// </summary>
public class SimpleProfitByPeriodsPanel2ExtendedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly List<PeriodTrade> _tradeHistory = new();
	
	private decimal _lastRealizedPnL;
	
	/// <summary>
	/// Initializes a new instance of <see cref="SimpleProfitByPeriodsPanel2ExtendedStrategy"/>.
	/// </summary>
	public SimpleProfitByPeriodsPanel2ExtendedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to refresh the dashboard", "General");
	}
	
	/// <summary>
	/// Candle series used to periodically refresh the statistics.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		
		_tradeHistory.Clear();
		_lastRealizedPnL = 0m;
		Comment = string.Empty;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_lastRealizedPnL = PnLManager?.RealizedPnL ?? PnL;
		
		SubscribeCandles(CandleType)
				.Bind(ProcessCandle)
				.Start();
		
		UpdateDashboard(time);
	}
	
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);
		
		var tradeInfo = trade.Trade;
		if (tradeInfo == null)
			return;
		
		if (tradeInfo.Security != Security)
			return;
		
		if (tradeInfo.Volume <= 0m)
			return;
		
		var realizedPnL = PnLManager?.RealizedPnL ?? PnL;
		var delta = realizedPnL - _lastRealizedPnL;
		_lastRealizedPnL = realizedPnL;
		
		var time = tradeInfo.Time == default ? CurrentTime : tradeInfo.Time;
		
		_tradeHistory.Add(new PeriodTrade(time, delta));
		
		UpdateDashboard(time);
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		UpdateDashboard(candle.CloseTime);
	}
	
	private void UpdateDashboard(DateTimeOffset now)
	{
		var dayStart = GetTradingDayStart(now);
		var weekStart = GetWeekStart(dayStart);
		var monthStart = new DateTimeOffset(dayStart.Year, dayStart.Month, 1, 0, 0, 0, dayStart.Offset);
		
		CleanupHistory(weekStart.AddDays(-7));
		
		var dayProfit = 0m;
		var weekProfit = 0m;
		var monthProfit = 0m;
		var dayDeals = 0;
		var weekDeals = 0;
		var monthDeals = 0;
		
		for (var i = 0; i < _tradeHistory.Count; i++)
		{
			var record = _tradeHistory[i];
			if (record.Time >= dayStart)
			{
				dayProfit += record.Profit;
				dayDeals++;
			}
			
			if (record.Time >= weekStart)
			{
				weekProfit += record.Profit;
				weekDeals++;
			}
			
			if (record.Time >= monthStart)
			{
				monthProfit += record.Profit;
				monthDeals++;
			}
		}
		
		var currentBalance = Portfolio?.CurrentValue ?? 0m;
		
		var dayPercent = CalculatePercent(currentBalance, currentBalance - dayProfit);
		var weekPercent = CalculatePercent(currentBalance, currentBalance - weekProfit);
		var monthPercent = CalculatePercent(currentBalance, currentBalance - monthProfit);
		
		var currency = Portfolio?.Currency ?? Security?.Currency ?? string.Empty;
		var currencyPrefix = currency.IsEmpty() ? string.Empty : currency + " ";
		
		Comment = string.Join(Environment.NewLine,
			FormatProfitLine("Daily", currencyPrefix, dayProfit, dayPercent),
			FormatProfitLine("Weekly", currencyPrefix, weekProfit, weekPercent),
			FormatProfitLine("Monthly", currencyPrefix, monthProfit, monthPercent),
			FormatDealsLine("Daily", dayDeals),
			FormatDealsLine("Weekly", weekDeals),
			FormatDealsLine("Monthly", monthDeals));
	}
	
	private static DateTimeOffset GetTradingDayStart(DateTimeOffset now)
	{
		var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
		
		return dayStart.DayOfWeek switch
		{
			DayOfWeek.Saturday => dayStart.AddDays(-1),
			DayOfWeek.Sunday => dayStart.AddDays(-2),
			_ => dayStart
		};
	}
	
	private static DateTimeOffset GetWeekStart(DateTimeOffset dayStart)
	{
		return dayStart.DayOfWeek switch
		{
			DayOfWeek.Monday => dayStart,
			DayOfWeek.Tuesday => dayStart.AddDays(-1),
			DayOfWeek.Wednesday => dayStart.AddDays(-2),
			DayOfWeek.Thursday => dayStart.AddDays(-3),
			DayOfWeek.Friday => dayStart.AddDays(-4),
			DayOfWeek.Saturday => dayStart.AddDays(-5),
			DayOfWeek.Sunday => dayStart.AddDays(-6),
			_ => dayStart
		};
	}
	
	private void CleanupHistory(DateTimeOffset minTime)
	{
		var index = 0;
		while (index < _tradeHistory.Count)
		{
			if (_tradeHistory[index].Time >= minTime)
			{
				index++;
			}
			else
			{
				_tradeHistory.RemoveAt(index);
			}
		}
	}
	
	private static decimal CalculatePercent(decimal currentBalance, decimal previousBalance)
	{
		if (previousBalance <= 0m)
			return 0m;
		
		return (currentBalance / previousBalance - 1m) * 100m;
	}
	
	private static string FormatProfitLine(string period, string currencyPrefix, decimal profit, decimal percent)
	{
		var profitText = profit.ToString("0.00", CultureInfo.InvariantCulture);
		var percentText = percent.ToString("0.00", CultureInfo.InvariantCulture);
		return $"{period} Profit:   {currencyPrefix}{profitText} ({percentText}%)";
	}
	
	private static string FormatDealsLine(string period, int deals)
	{
		return $"{period} Deals:    {deals}";
	}
	
	private readonly struct PeriodTrade
	{
		public PeriodTrade(DateTimeOffset time, decimal profit)
		{
			Time = time;
			Profit = profit;
		}
		
		public DateTimeOffset Time { get; }
		
		public decimal Profit { get; }
	}
}

