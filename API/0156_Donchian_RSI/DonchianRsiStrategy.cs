using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy combining Donchian Channels and RSI indicators.
    /// Buys on Donchian breakouts when RSI confirms trend is not overextended.
    /// </summary>
    public class DonchianRsiStrategy : Strategy
    {
        private readonly StrategyParam<int> _donchianPeriod;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOverboughtLevel;
        private readonly StrategyParam<decimal> _rsiOversoldLevel;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Period for Donchian Channels calculation.
        /// </summary>
        public int DonchianPeriod
        {
            get => _donchianPeriod.Value;
            set => _donchianPeriod.Value = value;
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
        /// Stop loss percentage.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Candle type for strategy calculation.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        // Variables to store previous high and low values for breakout detection
        private decimal _prevUpperBand;
        private decimal _prevLowerBand;
        private bool _isFirstCalculation = true;

        /// <summary>
        /// Initialize strategy.
        /// </summary>
        public DonchianRsiStrategy()
        {
            _donchianPeriod = Param(nameof(DonchianPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Donchian Period", "Period for Donchian Channels calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

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

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

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

            // Create indicators
            var donchian = new DonchianChannel { Length = DonchianPeriod };
            var rsi = new RSI { Length = RsiPeriod };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Bind indicators to candles
            subscription
                .Bind(donchian, rsi, ProcessCandle)
                .Start();

            // Enable stop-loss
            StartProtection(
                takeProfit: null,
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
                isStopTrailing: false,
                useMarketOrders: true
            );

            // Setup chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, donchian);
                
                // Create second area for RSI
                var rsiArea = CreateChartArea();
                DrawIndicator(rsiArea, rsi);
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Store current bands before comparison
            var currentUpper = upperBand;
            var currentLower = lowerBand;

            // Skip first calculation to avoid false breakouts
            if (_isFirstCalculation)
            {
                _isFirstCalculation = false;
                _prevUpperBand = currentUpper;
                _prevLowerBand = currentLower;
                return;
            }

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Detect breakouts by comparing current price to previous Donchian bands
            bool upperBreakout = candle.ClosePrice > _prevUpperBand;
            bool lowerBreakout = candle.ClosePrice < _prevLowerBand;

            // Trading logic
            if (upperBreakout && rsiValue < RsiOverboughtLevel && Position <= 0)
            {
                // Upward breakout with RSI not overbought - Buy
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
            }
            else if (lowerBreakout && rsiValue > RsiOversoldLevel && Position >= 0)
            {
                // Downward breakout with RSI not oversold - Sell
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
            }
            else if (Position > 0 && candle.ClosePrice < middleBand)
            {
                // Exit long position when price falls below middle band
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > middleBand)
            {
                // Exit short position when price rises above middle band
                BuyMarket(Math.Abs(Position));
            }

            // Update previous bands for next comparison
            _prevUpperBand = currentUpper;
            _prevLowerBand = currentLower;
        }
    }
}
