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
    /// Implementation of strategy #164 - MA + Williams %R.
    /// Buy when price is above MA and Williams %R is below -80 (oversold).
    /// Sell when price is below MA and Williams %R is above -20 (overbought).
    /// </summary>
    public class MaWilliamsRStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<MovingAverageTypeEnum> _maType;
        private readonly StrategyParam<int> _williamsRPeriod;
        private readonly StrategyParam<decimal> _williamsROversold;
        private readonly StrategyParam<decimal> _williamsROverbought;
        private readonly StrategyParam<Unit> _stopLoss;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Moving Average period.
        /// </summary>
        public int MaPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Moving Average type.
        /// </summary>
        public MovingAverageTypeEnum MaType
        {
            get => _maType.Value;
            set => _maType.Value = value;
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
        /// Initialize <see cref="MaWilliamsRStrategy"/>.
        /// </summary>
        public MaWilliamsRStrategy()
        {
            _maPeriod = Param(nameof(MaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average", "MA Parameters");

            _maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
                .SetDisplay("MA Type", "Type of Moving Average", "MA Parameters");

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

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Candle type for strategy", "General");
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
            LengthIndicator<decimal> ma;
            
            // Create MA based on selected type
            switch (MaType)
            {
                case MovingAverageTypeEnum.Exponential:
                    ma = new ExponentialMovingAverage { Length = MaPeriod };
                    break;
                case MovingAverageTypeEnum.Weighted:
                    ma = new WeightedMovingAverage { Length = MaPeriod };
                    break;
                case MovingAverageTypeEnum.Smoothed:
                    ma = new SmoothedMovingAverage { Length = MaPeriod };
                    break;
                case MovingAverageTypeEnum.HullMA:
                    ma = new HullMovingAverage { Length = MaPeriod };
                    break;
                case MovingAverageTypeEnum.Simple:
                default:
                    ma = new SimpleMovingAverage { Length = MaPeriod };
                    break;
            }
            
            var williamsR = new WilliamsR { Length = WilliamsRPeriod };

            // Setup candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators to candles
            subscription
                .Bind(ma, williamsR, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, ma);
                
                // Create separate area for Williams %R
                var oscillatorArea = CreateChartArea();
                if (oscillatorArea != null)
                {
                    DrawIndicator(oscillatorArea, williamsR);
                }
                
                DrawOwnTrades(area);
            }

            // Start protective orders
            StartProtection(StopLoss);
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal williamsRValue)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Current price
            var price = candle.ClosePrice;
            
            // Determine if price is above or below MA
            var isPriceAboveMA = price > maValue;

            LogInfo($"Candle: {candle.OpenTime}, Close: {price}, " +
                   $"MA: {maValue}, Price > MA: {isPriceAboveMA}, " +
                   $"Williams %R: {williamsRValue}");

            // Trading rules
            if (isPriceAboveMA && williamsRValue < WilliamsROversold && Position <= 0)
            {
                // Buy signal - price above MA and Williams %R oversold
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                LogInfo($"Buy signal: Price above MA and Williams %R oversold ({williamsRValue} < {WilliamsROversold}). Volume: {volume}");
            }
            else if (!isPriceAboveMA && williamsRValue > WilliamsROverbought && Position >= 0)
            {
                // Sell signal - price below MA and Williams %R overbought
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                
                LogInfo($"Sell signal: Price below MA and Williams %R overbought ({williamsRValue} > {WilliamsROverbought}). Volume: {volume}");
            }
            // Exit conditions
            else if (!isPriceAboveMA && Position > 0)
            {
                // Exit long position when price falls below MA
                SellMarket(Position);
                LogInfo($"Exit long: Price fell below MA. Position: {Position}");
            }
            else if (isPriceAboveMA && Position < 0)
            {
                // Exit short position when price rises above MA
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Price rose above MA. Position: {Position}");
            }
        }
        
        /// <summary>
        /// Enum for Moving Average types.
        /// </summary>
        public enum MovingAverageTypeEnum
        {
            /// <summary>
            /// Simple Moving Average
            /// </summary>
            Simple,
            
            /// <summary>
            /// Exponential Moving Average
            /// </summary>
            Exponential,
            
            /// <summary>
            /// Weighted Moving Average
            /// </summary>
            Weighted,
            
            /// <summary>
            /// Smoothed Moving Average
            /// </summary>
            Smoothed,
            
            /// <summary>
            /// Hull Moving Average
            /// </summary>
            HullMA
        }
    }
}
