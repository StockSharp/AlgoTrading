using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Pre-Holiday Strength trading strategy.
/// The strategy enters long position before a holiday and exits after the holiday.
/// </summary>
public class PreHolidayStrengthStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	// Dictionary of common holidays (month, day)
	private readonly Dictionary<(int Month, int Day), string> _holidays = new Dictionary<(int Month, int Day), string>
	{
		// US Holidays (approximate dates, some holidays like Easter vary)
		{ (1, 1), "New Year's Day" },
		{ (7, 4), "Independence Day" },
		{ (12, 25), "Christmas" },
		{ (11, 25), "Thanksgiving" }, // Approximate (4th Thursday in November)
		{ (5, 31), "Memorial Day" },  // Approximate (last Monday in May)
		{ (9, 4), "Labor Day" },	  // Approximate (first Monday in September)
		// Add more holidays as needed
	};
	
	private bool _inPreHolidayPosition;

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PreHolidayStrengthStrategy"/>.
	/// </summary>
	public PreHolidayStrengthStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection");
		
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");
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

		_inPreHolidayPosition = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		// Create a simple moving average indicator
		var sma = new SimpleMovingAverage { Length = MaPeriod };
		
		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();
		
		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
		
		// Start position protection
		StartProtection(
			takeProfit: new Unit(0), // No take profit
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
		
		// Skip if strategy is not ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		var date = candle.OpenTime;
		var tomorrow = date.AddDays(1);
		
		bool isTomorrowHoliday = IsHoliday(tomorrow);
		bool isToday = IsHoliday(date);
		
		// Enter position one day before holiday
		if (isTomorrowHoliday && !_inPreHolidayPosition && candle.ClosePrice > maValue && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			
			_inPreHolidayPosition = true;
			
			var holidayName = GetHolidayName(tomorrow);
			LogInfo($"Buy signal before holiday {holidayName}: Date={date:yyyy-MM-dd}, Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
		}
		// Exit position after holiday
		else if (_inPreHolidayPosition && isToday && Position > 0)
		{
			ClosePosition();
			
			_inPreHolidayPosition = false;
			
			var holidayName = GetHolidayName(date);
			LogInfo($"Closing position after holiday {holidayName}: Date={date:yyyy-MM-dd}, Position={Position}");
		}
	}
	
	private bool IsHoliday(DateTimeOffset date)
	{
		return _holidays.ContainsKey((date.Month, date.Day));
	}
	
	private string GetHolidayName(DateTimeOffset date)
	{
		if (_holidays.TryGetValue((date.Month, date.Day), out var holidayName))
			return holidayName;
		
		return "Unknown Holiday";
	}
}
