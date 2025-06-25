using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy combining Parabolic SAR and RSI indicators.
    /// Uses PSAR for trend direction and RSI to filter entries in overextended conditions.
    /// </summary>
    public class ParabolicSarRsiStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _sarAf;
        private readonly StrategyParam<decimal> _sarMaxAf;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOverboughtLevel;
        private readonly StrategyParam<decimal> _rsiOversoldLevel;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Parabolic SAR acceleration factor.
        /// </summary>
        public decimal SarAf
        {
            get => _sarAf.Value;
            set => _sarAf.Value = value;
        }

        /// <summary>
        /// Parabolic SAR maximum acceleration factor.
        /// </summary>
        public decimal SarMaxAf
        {
            get => _sarMaxAf.Value;
            set => _sarMaxAf.Value = value;
        }

        /// <summary>
        /// Period for RSI calculation.
        /// </summary>
        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set => _rsiPeriod.Value = value;
        }

        /// <summary>
        /// RSI overbought level.
        /// </summary>
        public decimal RsiOverboughtLevel
        {
            get => _rsiOverboughtLevel.Value;
            set => _rsiOverboughtLevel.Value = value;
        }

        /// <summary>
        /// RSI oversold level.
        /// </summary>
        public decimal RsiOversoldLevel
        {
            get => _rsiOversoldLevel.Value;
            set => _rsiOversoldLevel.Value = value;
        }

        /// <summary>
        /// Candle type for strategy calculation.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize strategy.
        /// </summary>
        public ParabolicSarRsiStrategy()
        {
            _sarAf = Param(nameof(SarAf), 0.02m)
                .SetRange(0.01m, 0.1m)
                .SetDisplay("SAR AF", "SAR acceleration factor", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(0.01m, 0.05m, 0.01m);

            _sarMaxAf = Param(nameof(SarMaxAf), 0.2m)
                .SetRange(0.1m, 0.5m)
                .SetDisplay("SAR Max AF", "SAR maximum acceleration factor", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(0.1m, 0.3m, 0.1m);

            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _rsiOverboughtLevel = Param(nameof(RsiOverboughtLevel), 70m)
                .SetRange(50, 90)
                .SetDisplay("RSI Overbought", "RSI level considered overbought", "Trading Levels")
                .SetCanOptimize(true)
                .SetOptimize(65, 80, 5);

            _rsiOversoldLevel = Param(nameof(RsiOversoldLevel), 30m)
                .SetRange(10, 50)
                .SetDisplay("RSI Oversold", "RSI level considered oversold", "Trading Levels")
                .SetCanOptimize(true)
                .SetOptimize(20, 35, 5);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

            // Create indicators
            var sar = new ParabolicSAR
            {
                Acceleration = SarAf,
                AccelerationLimit = SarMaxAf
            };
            
            var rsi = new RSI { Length = RsiPeriod };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Bind indicators to candles
            subscription
                .Bind(sar, rsi, ProcessCandle)
                .Start();

            // Enable dynamic stop-loss using SAR
            StartProtection(
                takeProfit: null,
                stopLoss: null, // Using SAR as dynamic stop instead of fixed percentage
                isStopTrailing: false,
                useMarketOrders: true
            );

            // Setup chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, sar);
                
                // Create second area for RSI
                var rsiArea = CreateChartArea();
                DrawIndicator(rsiArea, rsi);
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal rsiValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Trading logic
            if (candle.ClosePrice > sarValue && rsiValue < RsiOverboughtLevel && Position <= 0)
            {
                // Price above SAR (bullish) and RSI not overbought - Buy
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
            }
            else if (candle.ClosePrice < sarValue && rsiValue > RsiOversoldLevel && Position >= 0)
            {
                // Price below SAR (bearish) and RSI not oversold - Sell
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
            }
            else if (Position > 0 && candle.ClosePrice < sarValue)
            {
                // Exit long position when price crosses below SAR
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > sarValue)
            {
                // Exit short position when price crosses above SAR
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}
