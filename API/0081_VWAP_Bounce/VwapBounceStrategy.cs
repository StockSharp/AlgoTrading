using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// VWAP Bounce Strategy.
    /// Enters long when price is below VWAP and a bullish candle forms.
    /// Enters short when price is above VWAP and a bearish candle forms.
    /// </summary>
    public class VwapBounceStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<Unit> _stopLoss;
        
        private decimal _prevVwap;

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
        /// Initializes a new instance of the <see cref="VwapBounceStrategy"/>.
        /// </summary>
        public VwapBounceStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");
                
            _stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
                .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
                .SetRange(0.5m, 5m, 0.5m)
                .SetCanOptimize(true);
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

            // Enable position protection using stop-loss
            StartProtection(
                takeProfit: null,
                stopLoss: StopLoss,
                isStopTrailing: false,
                useMarketOrders: true
            );

            // Initialize VWAP
            _prevVwap = 0;

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

            // Calculate VWAP for current candle
            decimal vwap = candle.TotalVolume != 0 
                ? candle.TotalPrice / candle.TotalVolume 
                : candle.ClosePrice;

            // If VWAP is not initialized yet
            if (_prevVwap == 0)
            {
                _prevVwap = vwap;
                return;
            }
            
            // Bullish candle condition (Close > Open)
            bool isBullishCandle = candle.ClosePrice > candle.OpenPrice;
            
            // Bearish candle condition (Close < Open)
            bool isBearishCandle = candle.ClosePrice < candle.OpenPrice;
            
            // Long entry: Price below VWAP and bullish candle
            if (candle.ClosePrice < vwap && isBullishCandle && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: Close {candle.ClosePrice}, VWAP {vwap}, Bullish Candle");
            }
            // Short entry: Price above VWAP and bearish candle
            else if (candle.ClosePrice > vwap && isBearishCandle && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: Close {candle.ClosePrice}, VWAP {vwap}, Bearish Candle");
            }

            // Update previous VWAP value
            _prevVwap = vwap;
        }
    }
}