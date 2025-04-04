using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Combined strategy that uses Bollinger Bands and RSI indicators
    /// for mean reversion trading.
    /// </summary>
    public class BollingerRsiStrategy : Strategy
    {
        private readonly StrategyParam<int> _bollingerPeriod;
        private readonly StrategyParam<decimal> _bollingerDeviation;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOversold;
        private readonly StrategyParam<decimal> _rsiOverbought;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossAtr;

        /// <summary>
        /// Bollinger Bands period.
        /// </summary>
        public int BollingerPeriod
        {
            get => _bollingerPeriod.Value;
            set => _bollingerPeriod.Value = value;
        }

        /// <summary>
        /// Bollinger Bands standard deviation multiplier.
        /// </summary>
        public decimal BollingerDeviation
        {
            get => _bollingerDeviation.Value;
            set => _bollingerDeviation.Value = value;
        }

        /// <summary>
        /// RSI period.
        /// </summary>
        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set => _rsiPeriod.Value = value;
        }

        /// <summary>
        /// RSI oversold level.
        /// </summary>
        public decimal RsiOversold
        {
            get => _rsiOversold.Value;
            set => _rsiOversold.Value = value;
        }

        /// <summary>
        /// RSI overbought level.
        /// </summary>
        public decimal RsiOverbought
        {
            get => _rsiOverbought.Value;
            set => _rsiOverbought.Value = value;
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
        /// Stop-loss in ATR multiplier.
        /// </summary>
        public decimal StopLossAtr
        {
            get => _stopLossAtr.Value;
            set => _stopLossAtr.Value = value;
        }

        /// <summary>
        /// Strategy constructor.
        /// </summary>
        public BollingerRsiStrategy()
        {
            _bollingerPeriod = Param(nameof(BollingerPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Period", "Period of the Bollinger Bands indicator", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 3.0m, 0.5m);

            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _rsiOversold = Param(nameof(RsiOversold), 30m)
                .SetNotNegative()
                .SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(20m, 40m, 5m);

            _rsiOverbought = Param(nameof(RsiOverbought), 70m)
                .SetNotNegative()
                .SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(60m, 80m, 5m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossAtr = Param(nameof(StopLossAtr), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management")
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

            // Create indicators
            var bollinger = new BollingerBands
            {
                Length = BollingerPeriod,
                Width = BollingerDeviation
            };

            var rsi = new RelativeStrengthIndex
            {
                Length = RsiPeriod
            };

            var atr = new AverageTrueRange
            {
                Length = 14
            };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);

            subscription
                .Bind(bollinger, rsi, atr, ProcessCandles)
                .Start();

            // Setup position protection
            StartProtection(
                takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
                stopLoss: new Unit(StopLossAtr, UnitTypes.Absolute) // Stop loss as ATR multiplier
            );

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, bollinger);
                DrawIndicator(area, rsi);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process candles and indicator values.
        /// </summary>
        private void ProcessCandles(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Long entry: price below lower Bollinger Band and RSI oversold
            if (candle.ClosePrice < lowerBand && rsiValue < RsiOversold && Position <= 0)
            {
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
            }
            // Short entry: price above upper Bollinger Band and RSI overbought
            else if (candle.ClosePrice > upperBand && rsiValue > RsiOverbought && Position >= 0)
            {
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
            }
            // Long exit: price returns to middle band
            else if (Position > 0 && candle.ClosePrice > middleBand)
            {
                SellMarket(Math.Abs(Position));
            }
            // Short exit: price returns to middle band
            else if (Position < 0 && candle.ClosePrice < middleBand)
            {
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}