using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy #75: OBV (On-Balance Volume) Divergence strategy.
    /// The strategy uses divergence between price and OBV indicator to identify potential reversal points.
    /// </summary>
    public class OBVDivergenceStrategy : Strategy
    {
        private readonly StrategyParam<int> _divergencePeriod;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;
        
        private OnBalanceVolume _obv;
        private SimpleMovingAverage _ma;
        
        // Store historical values for divergence detection
        private decimal _previousPrice;
        private decimal _previousObv;
        private decimal _currentPrice;
        private decimal _currentObv;

        /// <summary>
        /// Period for divergence detection.
        /// </summary>
        public int DivergencePeriod
        {
            get => _divergencePeriod.Value;
            set => _divergencePeriod.Value = value;
        }

        /// <summary>
        /// Moving average period for exit signal.
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
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
        public OBVDivergenceStrategy()
        {
            _divergencePeriod = Param(nameof(DivergencePeriod), 5)
                .SetGreaterThanZero()
                .SetDisplay("Divergence Period", "Number of periods to look back for divergence", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(3, 10, 1);

            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for moving average calculation (used for exit signal)", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

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
            return new[] { (Security, CandleType) };
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Initialize values
            _previousPrice = 0;
            _previousObv = 0;
            _currentPrice = 0;
            _currentObv = 0;

            // Create indicators
            _obv = new OnBalanceVolume();
            _ma = new SimpleMovingAverage
            {
                Length = MAPeriod
            };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(_obv, _ma, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _ma);
                DrawIndicator(area, _obv);
                DrawOwnTrades(area);
            }

            // Start position protection
            StartProtection(
                takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on the strategy's exit logic
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );
        }

        private void ProcessCandle(ICandleMessage candle, IIndicatorValue obvValue, IIndicatorValue maValue)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Store price and OBV values
            if (_divergencePeriod <= 0)
                return;

            _previousPrice = _currentPrice;
            _previousObv = _currentObv;
            
            _currentPrice = candle.ClosePrice;
            _currentObv = obvValue.GetValue<decimal>();
            
            var maPrice = maValue.GetValue<decimal>();

            // We need at least two points to detect divergence
            if (_previousPrice == 0)
                return;

            // Check for bullish divergence
            // Price makes lower low but OBV makes higher low
            var bullishDivergence = _currentPrice < _previousPrice && _currentObv > _previousObv;

            // Check for bearish divergence
            // Price makes higher high but OBV makes lower high
            var bearishDivergence = _currentPrice > _previousPrice && _currentObv < _previousObv;

            // Log divergence information
            LogInfo($"Price: {_previousPrice} -> {_currentPrice}, OBV: {_previousObv} -> {_currentObv}");
            LogInfo($"Bullish divergence: {bullishDivergence}, Bearish divergence: {bearishDivergence}");

            // Trading decisions based on divergence
            if (bullishDivergence && Position <= 0)
            {
                // Bullish divergence - go long
                CancelActiveOrders();
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: Bullish divergence detected at price {_currentPrice}");
            }
            else if (bearishDivergence && Position >= 0)
            {
                // Bearish divergence - go short
                CancelActiveOrders();
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: Bearish divergence detected at price {_currentPrice}");
            }

            // Exit logic based on moving average
            if (Position > 0 && _currentPrice > maPrice)
            {
                // Exit long position when price is above MA
                SellMarket(Math.Abs(Position));
                LogInfo($"Long exit: Price {_currentPrice} above MA {maPrice}");
            }
            else if (Position < 0 && _currentPrice < maPrice)
            {
                // Exit short position when price is below MA
                BuyMarket(Math.Abs(Position));
                LogInfo($"Short exit: Price {_currentPrice} below MA {maPrice}");
            }
        }
    }
}