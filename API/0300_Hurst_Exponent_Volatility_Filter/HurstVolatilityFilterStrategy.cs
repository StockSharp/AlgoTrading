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
	/// Strategy using the Hurst exponent to identify mean-reversion markets
	/// with an ATR-based volatility filter to confirm entry signals
	/// </summary>
	public class HurstVolatilityFilterStrategy : Strategy
	{
		// Strategy parameters
		private readonly StrategyParam<int> _hurstPeriodParam;
		private readonly StrategyParam<int> _maPeriodParam;
		private readonly StrategyParam<int> _atrPeriodParam;
		private readonly StrategyParam<decimal> _stopLossParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		// Indicators
		private SimpleMovingAverage _sma;
		private AverageTrueRange _atr;
		private HurstExponent _hurstExponent;

		// Internal state variables
		private decimal _averageAtr;
		private bool _isLongPosition;
		private decimal _positionEntryPrice;

		/// <summary>
		/// Period for Hurst exponent calculation
		/// </summary>
		public int HurstPeriod
		{
			get => _hurstPeriodParam.Value;
			set => _hurstPeriodParam.Value = value;
		}

		/// <summary>
		/// Period for Moving Average calculation
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriodParam.Value;
			set => _maPeriodParam.Value = value;
		}

		/// <summary>
		/// Period for ATR calculation
		/// </summary>
		public int ATRPeriod
		{
			get => _atrPeriodParam.Value;
			set => _atrPeriodParam.Value = value;
		}

		/// <summary>
		/// Stop loss as percentage of entry price
		/// </summary>
		public decimal StopLoss
		{
			get => _stopLossParam.Value;
			set => _stopLossParam.Value = value;
		}

		/// <summary>
		/// Candle type for strategy operation
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Constructor with default parameters
		/// </summary>
		public HurstVolatilityFilterStrategy()
		{
			_hurstPeriodParam = Param(nameof(HurstPeriod), 100)
				.SetDisplay("Hurst Period", "Period for calculating Hurst exponent", "Indicators")
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(50, 150, 10);

			_maPeriodParam = Param(nameof(MAPeriod), 20)
				.SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators")
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_atrPeriodParam = Param(nameof(ATRPeriod), 14)
				.SetDisplay("ATR Period", "Period for calculating Average True Range", "Indicators")
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_stopLossParam = Param(nameof(StopLoss), 2.0m)
				.SetDisplay("Stop Loss", "Stop loss percentage from entry price", "Risk Management")
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
			
			// Reset state
			_isLongPosition = false;
			_positionEntryPrice = 0;
			_averageAtr = 0;

			// Create indicators
			_sma = new() { Length = MAPeriod };
			_atr = new() { Length = ATRPeriod };
			_hurstExponent = new()
			{
				// Configure Hurst exponent displacement indicator
				Length = HurstPeriod
			};

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to candle events
			subscription
				.Bind(_sma, _atr, ProcessCandle)
				.Start();
			
			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _sma);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}
			
			// Enable position protection with stop loss
			StartProtection(
				takeProfit: new Unit(0), // No take-profit, using custom exit conditions
				stopLoss: new Unit(StopLoss, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal? smaValue, decimal? atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Process Hurst exponent
			var hurstValue = CalculateHurstExponentValue(candle);
			
			// Update average ATR
			UpdateAverageAtr(atrValue);
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Manage open positions
			if (Position != 0)
			{
				CheckExitConditions(candle.ClosePrice, smaValue);
			}
			else
			{
				CheckEntryConditions(candle.ClosePrice, smaValue, hurstValue, atrValue);
			}
		}
		
		private decimal? CalculateHurstExponentValue(ICandleMessage candle)
		{
			// In a real implementation, this would use R/S analysis or other methods
			// to calculate the Hurst exponent. For this example, we'll use a placeholder
			// logic that estimates the Hurst exponent based on recent price behavior.
			
			// Process current price through the displacement indicator
			var hurstValue = _hurstExponent.Process(candle).ToDecimal();
			
			// For demonstration purposes - in a real implementation you'd use
			// a proper Hurst exponent calculation library or algorithm
			// This is just a placeholder that gives a value between 0 and 1
			return hurstValue;
		}
		
		private void UpdateAverageAtr(decimal atrValue)
		{
			if (_averageAtr == 0)
			{
				_averageAtr = atrValue;
			}
			else
			{
				// Simple exponential smoothing
				_averageAtr = 0.9m * _averageAtr + 0.1m * atrValue;
			}
		}
		
		private void CheckEntryConditions(decimal price, decimal smaValue, decimal hurstValue, decimal atrValue)
		{
			// Check for mean-reversion markets (Hurst < 0.5)
			if (hurstValue < 0.5m)
			{
				// Check volatility is lower than average (filtered condition)
				if (atrValue < _averageAtr)
				{
					// Long signal: Price is below average in mean-reverting market with low volatility
					if (price < smaValue)
					{
						EnterLong(price);
					}
					// Short signal: Price is above average in mean-reverting market with low volatility
					else if (price > smaValue)
					{
						EnterShort(price);
					}
				}
			}
		}
		
		private void CheckExitConditions(decimal price, decimal smaValue)
		{
			// Mean reversion exit strategy
			if (_isLongPosition && price > smaValue)
			{
				ExitPosition(price);
			}
			else if (!_isLongPosition && price < smaValue)
			{
				ExitPosition(price);
			}
		}
		
		private void EnterLong(decimal price)
		{
			// Create and send a buy market order
			var volume = Volume;
			BuyMarket(volume);
			
			// Update internal state
			_isLongPosition = true;
			_positionEntryPrice = price;
			
			LogInfo($"Enter LONG at {price}, Hurst shows mean-reversion market with low volatility");
		}
		
		private void EnterShort(decimal price)
		{
			// Create and send a sell market order
			var volume = Volume;
			SellMarket(volume);
			
			// Update internal state
			_isLongPosition = false;
			_positionEntryPrice = price;
			
			LogInfo($"Enter SHORT at {price}, Hurst shows mean-reversion market with low volatility");
		}
		
		private void ExitPosition(decimal price)
		{
			// Close position at market
			ClosePosition();
			
			// Calculate profit/loss for logging
			decimal pnl = _isLongPosition 
				? (price - _positionEntryPrice) / _positionEntryPrice * 100 
				: (_positionEntryPrice - price) / _positionEntryPrice * 100;
			
			LogInfo($"Exit position at {price}, P&L: {pnl:F2}%, Mean reversion complete");
			
			// Reset position tracking
			_positionEntryPrice = 0;
		}
		
		/// <inheritdoc />
		protected override void OnStopped()
		{
			// Close any open positions when strategy stops
			if (Position != 0)
			{
				ClosePosition();
			}
			
			base.OnStopped();
		}
	}
}
