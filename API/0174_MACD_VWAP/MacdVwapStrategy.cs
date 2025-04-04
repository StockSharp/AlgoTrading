using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on MACD and VWAP indicators.
    /// Enters long when MACD > Signal and price > VWAP
    /// Enters short when MACD < Signal and price < VWAP
    /// </summary>
    public class MacdVwapStrategy : Strategy
    {
        private readonly StrategyParam<int> _macdFast;
        private readonly StrategyParam<int> _macdSlow;
        private readonly StrategyParam<int> _macdSignal;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// MACD fast period
        /// </summary>
        public int MacdFast
        {
            get => _macdFast.Value;
            set => _macdFast.Value = value;
        }

        /// <summary>
        /// MACD slow period
        /// </summary>
        public int MacdSlow
        {
            get => _macdSlow.Value;
            set => _macdSlow.Value = value;
        }

        /// <summary>
        /// MACD signal period
        /// </summary>
        public int MacdSignal
        {
            get => _macdSignal.Value;
            set => _macdSignal.Value = value;
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
        public MacdVwapStrategy()
        {
            _macdFast = Param(nameof(MacdFast), 12)
                .SetGreaterThanZero()
                .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(8, 16, 2);

            _macdSlow = Param(nameof(MacdSlow), 26)
                .SetGreaterThanZero()
                .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(20, 30, 2);

            _macdSignal = Param(nameof(MacdSignal), 9)
                .SetGreaterThanZero()
                .SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 12, 1);

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
            var macd = new MovingAverageConvergenceDivergence
            {
                FastMa = new ExponentialMovingAverage { Length = MacdFast },
                SlowMa = new ExponentialMovingAverage { Length = MacdSlow },
                SignalMa = new ExponentialMovingAverage { Length = MacdSignal }
            };

            var vwap = new VolumeWeightedMovingAverage();

            // Enable position protection with stop-loss
            StartProtection(
                takeProfit: new Unit(0), // No take profit
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(macd, vwap, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, macd);
                DrawIndicator(area, vwap);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal vwapValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Get additional values from MACD (signal line)
            var macdIndicator = (MovingAverageConvergenceDivergence)Indicators.FindById(nameof(MovingAverageConvergenceDivergence));
            if (macdIndicator == null)
                return;

            var signalValue = macdIndicator.SignalMa.GetCurrentValue();
            
            // Current price (close of the candle)
            var price = candle.ClosePrice;

            // Trading logic
            if (macdValue > signalValue && price > vwapValue && Position <= 0)
            {
                // Buy signal: MACD above signal and price above VWAP
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (macdValue < signalValue && price < vwapValue && Position >= 0)
            {
                // Sell signal: MACD below signal and price below VWAP
                SellMarket(Volume + Math.Abs(Position));
            }
            // Exit conditions
            else if (macdValue < signalValue && Position > 0)
            {
                // Exit long position when MACD crosses below signal
                SellMarket(Position);
            }
            else if (macdValue > signalValue && Position < 0)
            {
                // Exit short position when MACD crosses above signal
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}