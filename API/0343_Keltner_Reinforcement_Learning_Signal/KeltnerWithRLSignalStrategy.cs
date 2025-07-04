using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Keltner with Reinforcement Learning Signal strategy.
	/// Entry condition:
	/// Long: Price > EMA + k*ATR && RL_Signal = Buy
	/// Short: Price < EMA - k*ATR && RL_Signal = Sell
	/// Exit condition:
	/// Long: Price < EMA
	/// Short: Price > EMA
	/// </summary>
	public class KeltnerWithRLSignalStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<decimal> _stopLossAtr;
		private readonly StrategyParam<DataType> _candleType;
		
		private enum RLSignal
		{
			None,
			Buy,
			Sell
		}
		
		private RLSignal _currentSignal = RLSignal.None;
		
		// State variables for RL
		private decimal _lastPrice;
		private decimal _previousEma;
		private decimal _previousAtr;
		private decimal _previousPrice;
		private decimal _previousSignalPrice;
		private int _consecutiveWins;
		private int _consecutiveLosses;
		
		/// <summary>
		/// EMA period.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}
		
		/// <summary>
		/// ATR period.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}
		
		/// <summary>
		/// ATR multiplier for Keltner channel.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}
		
		/// <summary>
		/// Stop loss in ATR multiples.
		/// </summary>
		public decimal StopLossAtr
		{
			get => _stopLossAtr.Value;
			set => _stopLossAtr.Value = value;
		}
		
		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Constructor with default parameters.
		/// </summary>
		public KeltnerWithRLSignalStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Period", "Period for the exponential moving average", "Keltner Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for the average true range", "Keltner Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);
				
			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channels", "Keltner Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m);
				
			_stopLossAtr = Param(nameof(StopLossAtr), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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
			
			// Initialize RL state variables
			_consecutiveWins = 0;
			_consecutiveLosses = 0;
			
			// Create Keltner Channels using EMA and ATR
			var keltner = new KeltnerChannels
			{
				Length = EmaPeriod,
				Multiplier = AtrMultiplier
			};
			
			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(keltner, ProcessCandle)
				.Start();
			
			// Subscribe to own trades for reinforcement learning feedback
			this
				.WhenOwnTradeReceived()
				.Do(ProcessOwnTrade)
				.Apply(this);
			
			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, keltner);
				DrawOwnTrades(area);
			}
		}
		
		/// <summary>
		/// Process each candle and Keltner Channel values.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue keltnerValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Extract Keltner Channel values
			var upperBand = keltnerValue.GetValue<Tuple<decimal, decimal, decimal>>().Item1;
			var middleBand = keltnerValue.GetValue<Tuple<decimal, decimal, decimal>>().Item2; // This is the EMA
			var lowerBand = keltnerValue.GetValue<Tuple<decimal, decimal, decimal>>().Item3;
			
			// Calculate current ATR value (upper - middle)/multiplier
			var currentAtr = (upperBand - middleBand) / AtrMultiplier;
			
			// Update price and RL state
			_lastPrice = candle.ClosePrice;
			
			// Generate RL signal based on current state
			UpdateRLSignal(candle, middleBand, currentAtr);
			
			// Trading logic
			var price = candle.ClosePrice;
			var priceAboveUpperBand = price > upperBand;
			var priceBelowLowerBand = price < lowerBand;
			
			// Entry conditions
			
			// Long entry: Price above upper band and RL signal is Buy
			if (priceAboveUpperBand && _currentSignal == RLSignal.Buy && Position <= 0)
			{
				LogInfo($"Long signal: Price {price} > Upper Band {upperBand}, RL Signal = Buy");
				BuyMarket(Volume);
				_previousSignalPrice = price;
			}
			// Short entry: Price below lower band and RL signal is Sell
			else if (priceBelowLowerBand && _currentSignal == RLSignal.Sell && Position >= 0)
			{
				LogInfo($"Short signal: Price {price} < Lower Band {lowerBand}, RL Signal = Sell");
				SellMarket(Volume);
				_previousSignalPrice = price;
			}
			
			// Exit conditions
			
			// Exit long: Price drops below EMA (middle band)
			if (Position > 0 && price < middleBand)
			{
				LogInfo($"Exit long: Price {price} < EMA {middleBand}");
				SellMarket(Math.Abs(Position));
			}
			// Exit short: Price rises above EMA (middle band)
			else if (Position < 0 && price > middleBand)
			{
				LogInfo($"Exit short: Price {price} > EMA {middleBand}");
				BuyMarket(Math.Abs(Position));
			}
			
			// Set stop loss based on ATR
			ApplyAtrStopLoss(price, currentAtr);
			
			// Update previous values for next iteration
			_previousEma = middleBand;
			_previousAtr = currentAtr;
			_previousPrice = price;
		}
		
		/// <summary>
		/// Update Reinforcement Learning signal based on current state.
		/// This is a simplified RL model (Q-learning) for demonstration.
		/// In a real system, this would likely be a more sophisticated model.
		/// </summary>
		private void UpdateRLSignal(ICandleMessage candle, decimal ema, decimal atr)
		{
			// Features for RL decision:
			// 1. Price position relative to EMA
			bool priceAboveEma = candle.ClosePrice > ema;
			
			// 2. Recent momentum
			bool priceIncreasing = candle.ClosePrice > _previousPrice;
			
			// 3. Volatility
			bool volatilityIncreasing = atr > _previousAtr;
			
			// 4. Candle pattern (bullish/bearish)
			bool bullishCandle = candle.ClosePrice > candle.OpenPrice;
			
			// 5. Previous trade outcome
			// More conservative after losses, more aggressive after wins
			bool aggressiveMode = _consecutiveWins > _consecutiveLosses;
			
			// Simplified Q-learning decision matrix
			if (bullishCandle && priceAboveEma && (priceIncreasing || aggressiveMode))
			{
				_currentSignal = RLSignal.Buy;
				LogInfo("RL Signal: Buy");
			}
			else if (!bullishCandle && !priceAboveEma && (!priceIncreasing || aggressiveMode))
			{
				_currentSignal = RLSignal.Sell;
				LogInfo("RL Signal: Sell");
			}
			else
			{
				// If conditions are mixed, maintain current signal or go neutral
				if (volatilityIncreasing)
				{
					// High volatility might warrant reducing exposure
					_currentSignal = RLSignal.None;
					LogInfo("RL Signal: None (high volatility)");
				}
				// Otherwise keep current signal
			}
		}
		
		/// <summary>
		/// Process own trades for reinforcement learning feedback.
		/// </summary>
		private void ProcessOwnTrade(MyTrade trade)
		{
			// Skip if we don't have a previous signal price (first trade)
			if (_previousSignalPrice == 0)
				return;
				
			// Determine if the trade was profitable
			bool profitable;
			
			if (trade.Order.Side == Sides.Buy)
			{
				// For buys, it's profitable if current price > entry price
				profitable = _lastPrice > trade.Trade.Price;
			}
			else
			{
				// For sells, it's profitable if current price < entry price
				profitable = _lastPrice < trade.Trade.Price;
			}
			
			// Update consecutive win/loss counters for RL state
			if (profitable)
			{
				_consecutiveWins++;
				_consecutiveLosses = 0;
				LogInfo($"Profitable trade: Win streak = {_consecutiveWins}");
			}
			else
			{
				_consecutiveLosses++;
				_consecutiveWins = 0;
				LogInfo($"Unprofitable trade: Loss streak = {_consecutiveLosses}");
			}
		}
		
		/// <summary>
		/// Apply ATR-based stop loss.
		/// </summary>
		private void ApplyAtrStopLoss(decimal price, decimal atr)
		{
			// Dynamic stop loss based on ATR
			if (Position > 0) // Long position
			{
				var stopLevel = price - (StopLossAtr * atr);
				if (_lastPrice < stopLevel)
				{
					LogInfo($"ATR Stop Loss triggered for long position: Current {_lastPrice} < Stop {stopLevel}");
					SellMarket(Math.Abs(Position));
				}
			}
			else if (Position < 0) // Short position
			{
				var stopLevel = price + (StopLossAtr * atr);
				if (_lastPrice > stopLevel)
				{
					LogInfo($"ATR Stop Loss triggered for short position: Current {_lastPrice} > Stop {stopLevel}");
					BuyMarket(Math.Abs(Position));
				}
			}
		}
	}
}