using System;
using System.Collections.Generic;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Statistical Pairs Trading strategy.
	/// Trades the spread between two correlated assets, entering positions when
	/// the spread deviates significantly from its mean.
	/// </summary>
	public class PairsTradingStrategy : Strategy
	{
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<Security> _secondSecurity;

		private SimpleMovingAverage _spreadMA;
		private StandardDeviation _spreadStdDev;
		
		private decimal _spread;
		private decimal _lastSecondPrice;

		/// <summary>
		/// Period for calculating mean and standard deviation of the spread.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}
		
		/// <summary>
		/// Number of standard deviations for entry signals.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
		}
		
		/// <summary>
		/// Stop-loss percentage parameter.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Second security in the pair.
		/// </summary>
		public Security SecondSecurity
		{
			get => _secondSecurity.Value;
			set => _secondSecurity.Value = value;
		}
		
		/// <summary>
		/// Constructor.
		/// </summary>
		public PairsTradingStrategy()
		{
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for calculating spread mean and standard deviation", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 40, 5);
				
			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Multiplier", "Number of standard deviations for entry signals", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop-loss %", "Stop-loss as percentage of spread at entry", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
				
			_secondSecurity = Param<Security>(nameof(SecondSecurity))
				.SetDisplay("Second Security", "Second security in the pair", "General")
				.SetRequired();
		}
		
		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return
			[
				(Security, CandleType),
				(SecondSecurity, CandleType)
			];
		}
		
		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			if (SecondSecurity == null)
				throw new InvalidOperationException("Second security is not specified.");
			
			// Initialize indicators
			_spreadMA = new() { Length = LookbackPeriod };
			_spreadStdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Create subscriptions for both securities
			var firstSecuritySubscription = SubscribeCandles(CandleType);
			var secondSecuritySubscription = SubscribeCandles(CandleType, security: SecondSecurity);
			
			// Bind to first security candles
			firstSecuritySubscription
				.Bind(ProcessFirstSecurityCandle)
				.Start();
			
			// Bind to second security candles
			secondSecuritySubscription
				.Bind(ProcessSecondSecurityCandle)
				.Start();
			
			// Enable position protection with stop-loss
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop-loss as percentage
			);
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, firstSecuritySubscription);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessFirstSecurityCandle(ICandleMessage candle)
		{
			// Skip if we don't have price for the second security yet
			if (_lastSecondPrice == 0)
				return;
				
			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Calculate the spread: Asset1 - Asset2
			_spread = candle.ClosePrice - _lastSecondPrice;
			
			// Process the spread through indicators
			var maValue = _spreadMA.Process(_spread, candle.ServerTime, candle.State == CandleStates.Finished);
			var stdDevValue = _spreadStdDev.Process(_spread, candle.ServerTime, candle.State == CandleStates.Finished);
			
			// Skip until indicators are formed
			if (!_spreadMA.IsFormed || !_spreadStdDev.IsFormed)
				return;
			
			decimal spreadMean = maValue.ToDecimal();
			decimal spreadStdDev = stdDevValue.ToDecimal();
			
			// Calculate entry thresholds
			decimal upperThreshold = spreadMean + (spreadStdDev * DeviationMultiplier);
			decimal lowerThreshold = spreadMean - (spreadStdDev * DeviationMultiplier);
			
			// Trading logic
			if (_spread < lowerThreshold)
			{
				// Spread is below lower threshold: 
				// Buy Asset1 (Security), Sell Asset2 (SecondSecurity)
				if (Position <= 0)
				{
					// Close any existing position and enter new position
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long Signal: Spread({_spread:F4}) < Lower Threshold({lowerThreshold:F4})");
					
					// Note: In a real implementation, you would also place a sell order
					// for the second security here, using a different strategy instance or connector
				}
			}
			else if (_spread > upperThreshold)
			{
				// Spread is above upper threshold:
				// Sell Asset1 (Security), Buy Asset2 (SecondSecurity)
				if (Position >= 0)
				{
					// Close any existing position and enter new position
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short Signal: Spread({_spread:F4}) > Upper Threshold({upperThreshold:F4})");
					
					// Note: In a real implementation, you would also place a buy order
					// for the second security here, using a different strategy instance or connector
				}
			}
			else if ((_spread > spreadMean && Position > 0) || 
					(_spread < spreadMean && Position < 0))
			{
				// Exit signals: Spread returned to the mean
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit Long: Spread({_spread:F4}) > Mean({spreadMean:F4})");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit Short: Spread({_spread:F4}) < Mean({spreadMean:F4})");
				}
			}
		}
		
		private void ProcessSecondSecurityCandle(ICandleMessage candle)
		{
			// Store the close price of the second security for spread calculation
			_lastSecondPrice = candle.ClosePrice;
		}
	}
}
