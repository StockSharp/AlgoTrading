using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Implementation of strategy #152 - Supertrend + RSI.
    /// Buy when price is above Supertrend and RSI is below 30 (oversold).
    /// Sell when price is below Supertrend and RSI is above 70 (overbought).
    /// </summary>
    public class SupertrendRsiStrategy : Strategy
    {
        private readonly StrategyParam<int> _supertrendPeriod;
        private readonly StrategyParam<decimal> _supertrendMultiplier;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOversold;
        private readonly StrategyParam<decimal> _rsiOverbought;
        private readonly StrategyParam<DataType> _candleType;

        // Indicators
        private SuperTrend _supertrend;
        private RelativeStrengthIndex _rsi;

        /// <summary>
        /// Supertrend period.
        /// </summary>
        public int SupertrendPeriod
        {
            get => _supertrendPeriod.Value;
            set => _supertrendPeriod.Value = value;
        }

        /// <summary>
        /// Supertrend multiplier.
        /// </summary>
        public decimal SupertrendMultiplier
        {
            get => _supertrendMultiplier.Value;
            set => _supertrendMultiplier.Value = value;
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
        /// Candle type used for strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize <see cref="SupertrendRsiStrategy"/>.
        /// </summary>
        public SupertrendRsiStrategy()
        {
            _supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
                .SetGreaterThanZero()
                .SetDisplay("Supertrend Period", "Period for ATR in Supertrend", "Supertrend Parameters");

            _supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
                .SetGreaterThanZero()
                .SetDisplay("Supertrend Multiplier", "Multiplier for ATR in Supertrend", "Supertrend Parameters");

            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Period for relative strength index", "RSI Parameters");

            _rsiOversold = Param(nameof(RsiOversold), 30m)
                .SetRange(1, 100)
                .SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters");

            _rsiOverbought = Param(nameof(RsiOverbought), 70m)
                .SetRange(1, 100)
                .SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters");

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Candle type for strategy", "General");
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
            _supertrend = new SuperTrend
            {
                Length = SupertrendPeriod,
                Multiplier = SupertrendMultiplier
            };

            _rsi = new RelativeStrengthIndex
            {
                Length = RsiPeriod
            };

            // Setup candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind both indicators to the candle feed
            subscription
                .Bind(_supertrend, _rsi, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _supertrend);
                
                // Create separate area for RSI
                var rsiArea = CreateChartArea();
                if (rsiArea != null)
                {
                    DrawIndicator(rsiArea, _rsi);
                }
                
                DrawOwnTrades(area);
            }

            // Using Supertrend for dynamic stop-loss
            // (the strategy design already includes the dynamic stop-loss mechanism
            // through the Supertrend indicator crossovers)
        }

        private void ProcessCandle(ICandleMessage candle, decimal supertrendValue, decimal rsiValue)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, " +
                   $"Supertrend: {supertrendValue}, RSI: {rsiValue}");

            // Trading rules
            var trend = candle.ClosePrice > supertrendValue ? 1 : -1; // 1 = uptrend, -1 = downtrend

            if (trend > 0 && rsiValue < RsiOversold && Position <= 0)
            {
                // Buy signal - price above Supertrend (uptrend) and RSI is oversold
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);

                LogInfo($"Buy signal: Uptrend (Price > Supertrend) and RSI oversold ({rsiValue} < {RsiOversold}). Volume: {volume}");
            }
            else if (trend < 0 && rsiValue > RsiOverbought && Position >= 0)
            {
                // Sell signal - price below Supertrend (downtrend) and RSI is overbought
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);

                LogInfo($"Sell signal: Downtrend (Price < Supertrend) and RSI overbought ({rsiValue} > {RsiOverbought}). Volume: {volume}");
            }
            // Exit conditions are handled by Supertrend crossovers
            else if (trend < 0 && Position > 0)
            {
                // Exit long position when trend turns down
                SellMarket(Math.Abs(Position));
                LogInfo($"Exit long: Trend turned down (Price < Supertrend). Position: {Position}");
            }
            else if (trend > 0 && Position < 0)
            {
                // Exit short position when trend turns up
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Trend turned up (Price > Supertrend). Position: {Position}");
            }
        }
    }
}
