using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Bollinger Bands and RSI.
    /// 
    /// Entry criteria:
    /// Long: Price < BB_lower && RSI < 30 (oversold near lower band)
    /// Short: Price > BB_upper && RSI > 70 (overbought near upper band)
    /// 
    /// Exit criteria:
    /// Long: Price > BB_middle (price returns to middle band)
    /// Short: Price < BB_middle (price returns to middle band)
    /// </summary>
    public class BollingerRsiStrategy : Strategy
    {
        private readonly StrategyParam<int> _bollingerPeriod;
        private readonly StrategyParam<decimal> _bollingerDeviation;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOversold;
        private readonly StrategyParam<decimal> _rsiOverbought;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Period for Bollinger Bands calculation.
        /// </summary>
        public int BollingerPeriod
        {
            get => _bollingerPeriod.Value;
            set => _bollingerPeriod.Value = value;
        }

        /// <summary>
        /// Standard deviation multiplier for Bollinger Bands.
        /// </summary>
        public decimal BollingerDeviation
        {
            get => _bollingerDeviation.Value;
            set => _bollingerDeviation.Value = value;
        }

        /// <summary>
        /// Period for RSI calculation.
        /// </summary>
        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set => _rsiPeriod.Value = value;
        }

        /// <summary>
        /// RSI level for oversold condition.
        /// </summary>
        public decimal RsiOversold
        {
            get => _rsiOversold.Value;
            set => _rsiOversold.Value = value;
        }

        /// <summary>
        /// RSI level for overbought condition.
        /// </summary>
        public decimal RsiOverbought
        {
            get => _rsiOverbought.Value;
            set => _rsiOverbought.Value = value;
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
        /// Constructor.
        /// </summary>
        public BollingerRsiStrategy()
        {
            _bollingerPeriod = Param(nameof(BollingerPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Period", "Number of periods used for Bollinger Bands", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Deviation", "Number of standard deviations for Bollinger Bands", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 3.0m, 0.5m);

            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Number of periods used for RSI", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _rsiOversold = Param(nameof(RsiOversold), 30m)
                .SetRange(10, 40)
                .SetDisplay("RSI Oversold", "RSI level below which the market is considered oversold", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(20, 40, 5);

            _rsiOverbought = Param(nameof(RsiOverbought), 70m)
                .SetRange(60, 90)
                .SetDisplay("RSI Overbought", "RSI level above which the market is considered overbought", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(60, 80, 5);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
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
            var bollinger = new BollingerBands
            {
                Length = BollingerPeriod,
                Width = BollingerDeviation
            };

            var rsi = new RelativeStrengthIndex
            {
                Length = RsiPeriod
            };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            // Process candles with bollinger and rsi
            subscription
                .Bind(bollinger, rsi, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, bollinger);
                
                // Create separate area for RSI
                var rsiArea = CreateChartArea();
                if (rsiArea != null)
                {
                    DrawIndicator(rsiArea, rsi);
                }
                
                DrawOwnTrades(area);
            }
            
            // Start protection with ATR-based stop loss
            var atrMultiplier = 2;
            StartProtection(
                takeProfit: null,
                stopLoss: new Unit(atrMultiplier, UnitTypes.Atr)
            );
        }

        private void ProcessCandle(ICandleMessage candle, 
            decimal middleBand, decimal upperBand, decimal lowerBand, 
            decimal rsiValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Trading logic
            var price = candle.ClosePrice;
            
            // Log current state
            this.AddInfoLog($"Close: {price}, Middle: {middleBand:N2}, Upper: {upperBand:N2}, Lower: {lowerBand:N2}, RSI: {rsiValue:N2}");
            
            // Entry logic
            if (Position == 0)
            {
                if (price < lowerBand && rsiValue < RsiOversold)
                {
                    // Buy signal: price below lower band and RSI oversold
                    BuyMarket(Volume);
                    this.AddInfoLog($"Buy signal: Price ({price}) below lower band ({lowerBand}) and RSI ({rsiValue}) below {RsiOversold}");
                }
                else if (price > upperBand && rsiValue > RsiOverbought)
                {
                    // Sell signal: price above upper band and RSI overbought
                    SellMarket(Volume);
                    this.AddInfoLog($"Sell signal: Price ({price}) above upper band ({upperBand}) and RSI ({rsiValue}) above {RsiOverbought}");
                }
            }
            // Exit logic
            else if (Position > 0 && price > middleBand)
            {
                // Exit long position: price returned to middle band
                SellMarket(Math.Abs(Position));
                this.AddInfoLog($"Exit long position: Price ({price}) returned to middle band ({middleBand})");
            }
            else if (Position < 0 && price < middleBand)
            {
                // Exit short position: price returned to middle band
                BuyMarket(Math.Abs(Position));
                this.AddInfoLog($"Exit short position: Price ({price}) returned to middle band ({middleBand})");
            }
        }
    }
}
