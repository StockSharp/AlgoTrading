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
	/// Statistical Arbitrage strategy that trades pairs of securities based on their relative mean reversion.
	/// Enters when one asset is below its mean while the other is above its mean.
	/// </summary>
	public class StatisticalArbitrageStrategy : Strategy
	{
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<Security> _secondSecurity;
		
		private SimpleMovingAverage _firstMA;
		private SimpleMovingAverage _secondMA;
		
		private decimal _lastFirstPrice;
		private decimal _lastSecondPrice;
		private decimal _entrySpread;
		private decimal _secondMAValue;
		
		/// <summary>
		/// Period for calculating moving averages.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
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
		public StatisticalArbitrageStrategy()
		{
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for calculating moving averages", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

			_lastFirstPrice = 0;
			_lastSecondPrice = 0;
			_entrySpread = 0;
			_secondMAValue = 0;

			if (SecondSecurity == null)
				throw new InvalidOperationException("Second security is not specified.");
			
			// Initialize indicators
			_firstMA = new() { Length = LookbackPeriod };
			_secondMA = new() { Length = LookbackPeriod };
			
			// Create subscriptions for both securities
			var firstSecuritySubscription = SubscribeCandles(CandleType);
			var secondSecuritySubscription = SubscribeCandles(CandleType, security: SecondSecurity);
			
			// Bind to first security candles
			firstSecuritySubscription
				.Bind(_firstMA, ProcessFirstSecurityCandle)
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
				DrawIndicator(area, _firstMA);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessFirstSecurityCandle(ICandleMessage candle, decimal firstMAValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
				
			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Store current price
			_lastFirstPrice = candle.ClosePrice;
			
			// Skip if we don't have both prices or if indicators aren't formed
			if (_lastSecondPrice == 0 || !_firstMA.IsFormed || !_secondMA.IsFormed)
				return;
			
			// Get last second MA value stored earlier
			decimal secondMAValue = _secondMAValue;
			
			// Trading logic
			bool isFirstBelowMA = _lastFirstPrice < firstMAValue;
			bool isSecondAboveMA = _lastSecondPrice > secondMAValue;
			bool isFirstAboveMA = _lastFirstPrice > firstMAValue;
			bool isSecondBelowMA = _lastSecondPrice < secondMAValue;
			
			decimal currentSpread = _lastFirstPrice - _lastSecondPrice;
			
			// Long signal: First asset below MA, Second asset above MA
			if (isFirstBelowMA && isSecondAboveMA)
			{
				// If we're not already in a long position
				if (Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_entrySpread = currentSpread;
					LogInfo($"Long Signal: {Security.Code}({_lastFirstPrice:F4}) < MA({firstMAValue:F4}) && " + 
							$"{SecondSecurity.Code}({_lastSecondPrice:F4}) > MA({secondMAValue:F4})");
					
					// Note: In a real implementation, you would also place a sell order
					// for the second security here, using a different strategy instance or connector
				}
			}
			// Short signal: First asset above MA, Second asset below MA
			else if (isFirstAboveMA && isSecondBelowMA)
			{
				// If we're not already in a short position
				if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_entrySpread = currentSpread;
					LogInfo($"Short Signal: {Security.Code}({_lastFirstPrice:F4}) > MA({firstMAValue:F4}) && " + 
							$"{SecondSecurity.Code}({_lastSecondPrice:F4}) < MA({secondMAValue:F4})");
					
					// Note: In a real implementation, you would also place a buy order
					// for the second security here, using a different strategy instance or connector
				}
			}
			// Exit signals
			else if ((Position > 0 && isFirstAboveMA) || (Position < 0 && isFirstBelowMA))
			{
				// Exit position when first asset crosses its moving average
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit Long: {Security.Code}({_lastFirstPrice:F4}) > MA({firstMAValue:F4})");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit Short: {Security.Code}({_lastFirstPrice:F4}) < MA({firstMAValue:F4})");
				}
			}
		}
		
		private void ProcessSecondSecurityCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Store current price
			_lastSecondPrice = candle.ClosePrice;
		
			// Process through MA indicator and store last value
			var maValue = _secondMA.Process(candle.ClosePrice, candle.ServerTime, candle.State == CandleStates.Finished);
			_secondMAValue = maValue.ToDecimal();
		}
	}
}
