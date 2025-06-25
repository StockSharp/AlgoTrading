using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy combining Keltner Channels and RSI indicators.
    /// Looks for mean reversion opportunities when price touches channel boundaries
    /// and RSI confirms oversold/overbought conditions.
    /// </summary>
    public class KeltnerRsiStrategy : Strategy
    {
        private readonly StrategyParam<int> _emaPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOverboughtLevel;
        private readonly StrategyParam<decimal> _rsiOversoldLevel;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// EMA period for Keltner Channels.
        /// </summary>
        public int EmaPeriod
        {
            get => _emaPeriod.Value;
            set => _emaPeriod.Value = value;
        }

        /// <summary>
        /// ATR period for Keltner Channels.
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// ATR multiplier for Keltner Channels width.
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
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

        // Fields for indicators
        private ExponentialMovingAverage _ema;
        private ATR _atr;
        private RSI _rsi;

        /// <summary>
        /// Initialize strategy.
        /// </summary>
        public KeltnerRsiStrategy()
        {
            _emaPeriod = Param(nameof(EmaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("EMA Period", "Period for EMA in Keltner Channels", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "Period for ATR in Keltner Channels", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

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
            return new[] { (Security, CandleType) };
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Create indicators
            _ema = new ExponentialMovingAverage { Length = EmaPeriod };
            _atr = new ATR { Length = AtrPeriod };
            _rsi = new RSI { Length = RsiPeriod };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Use WhenCandlesFinished to process candles manually
            subscription
                .WhenCandlesFinished(this)
                .Do(ProcessCandle)
                .Apply(this);

            // Start subscription
            subscription.Start();

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
                
                // Add indicators to chart
                DrawIndicator(area, _ema);
                
                // Create second area for RSI
                var rsiArea = CreateChartArea();
                DrawIndicator(rsiArea, _rsi);
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle)
        {
            // Process candle with indicators
            var emaValue = _ema.Process(candle).GetValue<decimal>();
            var atrValue = _atr.Process(candle).GetValue<decimal>();
            var rsiValue = _rsi.Process(candle).GetValue<decimal>();
            
            // Skip if indicators are not formed yet
            if (!_ema.IsFormed || !_atr.IsFormed || !_rsi.IsFormed)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate Keltner Channels
            var upperBand = emaValue + (atrValue * AtrMultiplier);
            var lowerBand = emaValue - (atrValue * AtrMultiplier);

            // Trading logic
            if (candle.ClosePrice < lowerBand && rsiValue < RsiOversoldLevel && Position <= 0)
            {
                // Price below lower Keltner band and RSI oversold - Buy
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
            }
            else if (candle.ClosePrice > upperBand && rsiValue > RsiOverboughtLevel && Position >= 0)
            {
                // Price above upper Keltner band and RSI overbought - Sell
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
            }
            else if (Position > 0 && candle.ClosePrice > emaValue)
            {
                // Exit long position when price crosses above EMA (middle band)
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice < emaValue)
            {
                // Exit short position when price crosses below EMA (middle band)
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}
