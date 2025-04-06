using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Williams %R with Momentum filter.
	/// </summary>
	public class WilliamsPercentRWithMomentumStrategy : Strategy
	{
		private readonly StrategyParam<int> _williamsRPeriod;
		private readonly StrategyParam<int> _momentumPeriod;
		private readonly StrategyParam<decimal> _williamsROversold;
		private readonly StrategyParam<decimal> _williamsROverbought;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Williams %R period parameter.
		/// </summary>
		public int WilliamsRPeriod
		{
			get => _williamsRPeriod.Value;
			set => _williamsRPeriod.Value = value;
		}

		/// <summary>
		/// Momentum period parameter.
		/// </summary>
		public int MomentumPeriod
		{
			get => _momentumPeriod.Value;
			set => _momentumPeriod.Value = value;
		}

		/// <summary>
		/// Williams %R oversold level parameter.
		/// </summary>
		public decimal WilliamsROversold
		{
			get => _williamsROversold.Value;
			set => _williamsROversold.Value = value;
		}

		/// <summary>
		/// Williams %R overbought level parameter.
		/// </summary>
		public decimal WilliamsROverbought
		{
			get => _williamsROverbought.Value;
			set => _williamsROverbought.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public WilliamsPercentRWithMomentumStrategy()
		{
			_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_momentumPeriod = Param(nameof(MomentumPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Momentum Period", "Period for Momentum calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_williamsROversold = Param(nameof(WilliamsROversold), -80m)
				.SetDisplay("Williams %R Oversold", "Williams %R oversold level", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(-90, -70, 5);

			_williamsROverbought = Param(nameof(WilliamsROverbought), -20m)
				.SetDisplay("Williams %R Overbought", "Williams %R overbought level", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(-30, -10, 5);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var williamsR = new WilliamsR { Length = WilliamsRPeriod };
			var momentum = new Momentum { Length = MomentumPeriod };
			var momentumSma = new SimpleMovingAverage { Length = MomentumPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(williamsR, momentum, (candle, williamsRValue, momentumValue) =>
				{
					// Calculate momentum average
					var momentumInput = new DecimalIndicatorValue(momentumValue);
					var momentumAvg = momentumSma.Process(momentumInput).GetValue<decimal>();
					
					// Process the strategy logic
					ProcessStrategy(candle, williamsRValue, momentumValue, momentumAvg);
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, williamsR);
				DrawIndicator(area, momentum);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessStrategy(ICandleMessage candle, decimal williamsRValue, decimal momentumValue, decimal momentumAvg)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check momentum - rising or falling
			var isMomentumRising = momentumValue > momentumAvg;
			
			// Trading logic
			if (williamsRValue < WilliamsROversold && isMomentumRising && Position <= 0)
			{
				// Williams %R oversold with rising momentum - Go long
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(volume);
			}
			else if (williamsRValue > WilliamsROverbought && !isMomentumRising && Position >= 0)
			{
				// Williams %R overbought with falling momentum - Go short
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(volume);
			}
			
			// Exit logic - when Williams %R crosses the middle (-50) level
			if ((Position > 0 && williamsRValue > -50) || (Position < 0 && williamsRValue < -50))
			{
				// Close position
				ClosePosition();
			}
		}
	}
}
