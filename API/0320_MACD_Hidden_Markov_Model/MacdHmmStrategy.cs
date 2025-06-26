using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// MACD strategy with Hidden Markov Model for state detection.
	/// </summary>
	public class MacdHmmStrategy : Strategy
	{
		private readonly StrategyParam<int> _macdFast;
		private readonly StrategyParam<int> _macdSlow;
		private readonly StrategyParam<int> _macdSignal;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _hmmHistoryLength;
		
		private MovingAverageConvergenceDivergence _macd;
		
		// Hidden Markov Model states
		private enum MarketState
		{
			Bullish,
			Neutral,
			Bearish
		}
		
		private MarketState _currentState = MarketState.Neutral;
		
		// Data for HMM calculations
		private readonly SynchronizedList<decimal> _priceChanges = [];
		private readonly SynchronizedList<decimal> _volumes = [];
		private decimal _prevPrice;
		
		/// <summary>
		/// MACD fast period.
		/// </summary>
		public int MacdFast
		{
			get => _macdFast.Value;
			set => _macdFast.Value = value;
		}

		/// <summary>
		/// MACD slow period.
		/// </summary>
		public int MacdSlow
		{
			get => _macdSlow.Value;
			set => _macdSlow.Value = value;
		}

		/// <summary>
		/// MACD signal period.
		/// </summary>
		public int MacdSignal
		{
			get => _macdSignal.Value;
			set => _macdSignal.Value = value;
		}

		/// <summary>
		/// Candle type to use for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Length of history for Hidden Markov Model.
		/// </summary>
		public int HmmHistoryLength
		{
			get => _hmmHistoryLength.Value;
			set => _hmmHistoryLength.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MacdHmmStrategy"/>.
		/// </summary>
		public MacdHmmStrategy()
		{
			_macdFast = Param(nameof(MacdFast), 12)
				.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(8, 20, 2);

			_macdSlow = Param(nameof(MacdSlow), 26)
				.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 2);

			_macdSignal = Param(nameof(MacdSignal), 9)
				.SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 15, 1);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).ToTimeFrameDataType())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
				
			_hmmHistoryLength = Param(nameof(HmmHistoryLength), 100)
				.SetDisplay("HMM History Length", "Length of history for Hidden Markov Model", "HMM Parameters")
				.SetCanOptimize(true)
				.SetOptimize(50, 200, 10);
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

			// Create MACD indicator
			_macd = new MovingAverageConvergenceDivergence
			{
				FastMa = new ExponentialMovingAverage { Length = MacdFast },
				SlowMa = new ExponentialMovingAverage { Length = MacdSlow },
				SignalMa = new ExponentialMovingAverage { Length = MacdSignal }
			};

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(_macd, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _macd);
				DrawOwnTrades(area);
			}
			
			// Setup position protection
			StartProtection(
				new Unit(2, UnitTypes.Percent), 
				new Unit(2, UnitTypes.Percent)
			);
		}
		
		private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Update HMM data
			UpdateHmmData(candle);
			
			// Determine market state using HMM
			CalculateMarketState();
			
			// Generate trade signals based on MACD and HMM state
			if (macdValue > signalValue && _currentState == MarketState.Bullish && Position <= 0)
			{
				// Buy signal - MACD above signal line and bullish state
				BuyMarket(Volume);
				LogInfo($"Buy Signal: MACD ({macdValue:F6}) > Signal ({signalValue:F6}) in Bullish state");
			}
			else if (macdValue < signalValue && _currentState == MarketState.Bearish && Position >= 0)
			{
				// Sell signal - MACD below signal line and bearish state
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Sell Signal: MACD ({macdValue:F6}) < Signal ({signalValue:F6}) in Bearish state");
			}
			else if ((Position > 0 && (_currentState == MarketState.Neutral || _currentState == MarketState.Bearish)) ||
					 (Position < 0 && (_currentState == MarketState.Neutral || _currentState == MarketState.Bullish)))
			{
				// Exit position if market state changes
				ClosePosition();
				LogInfo($"Exit Position: Market state changed to {_currentState}");
			}
		}
		
		private void UpdateHmmData(ICandleMessage candle)
		{
			// Calculate price change
			if (_prevPrice > 0)
			{
				decimal priceChange = candle.ClosePrice - _prevPrice;
				_priceChanges.Add(priceChange);
				_volumes.Add(candle.TotalVolume);
				
				// Maintain the desired history length
				while (_priceChanges.Count > HmmHistoryLength)
				{
					_priceChanges.RemoveAt(0);
					_volumes.RemoveAt(0);
				}
			}
			
			_prevPrice = candle.ClosePrice;
		}
		
		private void CalculateMarketState()
		{
			// Only perform state calculation when we have enough data
			if (_priceChanges.Count < 10)
				return;
				
			// Simple HMM approximation using recent price changes and volume patterns
			// Note: This is a simplified implementation - a real HMM would use proper state transition probabilities
			
			// Calculate statistics of recent price changes
			var recentChanges = _priceChanges.Skip(Math.Max(0, _priceChanges.Count - 10)).ToList();
			var positiveChanges = recentChanges.Count(c => c > 0);
			var negativeChanges = recentChanges.Count(c => c < 0);
			
			// Calculate average volume for up and down days
			decimal upVolume = 0;
			decimal downVolume = 0;
			int upCount = 0;
			int downCount = 0;
			
			for (int i = Math.Max(0, _priceChanges.Count - 10); i < _priceChanges.Count; i++)
			{
				if (_priceChanges[i] > 0)
				{
					upVolume += _volumes[i];
					upCount++;
				}
				else if (_priceChanges[i] < 0)
				{
					downVolume += _volumes[i];
					downCount++;
				}
			}
			
			upVolume = upCount > 0 ? upVolume / upCount : 0;
			downVolume = downCount > 0 ? downVolume / downCount : 0;
			
			// Determine market state based on price change direction and volume
			if (positiveChanges >= 7 || (positiveChanges >= 6 && upVolume > downVolume * 1.5m))
			{
				_currentState = MarketState.Bullish;
			}
			else if (negativeChanges >= 7 || (negativeChanges >= 6 && downVolume > upVolume * 1.5m))
			{
				_currentState = MarketState.Bearish;
			}
			else
			{
				_currentState = MarketState.Neutral;
			}
			
			LogInfo($"Market State: {_currentState}, Positive Changes: {positiveChanges}, Negative Changes: {negativeChanges}");
		}
	}
}