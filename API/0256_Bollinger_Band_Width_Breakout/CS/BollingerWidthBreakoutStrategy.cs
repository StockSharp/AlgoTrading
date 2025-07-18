using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades on Bollinger Band width breakouts.
	/// When Bollinger Band width increases significantly above its average, 
	/// it enters position in the direction determined by price movement.
	/// </summary>
	public class BollingerWidthBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerLength;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _stopMultiplier;
		
		private BollingerBands _bollinger;
		private SimpleMovingAverage _widthAverage;
		private AverageTrueRange _atr;
		private decimal _bestBidPrice;
		private decimal _bestAskPrice;

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerLength
		{
			get => _bollingerLength.Value;
			set => _bollingerLength.Value = value;
		}
		
		/// <summary>
		/// Bollinger Bands standard deviation multiplier.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}
		
		/// <summary>
		/// Period for width average calculation.
		/// </summary>
		public int AvgPeriod
		{
			get => _avgPeriod.Value;
			set => _avgPeriod.Value = value;
		}
		
		/// <summary>
		/// Standard deviation multiplier for breakout detection.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
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
		/// Stop-loss ATR multiplier.
		/// </summary>
		public int StopMultiplier
		{
			get => _stopMultiplier.Value;
			set => _stopMultiplier.Value = value;
		}
		
		/// <summary>
		/// Initialize <see cref="BollingerWidthBreakoutStrategy"/>.
		/// </summary>
		public BollingerWidthBreakoutStrategy()
		{
			_bollingerLength = Param(nameof(BollingerLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Length", "Period of the Bollinger Bands indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
				
			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
			
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for Bollinger width average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
			
			_multiplier = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
			
			_stopMultiplier = Param(nameof(StopMultiplier), 2)
				.SetGreaterThanZero()
				.SetDisplay("Stop Multiplier", "ATR multiplier for stop-loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);
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

			_bestBidPrice = default;
			_bestAskPrice = default;

			// Create indicators
			_bollinger = new BollingerBands
			{
				Length = BollingerLength,
				Width = BollingerDeviation
			};
			
			_widthAverage = new SimpleMovingAverage { Length = AvgPeriod };
			_atr = new AverageTrueRange { Length = BollingerLength };
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind Bollinger Bands
			subscription
				.BindEx(_bollinger, _atr, ProcessBollinger)
				.Start();

			SubscribeOrderBook()
				.Bind(d =>
				{
					_bestBidPrice = d.GetBestBid()?.Price ?? _bestBidPrice;
					_bestAskPrice = d.GetBestAsk()?.Price ?? _bestAskPrice;
				})
				.Start();

			// Create chart area for visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessBollinger(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			// Process candle through ATR
			var currentAtr = atrValue.ToDecimal();

			// Calculate Bollinger Band width
			var bollingerTyped = (BollingerBandsValue)bollingerValue;

			if (bollingerTyped.UpBand is not decimal upperBand)
				return;

			if (bollingerTyped.LowBand is not decimal lowerBand)
				return;

			var lastWidth = upperBand - lowerBand;

			// Process width through average
			var widthAvgValue = _widthAverage.Process(lastWidth, candle.ServerTime, candle.State == CandleStates.Finished);
			var avgWidth = widthAvgValue.ToDecimal();
			
			// Calculate width standard deviation (simplified approach)
			var stdDev = Math.Abs(lastWidth - avgWidth) * 1.5m; // Simplified approximation
			
			// Skip if indicators are not formed yet
			if (!_bollinger.IsFormed || !_widthAverage.IsFormed || !_atr.IsFormed)
			{
				return;
			}
			
			// Check if trading is allowed
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				return;
			}
			
			// Bollinger width breakout detection
			if (lastWidth > avgWidth + Multiplier * stdDev)
			{
				// Determine direction based on price and bands
				var priceDirection = false;
				
				// If price is closer to upper band, go long. If closer to lower band, go short.
				var upperDistance = Math.Abs(candle.ClosePrice - upperBand);
				var lowerDistance = Math.Abs(candle.ClosePrice - lowerBand);
				
				if (upperDistance < lowerDistance)
				{
					// Price is closer to upper band, likely bullish
					priceDirection = true;
				}
				
				// Cancel active orders before placing new ones
				CancelActiveOrders();
				
				// Calculate stop-loss based on current ATR
				var stopOffset = StopMultiplier * currentAtr;
				
				// Trade in the determined direction
				if (priceDirection && Position <= 0)
				{
					// Bullish direction - Buy
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (!priceDirection && Position >= 0)
				{
					// Bearish direction - Sell
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			// Check for exit condition - width returns to average
			else if ((Position > 0 || Position < 0) && lastWidth < avgWidth)
			{
				// Exit position
				ClosePosition();
			}
		}
	}
}
