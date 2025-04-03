using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy #70: Morning Star candle pattern strategy.
    /// The strategy looks for a Morning Star pattern - first bearish candle, second small candle (doji), third bullish candle that closes above the midpoint of the first.
    /// </summary>
    public class MorningStarStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;

        private ICandleMessage _firstCandle;
        private ICandleMessage _secondCandle;

        /// <summary>
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public MorningStarStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
                .SetNotNegative()
                .SetDisplay("Stop Loss %", "Stop loss percentage below the second candle's low", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(0.5m, 3.0m, 0.5m);
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

            // Reset candle storage
            _firstCandle = null;
            _secondCandle = null;

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // The strategy only takes long positions
            if (Position > 0)
                return;

            // If we have no previous candle stored, store the current one and return
            if (_firstCandle == null)
            {
                _firstCandle = candle;
                return;
            }

            // If we have one previous candle stored, store the current one as the second and return
            if (_secondCandle == null)
            {
                _secondCandle = candle;
                return;
            }

            // We now have three candles to analyze (the first two stored and the current one)
            var isMorningStar = CheckMorningStar(_firstCandle, _secondCandle, candle);

            if (isMorningStar)
            {
                // Morning Star pattern detected - enter long position
                BuyMarket(Volume);
                
                this.AddInfoLog($"Morning Star pattern detected. Entering long position at {candle.ClosePrice}");
                
                // Set stop-loss
                var stopPrice = _secondCandle.LowPrice * (1 - StopLossPercent / 100);
                this.AddInfoLog($"Setting stop-loss at {stopPrice}");
                
                // Setup trailing stop
                StartProtection(
                    takeProfit: new Unit(0, UnitTypes.Absolute), // no take profit, rely on exit signal
                    stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
                );
            }

            // Shift candles (drop first, move second to first, current to second)
            _firstCandle = _secondCandle;
            _secondCandle = candle;

            // Exit logic for existing positions
            if (Position > 0 && candle.HighPrice > _secondCandle.HighPrice)
            {
                SellMarket(Math.Abs(Position));
                this.AddInfoLog($"Exit signal: Price above previous high. Closing position at {candle.ClosePrice}");
            }
        }

        private bool CheckMorningStar(ICandleMessage first, ICandleMessage second, ICandleMessage third)
        {
            // Check the first candle is bearish (close lower than open)
            var firstIsBearish = first.ClosePrice < first.OpenPrice;
            
            // Check the third candle is bullish (close higher than open)
            var thirdIsBullish = third.ClosePrice > third.OpenPrice;
            
            // Calculate the body size (absolute difference between open and close)
            var firstBodySize = Math.Abs(first.OpenPrice - first.ClosePrice);
            var secondBodySize = Math.Abs(second.OpenPrice - second.ClosePrice);
            
            // Second candle should have a small body (doji or near-doji) - typically less than 30% of the first
            var secondIsSmall = secondBodySize < (firstBodySize * 0.3m);
            
            // Calculate midpoint of first candle
            var firstMidpoint = (first.HighPrice + first.LowPrice) / 2;
            
            // Third candle close should be above the midpoint of the first candle
            var thirdClosesHighEnough = third.ClosePrice > firstMidpoint;
            
            // Log pattern analysis
            this.AddInfoLog($"Pattern analysis: First bearish={firstIsBearish}, Second small={secondIsSmall}, " +
                           $"Third bullish={thirdIsBullish}, Third above midpoint={thirdClosesHighEnough}");
            
            // Return true if all conditions are met
            return firstIsBearish && secondIsSmall && thirdIsBullish && thirdClosesHighEnough;
        }
    }
}