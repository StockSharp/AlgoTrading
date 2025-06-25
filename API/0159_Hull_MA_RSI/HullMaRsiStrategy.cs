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
    /// Implementation of strategy #159 - Hull Moving Average + RSI.
    /// Buy when HMA is rising and RSI is below 30 (oversold).
    /// Sell when HMA is falling and RSI is above 70 (overbought).
    /// </summary>
    public class HullMaRsiStrategy : Strategy
    {
        private readonly StrategyParam<int> _hmaPeriod;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOversold;
        private readonly StrategyParam<decimal> _rsiOverbought;
        private readonly StrategyParam<Unit> _stopLoss;
        private readonly StrategyParam<DataType> _candleType;

        private decimal _prevHmaValue;

        /// <summary>
        /// Hull Moving Average period.
        /// </summary>
        public int HmaPeriod
        {
            get => _hmaPeriod.Value;
            set => _hmaPeriod.Value = value;
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
        /// Initialize <see cref="HullMaRsiStrategy"/>.
        /// </summary>
        public HullMaRsiStrategy()
        {
            _hmaPeriod = Param(nameof(HmaPeriod), 9)
                .SetGreaterThanZero()
                .SetDisplay("HMA Period", "Period for Hull Moving Average", "HMA Parameters");

            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Period for Relative Strength Index", "RSI Parameters");

            _rsiOversold = Param(nameof(RsiOversold), 30m)
                .SetRange(1, 100)
                .SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters");

            _rsiOverbought = Param(nameof(RsiOverbought), 70m)
                .SetRange(1, 100)
                .SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters");

            _stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Atr))
                .SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management");

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Candle type for strategy", "General");

            _prevHmaValue = 0;
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
            var hma = new HullMovingAverage { Length = HmaPeriod };
            var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

            // Reset previous HMA value
            _prevHmaValue = 0;

            // Setup candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators to candles
            subscription
                .Bind(hma, rsi, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, hma);
                
                // Create separate area for RSI
                var rsiArea = CreateChartArea();
                if (rsiArea != null)
                {
                    DrawIndicator(rsiArea, rsi);
                }
                
                DrawOwnTrades(area);
            }

            // Start protective orders
            StartProtection(StopLoss);
        }

        private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal rsiValue)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Determine if HMA is rising or falling
            var isHmaRising = _prevHmaValue != 0 && hmaValue > _prevHmaValue;
            var isHmaFalling = _prevHmaValue != 0 && hmaValue < _prevHmaValue;

            LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, " +
                   $"HMA: {hmaValue}, Previous HMA: {_prevHmaValue}, " +
                   $"HMA Rising: {isHmaRising}, HMA Falling: {isHmaFalling}, " +
                   $"RSI: {rsiValue}");

            // Trading rules
            if (isHmaRising && rsiValue < RsiOversold && Position <= 0)
            {
                // Buy signal - HMA rising and RSI oversold
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                LogInfo($"Buy signal: HMA rising and RSI oversold ({rsiValue} < {RsiOversold}). Volume: {volume}");
            }
            else if (isHmaFalling && rsiValue > RsiOverbought && Position >= 0)
            {
                // Sell signal - HMA falling and RSI overbought
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                
                LogInfo($"Sell signal: HMA falling and RSI overbought ({rsiValue} > {RsiOverbought}). Volume: {volume}");
            }
            // Exit conditions based on HMA direction change
            else if (isHmaFalling && Position > 0)
            {
                // Exit long position when HMA starts falling
                SellMarket(Position);
                LogInfo($"Exit long: HMA started falling. Position: {Position}");
            }
            else if (isHmaRising && Position < 0)
            {
                // Exit short position when HMA starts rising
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: HMA started rising. Position: {Position}");
            }

            // Update HMA value for next iteration
            _prevHmaValue = hmaValue;
        }
    }
}
