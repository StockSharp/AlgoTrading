using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Bollinger Bands and Stochastic Oscillator indicators.
    /// Enters long when price is at lower band and Stochastic is oversold (< 20)
    /// Enters short when price is at upper band and Stochastic is overbought (> 80)
    /// </summary>
    public class BollingerStochasticStrategy : Strategy
    {
        private readonly StrategyParam<int> _bollingerPeriod;
        private readonly StrategyParam<decimal> _bollingerDeviation;
        private readonly StrategyParam<int> _stochPeriod;
        private readonly StrategyParam<int> _stochK;
        private readonly StrategyParam<int> _stochD;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;

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
        /// Stochastic %K period
        /// </summary>
        public int StochPeriod
        {
            get => _stochPeriod.Value;
            set => _stochPeriod.Value = value;
        }
        
        /// <summary>
        /// Stochastic %K smoothing period
        /// </summary>
        public int StochK
        {
            get => _stochK.Value;
            set => _stochK.Value = value;
        }
        
        /// <summary>
        /// Stochastic %D period
        /// </summary>
        public int StochD
        {
            get => _stochD.Value;
            set => _stochD.Value = value;
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
        public BollingerStochasticStrategy()
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

            _stochPeriod = Param(nameof(StochPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);
                
            _stochK = Param(nameof(StochK), 3)
                .SetGreaterThanZero()
                .SetDisplay("Stochastic %K", "Smoothing for Stochastic %K line", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1, 5, 1);
                
            _stochD = Param(nameof(StochD), 3)
                .SetGreaterThanZero()
                .SetDisplay("Stochastic %D", "Period for Stochastic %D line", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1, 5, 1);
                
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

            var stochastic = new StochasticOscillator
            {
                Length = StochPeriod,
                KPeriod = StochK,
                DPeriod = StochD
            };
            
            var atr = new AverageTrueRange { Length = AtrPeriod };

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(bollinger, stochastic, atr, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, bollinger);
                
                // Create a separate area for Stochastic
                var stochArea = CreateChartArea();
                if (stochArea != null)
                {
                    DrawIndicator(stochArea, stochastic);
                }
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal bollingerValue, decimal stochasticValue, decimal atrValue)
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
            
            // Get Stochastic %K value (main line)
            var stochasticK = stochasticValue;
            
            // Current price (close of the candle)
            var price = candle.ClosePrice;

            // Stop-loss size based on ATR
            var stopSize = atrValue * AtrMultiplier;

            // Trading logic
            if (price <= lowerBand && stochasticK < 20 && Position <= 0)
            {
                // Buy signal: price at/below lower band and Stochastic oversold
                BuyMarket(Volume + Math.Abs(Position));
                
                // Set stop-loss
                var stopPrice = price - stopSize;
                RegisterOrder(this.CreateOrder(Sides.Sell, stopPrice, Math.Abs(Position + Volume)));
            }
            else if (price >= upperBand && stochasticK > 80 && Position >= 0)
            {
                // Sell signal: price at/above upper band and Stochastic overbought
                SellMarket(Volume + Math.Abs(Position));
                
                // Set stop-loss
                var stopPrice = price + stopSize;
                RegisterOrder(this.CreateOrder(Sides.Buy, stopPrice, Math.Abs(Position + Volume)));
            }
            // Exit conditions
            else if (price >= middleBand && Position < 0)
            {
                // Exit short position when price returns to middle band
                BuyMarket(Math.Abs(Position));
            }
            else if (price <= middleBand && Position > 0)
            {
                // Exit long position when price returns to middle band
                SellMarket(Position);
            }
        }