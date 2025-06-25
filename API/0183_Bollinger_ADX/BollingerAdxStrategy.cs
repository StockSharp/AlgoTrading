using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Bollinger Bands and ADX indicators.
    /// Enters long on upper Bollinger band breakout with strong trend (ADX > 25)
    /// Enters short on lower Bollinger band breakout with strong trend (ADX > 25)
    /// </summary>
    public class BollingerAdxStrategy : Strategy
    {
        private readonly StrategyParam<int> _bollingerPeriod;
        private readonly StrategyParam<decimal> _bollingerDeviation;
        private readonly StrategyParam<int> _adxPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;
        
        private bool _wasAboveBollinger;
        private bool _wasBelowBollinger;

        /// <summary>
        /// Bollinger Bands period
        /// </summary>
        public int BollingerPeriod
        {
            get => _bollingerPeriod.Value;
            set => _bollingerPeriod.Value = value;
        }

        /// <summary>
        /// Bollinger Bands deviation
        /// </summary>
        public decimal BollingerDeviation
        {
            get => _bollingerDeviation.Value;
            set => _bollingerDeviation.Value = value;
        }

        /// <summary>
        /// ADX period
        /// </summary>
        public int AdxPeriod
        {
            get => _adxPeriod.Value;
            set => _adxPeriod.Value = value;
        }
        
        /// <summary>
        /// ATR period for stop-loss calculation
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }
        
        /// <summary>
        /// ATR multiplier for stop-loss
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Candle type for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public BollingerAdxStrategy()
        {
            _bollingerPeriod = Param(nameof(BollingerPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(15, 30, 5);

            _bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 2.5m, 0.5m);

            _adxPeriod = Param(nameof(AdxPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);
                
            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);
                
            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

            // Create indicators
            var bollinger = new BollingerBands
            {
                Length = BollingerPeriod,
                Width = BollingerDeviation
            };
            
            var adx = new AverageDirectionalIndex { Length = AdxPeriod };
            
            var atr = new AverageTrueRange { Length = AtrPeriod };
            
            // Reset state variables
            _wasAboveBollinger = false;
            _wasBelowBollinger = false;

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(bollinger, adx, atr, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, bollinger);
                
                // Create a separate area for ADX
                var adxArea = CreateChartArea();
                if (adxArea != null)
                {
                    DrawIndicator(adxArea, adx);
                }
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal bollingerValue, decimal adxValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Get additional values from Bollinger Bands
            var bollingerIndicator = (BollingerBands)Indicators.FindById(nameof(BollingerBands));
            if (bollingerIndicator == null)
                return;

            var middleBand = bollingerValue; // Middle band is returned by default
            var upperBand = bollingerIndicator.UpBand.GetCurrentValue();
            var lowerBand = bollingerIndicator.LowBand.GetCurrentValue();
            
            // Current price (close of the candle)
            var price = candle.ClosePrice;
            
            // Check if price is above upper band or below lower band
            var isAboveUpper = price > upperBand;
            var isBelowLower = price < lowerBand;
            
            // Check for ADX strength
            var isStrongTrend = adxValue > 25;
            
            // Check for breakout pattern
            var isUpperBreakout = isAboveUpper && !_wasAboveBollinger;
            var isLowerBreakout = isBelowLower && !_wasBelowBollinger;
            
            // Update state for next candle
            _wasAboveBollinger = isAboveUpper;
            _wasBelowBollinger = isBelowLower;
            
            // Stop-loss size based on ATR
            var stopSize = atrValue * AtrMultiplier;

            // Trading logic
            if (isUpperBreakout && isStrongTrend && Position <= 0)
            {
                // Buy signal: price breaks above upper band with strong trend
                BuyMarket(Volume + Math.Abs(Position));
                
                // Set stop-loss
                var stopPrice = price - stopSize;
                RegisterOrder(this.CreateOrder(Sides.Sell, stopPrice, Math.Abs(Position + Volume)));
            }
            else if (isLowerBreakout && isStrongTrend && Position >= 0)
            {
                // Sell signal: price breaks below lower band with strong trend
                SellMarket(Volume + Math.Abs(Position));
                
                // Set stop-loss
                var stopPrice = price + stopSize;
                RegisterOrder(this.CreateOrder(Sides.Buy, stopPrice, Math.Abs(Position + Volume)));
            }
            // Exit conditions
            else if (adxValue < 20)
            {
                // Trend is weakening - close any position
                if (Position > 0)
                    SellMarket(Position);
                else if (Position < 0)
                    BuyMarket(Math.Abs(Position));
            }
            // Also exit when price returns to middle band
            else if (price <= middleBand && Position > 0)
            {
                // Exit long position when price returns to middle band
                SellMarket(Position);
            }
            else if (price >= middleBand && Position < 0)
            {
                // Exit short position when price returns to middle band
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}