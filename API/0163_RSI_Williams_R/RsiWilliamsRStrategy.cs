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
    /// Implementation of strategy #163 - RSI + Williams %R.
    /// Buy when RSI is below 30 and Williams %R is below -80 (double oversold condition).
    /// Sell when RSI is above 70 and Williams %R is above -20 (double overbought condition).
    /// </summary>
    public class RsiWilliamsRStrategy : Strategy
    {
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOversold;
        private readonly StrategyParam<decimal> _rsiOverbought;
        private readonly StrategyParam<int> _williamsRPeriod;
        private readonly StrategyParam<decimal> _williamsROversold;
        private readonly StrategyParam<decimal> _williamsROverbought;
        private readonly StrategyParam<Unit> _stopLoss;
        private readonly StrategyParam<DataType> _candleType;

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
        /// Williams %R period.
        /// </summary>
        public int WilliamsRPeriod
        {
            get => _williamsRPeriod.Value;
            set => _williamsRPeriod.Value = value;
        }

        /// <summary>
        /// Williams %R oversold level (usually below -80).
        /// </summary>
        public decimal WilliamsROversold
        {
            get => _williamsROversold.Value;
            set => _williamsROversold.Value = value;
        }

        /// <summary>
        /// Williams %R overbought level (usually above -20).
        /// </summary>
        public decimal WilliamsROverbought
        {
            get => _williamsROverbought.Value;
            set => _williamsROverbought.Value = value;
        }

        /// <summary>
        /// Stop-loss value.
        /// </summary>
        public Unit StopLoss
        {
            get => _stopLoss.Value;
            set => _stopLoss.Value = value;
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
        /// Initialize <see cref="RsiWilliamsRStrategy"/>.
        /// </summary>
        public RsiWilliamsRStrategy()
        {
            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Period for Relative Strength Index", "RSI Parameters");

            _rsiOversold = Param(nameof(RsiOversold), 30m)
                .SetRange(1, 100)
                .SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters");

            _rsiOverbought = Param(nameof(RsiOverbought), 70m)
                .SetRange(1, 100)
                .SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters");

            _williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("Williams %R Period", "Period for Williams %R", "Williams %R Parameters");

            _williamsROversold = Param(nameof(WilliamsROversold), -80m)
                .SetRange(-100, 0)
                .SetDisplay("Williams %R Oversold", "Williams %R level to consider market oversold", "Williams %R Parameters");

            _williamsROverbought = Param(nameof(WilliamsROverbought), -20m)
                .SetRange(-100, 0)
                .SetDisplay("Williams %R Overbought", "Williams %R level to consider market overbought", "Williams %R Parameters");

            _stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
                .SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management");

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
            var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
            var williamsR = new WilliamsR { Length = WilliamsRPeriod };

            // Setup candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators to candles
            subscription
                .Bind(rsi, williamsR, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // Create separate area for oscillators
                var oscillatorArea = CreateChartArea();
                if (oscillatorArea != null)
                {
                    DrawIndicator(oscillatorArea, rsi);
                    DrawIndicator(oscillatorArea, williamsR);
                }
                
                DrawOwnTrades(area);
            }

            // Start protective orders
            StartProtection(StopLoss);
        }

        private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal williamsRValue)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, " +
                   $"RSI: {rsiValue}, Williams %R: {williamsRValue}");

            // Trading rules
            if (rsiValue < RsiOversold && williamsRValue < WilliamsROversold && Position <= 0)
            {
                // Buy signal - double oversold condition
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                LogInfo($"Buy signal: Double oversold condition - RSI: {rsiValue} < {RsiOversold} and Williams %R: {williamsRValue} < {WilliamsROversold}. Volume: {volume}");
            }
            else if (rsiValue > RsiOverbought && williamsRValue > WilliamsROverbought && Position >= 0)
            {
                // Sell signal - double overbought condition
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                
                LogInfo($"Sell signal: Double overbought condition - RSI: {rsiValue} > {RsiOverbought} and Williams %R: {williamsRValue} > {WilliamsROverbought}. Volume: {volume}");
            }
            // Exit conditions
            else if (rsiValue > 50 && Position > 0)
            {
                // Exit long position when RSI returns to neutral zone
                SellMarket(Position);
                LogInfo($"Exit long: RSI returned to neutral zone ({rsiValue} > 50). Position: {Position}");
            }
            else if (rsiValue < 50 && Position < 0)
            {
                // Exit short position when RSI returns to neutral zone
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: RSI returned to neutral zone ({rsiValue} < 50). Position: {Position}");
            }
        }
    }
}
