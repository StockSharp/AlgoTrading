using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// MACD Histogram Reversal Strategy.
    /// Enters long when MACD histogram crosses above zero.
    /// Enters short when MACD histogram crosses below zero.
    /// </summary>
    public class MacdHistogramReversalStrategy : Strategy
    {
        private readonly StrategyParam<int> _fastPeriod;
        private readonly StrategyParam<int> _slowPeriod;
        private readonly StrategyParam<int> _signalPeriod;
        private readonly StrategyParam<Unit> _stopLoss;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal? _prevHistogram;

        /// <summary>
        /// Fast period for MACD calculation.
        /// </summary>
        public int FastPeriod
        {
            get => _fastPeriod.Value;
            set => _fastPeriod.Value = value;
        }

        /// <summary>
        /// Slow period for MACD calculation.
        /// </summary>
        public int SlowPeriod
        {
            get => _slowPeriod.Value;
            set => _slowPeriod.Value = value;
        }

        /// <summary>
        /// Signal period for MACD calculation.
        /// </summary>
        public int SignalPeriod
        {
            get => _signalPeriod.Value;
            set => _signalPeriod.Value = value;
        }

        /// <summary>
        /// Stop loss percentage from entry price.
        /// </summary>
        public Unit StopLoss
        {
            get => _stopLoss.Value;
            set => _stopLoss.Value = value;
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
        /// Initializes a new instance of the <see cref="MacdHistogramReversalStrategy"/>.
        /// </summary>
        public MacdHistogramReversalStrategy()
        {
            _fastPeriod = Param(nameof(FastPeriod), 12)
                .SetDisplay("Fast Period", "Fast period for MACD calculation", "MACD Settings")
                .SetRange(8, 16, 2)
                .SetCanOptimize(true);
                
            _slowPeriod = Param(nameof(SlowPeriod), 26)
                .SetDisplay("Slow Period", "Slow period for MACD calculation", "MACD Settings")
                .SetRange(20, 30, 2)
                .SetCanOptimize(true);
                
            _signalPeriod = Param(nameof(SignalPeriod), 9)
                .SetDisplay("Signal Period", "Signal period for MACD calculation", "MACD Settings")
                .SetRange(7, 13, 1)
                .SetCanOptimize(true);
                
            _stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
                .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
                .SetRange(1m, 3m, 0.5m)
                .SetCanOptimize(true);
                
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

            // Enable position protection using stop-loss
            StartProtection(
                takeProfit: null,
                stopLoss: StopLoss,
                isStopTrailing: false,
                useMarketOrders: true
            );

            // Initialize state
            _prevHistogram = null;
            
            // Create MACD histogram indicator
            var macdHistogram = new MacdHistogram
            {
                FastMa = { Length = FastPeriod },
                SlowMa = { Length = SlowPeriod },
                SignalMa = { Length = SignalPeriod }
            };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicator and process candles
            subscription
                .Bind(macdHistogram, ProcessCandle)
                .Start();
                
            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, macdHistogram);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process candle with MACD histogram value.
        /// </summary>
        /// <param name="candle">Candle.</param>
        /// <param name="histogramValue">MACD histogram value.</param>
        private void ProcessCandle(ICandleMessage candle, decimal histogramValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // If this is the first calculation, just store the value
            if (_prevHistogram == null)
            {
                _prevHistogram = histogramValue;
                return;
            }

            // Check for zero-line crossovers
            bool crossedAboveZero = _prevHistogram < 0 && histogramValue > 0;
            bool crossedBelowZero = _prevHistogram > 0 && histogramValue < 0;
            
            // Long entry: MACD histogram crossed above zero
            if (crossedAboveZero && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: MACD histogram crossed above zero ({_prevHistogram} -> {histogramValue})");
            }
            // Short entry: MACD histogram crossed below zero
            else if (crossedBelowZero && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: MACD histogram crossed below zero ({_prevHistogram} -> {histogramValue})");
            }
            
            // Update previous value
            _prevHistogram = histogramValue;
        }
    }
}