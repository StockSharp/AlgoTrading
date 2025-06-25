using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy #74: Williams %R Divergence strategy.
    /// The strategy looks for divergences between price and Williams %R indicator to identify potential reversal points.
    /// </summary>
    public class WilliamsPercentRDivergenceStrategy : Strategy
    {
        private readonly StrategyParam<int> _williamsRPeriod;
        private readonly StrategyParam<int> _divergencePeriod;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;

        private WilliamsR _williamsR;
        
        // Store historical values to detect divergence
        private decimal _previousPrice;
        private decimal _previousWilliamsR;
        private decimal _currentPrice;
        private decimal _currentWilliamsR;

        /// <summary>
        /// Williams %R period.
        /// </summary>
        public int WilliamsRPeriod
        {
            get => _williamsRPeriod.Value;
            set => _williamsRPeriod.Value = value;
        }

        /// <summary>
        /// Period for divergence detection.
        /// </summary>
        public int DivergencePeriod
        {
            get => _divergencePeriod.Value;
            set => _divergencePeriod.Value = value;
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
        public WilliamsPercentRDivergenceStrategy()
        {
            _williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);

            _divergencePeriod = Param(nameof(DivergencePeriod), 5)
                .SetGreaterThanZero()
                .SetDisplay("Divergence Period", "Number of periods to look back for divergence", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(3, 10, 1);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetNotNegative()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);
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

            // Initialize values
            _previousPrice = 0;
            _previousWilliamsR = 0;
            _currentPrice = 0;
            _currentWilliamsR = 0;

            // Create Williams %R indicator
            _williamsR = new WilliamsR
            {
                Length = WilliamsRPeriod
            };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(_williamsR, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _williamsR);
                DrawOwnTrades(area);
            }

            // Start position protection
            StartProtection(
                takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on the strategy's exit logic
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );
        }

        private void ProcessCandle(ICandleMessage candle, IIndicatorValue williamsRValue)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Store price and Williams %R values
            if (_divergencePeriod <= 0)
                return;

            _previousPrice = _currentPrice;
            _previousWilliamsR = _currentWilliamsR;
            
            _currentPrice = candle.ClosePrice;
            _currentWilliamsR = williamsRValue.GetValue<decimal>();

            // We need at least two points to detect divergence
            if (_previousPrice == 0)
                return;

            // Check for bullish divergence
            // Price makes lower low but Williams %R makes higher low
            var bullishDivergence = _currentPrice < _previousPrice && _currentWilliamsR > _previousWilliamsR;

            // Check for bearish divergence
            // Price makes higher high but Williams %R makes lower high
            var bearishDivergence = _currentPrice > _previousPrice && _currentWilliamsR < _previousWilliamsR;

            // Log divergence information
            LogInfo($"Price: {_previousPrice} -> {_currentPrice}, Williams %R: {_previousWilliamsR} -> {_currentWilliamsR}");
            LogInfo($"Bullish divergence: {bullishDivergence}, Bearish divergence: {bearishDivergence}");

            // Trading decisions based on divergence and current Williams %R levels
            if (bullishDivergence && _currentWilliamsR < -80 && Position <= 0)
            {
                // Bullish divergence with oversold condition - go long
                CancelActiveOrders();
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: Bullish divergence detected with Williams %R oversold ({_currentWilliamsR})");
            }
            else if (bearishDivergence && _currentWilliamsR > -20 && Position >= 0)
            {
                // Bearish divergence with overbought condition - go short
                CancelActiveOrders();
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: Bearish divergence detected with Williams %R overbought ({_currentWilliamsR})");
            }

            // Exit logic based on Williams %R levels
            if (Position > 0 && _currentWilliamsR > -20)
            {
                // Exit long position when Williams %R reaches overbought level
                SellMarket(Math.Abs(Position));
                LogInfo($"Long exit: Williams %R reached overbought level ({_currentWilliamsR})");
            }
            else if (Position < 0 && _currentWilliamsR < -80)
            {
                // Exit short position when Williams %R reaches oversold level
                BuyMarket(Math.Abs(Position));
                LogInfo($"Short exit: Williams %R reached oversold level ({_currentWilliamsR})");
            }
        }
    }
}