using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Implementation of Post-Holiday Weakness trading strategy.
	/// The strategy enters short position after a holiday and exits on Friday.
	/// </summary>
	public class PostHolidayWeaknessStrategy : Strategy
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
		
		private bool _inPostHolidayPosition;

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
		/// Initializes a new instance of the <see cref="PostHolidayWeaknessStrategy"/>.
		/// </summary>
		public PostHolidayWeaknessStrategy()
		{
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection");
			
			_maPeriod = Param(nameof(MaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");
			
			_inPostHolidayPosition = false;
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
			
			// Create a simple moving average indicator
			var sma = new StockSharp.Algo.Indicators.SimpleMovingAverage { Length = MaPeriod };
			
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

		private void ProcessCandle(ICandleMessage candle, decimal? maValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Skip if strategy is not ready
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			var date = candle.OpenTime;
			var yesterday = date.AddDays(-1);
			var dayOfWeek = date.DayOfWeek;
			
			bool wasYesterdayHoliday = IsHoliday(yesterday);
			
			// Enter position after holiday
			if (wasYesterdayHoliday && !_inPostHolidayPosition && candle.ClosePrice < maValue && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				_inPostHolidayPosition = true;
				
				var holidayName = GetHolidayName(yesterday);
				LogInfo($"Sell signal after holiday {holidayName}: Date={date:yyyy-MM-dd}, Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
			}
			// Exit position on Friday
			else if (_inPostHolidayPosition && dayOfWeek == DayOfWeek.Friday && Position < 0)
			{
				ClosePosition();
				
				_inPostHolidayPosition = false;
				
				LogInfo($"Closing position on Friday: Date={date:yyyy-MM-dd}, Position={Position}");
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
}
