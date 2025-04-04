using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on VWAP and Stochastic Oscillator indicators.
    /// Enters long when price is below VWAP and Stochastic is oversold (< 20)
    /// Enters short when price is above VWAP and Stochastic is overbought (> 80)
    /// </summary>
    public class VwapStochasticStrategy : Strategy
    {
        private readonly StrategyParam<int> _stochPeriod;
        private readonly StrategyParam<int> _stochK;
        private readonly StrategyParam<int> _stochD;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

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
        /// Stop-loss percentage
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
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
        public VwapStochasticStrategy()
        {
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
                
            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

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
            var vwap = new VolumeWeightedMovingAverage();

            var stochastic = new StochasticOscillator
            {
                Length = StochPeriod,
                KPeriod = StochK,
                DPeriod = StochD
            };

            // Enable position protection with stop-loss
            StartProtection(
                takeProfit: new Unit(0), // No take profit
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(vwap, stochastic, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, vwap);
                
                // Create a separate area for Stochastic
                var stochArea = CreateChartArea();
                if (stochArea != null)
                {
                    DrawIndicator(stochArea, stochastic);
                }
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal stochasticValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Current price (close of the candle)
            var price = candle.ClosePrice;
            
            // Check the relation of price to VWAP
            var isBelowVwap = price < vwapValue;
            var isAboveVwap = price > vwapValue;
            
            // Get Stochastic %K value
            var stochasticK = stochasticValue;

            // Trading logic
            if (isBelowVwap && stochasticK < 20 && Position <= 0)
            {
                // Buy signal: price below VWAP and Stochastic oversold
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (isAboveVwap && stochasticK > 80 && Position >= 0)
            {
                // Sell signal: price above VWAP and Stochastic overbought
                SellMarket(Volume + Math.Abs(Position));
            }
            // Exit conditions
            else if (price > vwapValue && Position > 0)
            {
                // Exit long when price crosses above VWAP
                SellMarket(Position);
            }
            else if (price < vwapValue && Position < 0)
            {
                // Exit short when price crosses below VWAP
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}