using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Implementation of January Effect trading strategy.
	/// The strategy enters long position in January and exits in February.
	/// </summary>
	public class JanuaryEffectStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<DataType> _candleType;

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
		/// Initializes a new instance of the <see cref="JanuaryEffectStrategy"/>.
		/// </summary>
		public JanuaryEffectStrategy()
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
			var month = date.Month;
			
			// January - BUY signal (Month = 1)
			if (month == 1 && Position <= 0 && candle.ClosePrice > maValue)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Buy signal in January: Date={date:yyyy-MM-dd}, Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
			}
			// February - EXIT signal (Month = 2)
			else if (month == 2 && Position > 0)
			{
				ClosePosition();
				LogInfo($"Closing position in February: Date={date:yyyy-MM-dd}, Position={Position}");
			}
		}
	}
}
