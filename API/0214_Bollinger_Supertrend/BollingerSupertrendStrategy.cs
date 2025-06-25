using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy based on Bollinger Bands and Supertrend indicators.
	/// Enters long when price breaks above upper Bollinger Band and is above Supertrend.
	/// Enters short when price breaks below lower Bollinger Band and is below Supertrend.
	/// Uses Supertrend for dynamic exit.
	/// </summary>
	public class BollingerSupertrendStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _supertrendPeriod;
		private readonly StrategyParam<decimal> _supertrendMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private BollingerBands _bollinger;
		private AverageTrueRange _atr;
		
		private bool _isLongTrend = false;
		private decimal _supertrendValue = 0;
		private decimal _lastClose = 0;
		
		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
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
		/// Supertrend ATR period.
		/// </summary>
		public int SupertrendPeriod
		{
			get => _supertrendPeriod.Value;
			set => _supertrendPeriod.Value = value;
		}
		
		/// <summary>
		/// Supertrend ATR multiplier.
		/// </summary>
		public decimal SupertrendMultiplier
		{
			get => _supertrendMultiplier.Value;
			set => _supertrendMultiplier.Value = value;
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
		public BollingerSupertrendStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);
				
			_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Period", "ATR period for Supertrend calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 14, 1);
				
			_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(2.0m, 4.0m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
			
			// Initialize indicators
			_bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};
			
			_atr = new AverageTrueRange
			{
				Length = SupertrendPeriod
			};
			
			// Create candles subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind Bollinger indicator to candle subscription
			subscription
				.Bind(_bollinger, _atr, ProcessCandle)
				.Start();
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, decimal bollingerValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
				
			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Extract Bollinger Band values
			var bollingerValues = bollingerValue as IComplexIndicatorValue;
			if (bollingerValues == null)
				return;
				
			var middleBand = bollingerValues[BollingerBands.Middle].GetValue<decimal>();
			var upperBand = bollingerValues[BollingerBands.Upper].GetValue<decimal>();
			var lowerBand = bollingerValues[BollingerBands.Lower].GetValue<decimal>();
			
			// Calculate Supertrend
			// Note: This is a simplified Supertrend implementation
			decimal atrValue2 = atrValue * SupertrendMultiplier;
			decimal upperBand2 = (candle.HighPrice + candle.LowPrice) / 2 + atrValue2;
			decimal lowerBand2 = (candle.HighPrice + candle.LowPrice) / 2 - atrValue2;
			
			// Determine Supertrend value and direction
			if (_lastClose == 0)
			{
				// First candle initialization
				_supertrendValue = candle.ClosePrice > (candle.HighPrice + candle.LowPrice) / 2 ? 
					lowerBand2 : upperBand2;
				_isLongTrend = candle.ClosePrice > _supertrendValue;
			}
			else
			{
				// Calculate Supertrend
				if (_isLongTrend)
				{
					// Previous trend was up
					if (candle.ClosePrice < _supertrendValue)
					{
						// Trend changes to down
						_isLongTrend = false;
						_supertrendValue = upperBand2;
					}
					else
					{
						// Trend remains up, adjust supertrend value
						_supertrendValue = Math.Max(lowerBand2, _supertrendValue);
					}
				}
				else
				{
					// Previous trend was down
					if (candle.ClosePrice > _supertrendValue)
					{
						// Trend changes to up
						_isLongTrend = true;
						_supertrendValue = lowerBand2;
					}
					else
					{
						// Trend remains down, adjust supertrend value
						_supertrendValue = Math.Min(upperBand2, _supertrendValue);
					}
				}
			}
			
			_lastClose = candle.ClosePrice;
			
			// Trading logic
			bool isPriceAboveSupertrend = candle.ClosePrice > _supertrendValue;
			bool isPriceAboveUpperBand = candle.ClosePrice > upperBand;
			bool isPriceBelowLowerBand = candle.ClosePrice < lowerBand;
			
			// Long signal: Price breaks above upper Bollinger Band and is above Supertrend
			if (isPriceAboveUpperBand && isPriceAboveSupertrend)
			{
				if (Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long Entry: Price({candle.ClosePrice}) > Upper BB({upperBand}) && Price > Supertrend({_supertrendValue})");
				}
			}
			// Short signal: Price breaks below lower Bollinger Band and is below Supertrend
			else if (isPriceBelowLowerBand && !isPriceAboveSupertrend)
			{
				if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short Entry: Price({candle.ClosePrice}) < Lower BB({lowerBand}) && Price < Supertrend({_supertrendValue})");
				}
			}
			// Exit signals based on Supertrend
			else if ((Position > 0 && !isPriceAboveSupertrend) || 
					(Position < 0 && isPriceAboveSupertrend))
			{
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit Long: Price({candle.ClosePrice}) < Supertrend({_supertrendValue})");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit Short: Price({candle.ClosePrice}) > Supertrend({_supertrendValue})");
				}
			}
		}
	}
}
