using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Heikin Ashi Reversal Strategy.
    /// Enters long when Heikin-Ashi candles change from bearish to bullish.
    /// Enters short when Heikin-Ashi candles change from bullish to bearish.
    /// </summary>
    public class HeikinAshiReversalStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<Unit> _stopLoss;
        
        private bool? _prevIsBullish;

        /// <summary>
        /// Type of candles to use.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
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
        /// Initializes a new instance of the <see cref="HeikinAshiReversalStrategy"/>.
        /// </summary>
        public HeikinAshiReversalStrategy()
        {
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
                .SetDisplay("Candle Type", "Type of candles to use", "General");
                
            _stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
                .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
                .SetRange(1m, 3m, 0.5m)
                .SetCanOptimize(true);
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

            // Initialize previous value
            _prevIsBullish = null;

            // Create subscription to candles
            var subscription = SubscribeCandles(CandleType);
            
            // Bind candle handler
            subscription
                .Bind(ProcessCandle)
                .Start();

            // Setup chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process new candle.
        /// </summary>
        /// <param name="candle">New candle.</param>
        private void ProcessCandle(ICandleMessage candle)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Calculate Heikin-Ashi candle values
            decimal haOpen, haClose, haHigh, haLow;
            
            if (_prevIsBullish == null)
            {
                // First candle - initialize HA values
                haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
                haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
                haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
                haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));
                
                // Store the initial bullish/bearish state
                _prevIsBullish = haClose > haOpen;
                return;
            }
            
            // Calculate previous HA open/close based on previous state
            decimal prevHaOpen = _prevIsBullish.Value 
                ? Math.Min(candle.OpenPrice, candle.ClosePrice)
                : Math.Max(candle.OpenPrice, candle.ClosePrice);
                
            decimal prevHaClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
            
            // Calculate current HA values
            haOpen = (prevHaOpen + prevHaClose) / 2;
            haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
            haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
            haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));
            
            // Determine if current HA candle is bullish or bearish
            bool isBullish = haClose > haOpen;
            
            // Check for trend reversal
            bool bullishReversal = !_prevIsBullish.Value && isBullish;
            bool bearishReversal = _prevIsBullish.Value && !isBullish;
            
            // Long entry: Bullish reversal
            if (bullishReversal && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                this.AddInfoLog($"Long entry: Heikin-Ashi reversal from bearish to bullish");
            }
            // Short entry: Bearish reversal
            else if (bearishReversal && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                this.AddInfoLog($"Short entry: Heikin-Ashi reversal from bullish to bearish");
            }
            
            // Update previous state
            _prevIsBullish = isBullish;
        }
    }
}