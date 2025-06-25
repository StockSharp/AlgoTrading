using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Stochastic RSI crossover.
    /// </summary>
    public class StochasticRsiCrossStrategy : Strategy
    {
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<int> _stochPeriod;
        private readonly StrategyParam<int> _kPeriod;
        private readonly StrategyParam<int> _dPeriod;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        // Cache for K and D values
        private decimal _prevK;
        private decimal _prevD;
        private bool _isFirstCandle = true;

        /// <summary>
        /// RSI period.
        /// </summary>
        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set => _rsiPeriod.Value = value;
        }

        /// <summary>
        /// Stochastic period.
        /// </summary>
        public int StochPeriod
        {
            get => _stochPeriod.Value;
            set => _stochPeriod.Value = value;
        }

        /// <summary>
        /// K period (fast).
        /// </summary>
        public int KPeriod
        {
            get => _kPeriod.Value;
            set => _kPeriod.Value = value;
        }

        /// <summary>
        /// D period (slow).
        /// </summary>
        public int DPeriod
        {
            get => _dPeriod.Value;
            set => _dPeriod.Value = value;
        }

        /// <summary>
        /// Stop loss percentage.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StochasticRsiCrossStrategy"/>.
        /// </summary>
        public StochasticRsiCrossStrategy()
        {
            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetRange(7, 30, 1)
                .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
                .SetCanOptimize(true);

            _stochPeriod = Param(nameof(StochPeriod), 14)
                .SetRange(5, 30, 1)
                .SetDisplay("Stochastic Period", "Period for Stochastic", "Indicators")
                .SetCanOptimize(true);

            _kPeriod = Param(nameof(KPeriod), 3)
                .SetRange(1, 10, 1)
                .SetDisplay("K Period", "Period for %K line", "Indicators")
                .SetCanOptimize(true);

            _dPeriod = Param(nameof(DPeriod), 3)
                .SetRange(1, 10, 1)
                .SetDisplay("D Period", "Period for %D line", "Indicators")
                .SetCanOptimize(true);

            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetRange(0.5m, 5m, 0.5m)
                .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
                .SetCanOptimize(true);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

            // Reset state variables
            _prevK = 0;
            _prevD = 0;
            _isFirstCandle = true;

            // Create a StochRsi indicator
            var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
            var stoch = new StochasticOscillator
            {
                Kperiod = StochPeriod,
                Kslow = KPeriod,
                Dslow = DPeriod
            };

            // Subscribe to candles
            var subscription = SubscribeCandles(CandleType);
            
            // Create a custom binding to simulate Stochastic RSI since it's not built-in
            subscription
                .Bind(stoch, rsi, ProcessCandle)
                .Start();

            // Enable position protection
            StartProtection(
                takeProfit: null,
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
                useMarketOrders: true
            );

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, rsi);
                DrawIndicator(area, stoch);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, StochasticValue stochValue, decimal rsiValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Get K and D values from stochastic
            decimal kValue = stochValue.K;
            decimal dValue = stochValue.D;

            // For the first candle, just store values and return
            if (_isFirstCandle)
            {
                _prevK = kValue;
                _prevD = dValue;
                _isFirstCandle = false;
                return;
            }

            // Check for crossovers
            bool kCrossedAboveD = _prevK <= _prevD && kValue > dValue;
            bool kCrossedBelowD = _prevK >= _prevD && kValue < dValue;

            // Entry logic
            if (kCrossedAboveD && kValue < 20 && Position <= 0)
            {
                // Buy when %K crosses above %D in oversold territory (below 20)
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                LogInfo($"Buy signal: Stochastic RSI %K ({kValue:F2}) crossed above %D ({dValue:F2}) in oversold zone");
            }
            else if (kCrossedBelowD && kValue > 80 && Position >= 0)
            {
                // Sell when %K crosses below %D in overbought territory (above 80)
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                LogInfo($"Sell signal: Stochastic RSI %K ({kValue:F2}) crossed below %D ({dValue:F2}) in overbought zone");
            }

            // Exit logic
            if (Position > 0 && kValue > 50)
            {
                // Exit long when %K rises above 50 (middle zone)
                SellMarket(Math.Abs(Position));
                LogInfo($"Exiting long position: Stochastic RSI %K reached {kValue:F2}");
            }
            else if (Position < 0 && kValue < 50)
            {
                // Exit short when %K falls below 50 (middle zone)
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exiting short position: Stochastic RSI %K reached {kValue:F2}");
            }

            // Update previous values for next comparison
            _prevK = kValue;
            _prevD = dValue;
        }
    }
}