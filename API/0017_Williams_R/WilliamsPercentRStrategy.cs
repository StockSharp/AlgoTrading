using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Williams %R indicator.
    /// </summary>
    public class WilliamsPercentRStrategy : Strategy
    {
        private readonly StrategyParam<int> _period;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Williams %R period.
        /// </summary>
        public int Period
        {
            get => _period.Value;
            set => _period.Value = value;
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
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilliamsPercentRStrategy"/>.
        /// </summary>
        public WilliamsPercentRStrategy()
        {
            _period = Param(nameof(Period), 14)
                .SetDisplay("Period", "Period for Williams %R calculation", "Indicators")
                .SetRange(5, 50, 1)
                .SetCanOptimize(true);

            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
                .SetRange(0.5m, 5m, 0.5m)
                .SetCanOptimize(true);

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

            // Create Williams %R indicator
            var williamsR = new WilliamsR { Length = Period };

            // Subscribe to candles and bind the indicator
            var subscription = SubscribeCandles(CandleType);
            subscription
                .BindEx(williamsR, ProcessCandle)
                .Start();
                
            // Enable position protection
            StartProtection(
                takeProfit: null,
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
                useMarketOrders: true
            );

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, williamsR);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            var williamsRValue = value.GetValue<decimal>();

            // Note: Williams %R values are negative, typically from 0 to -100
            // Oversold: Below -80
            // Overbought: Above -20

            // Entry logic
            if (williamsRValue < -80 && Position <= 0)
            {
                // Oversold condition - buy signal
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                LogInfo($"Buy signal: Williams %R oversold at {williamsRValue}");
            }
            else if (williamsRValue > -20 && Position >= 0)
            {
                // Overbought condition - sell signal
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                LogInfo($"Sell signal: Williams %R overbought at {williamsRValue}");
            }

            // Exit logic
            if (Position > 0 && williamsRValue > -50)
            {
                // Exit long position when returning to neutral territory
                SellMarket(Math.Abs(Position));
                LogInfo($"Exiting long position: Williams %R at {williamsRValue}");
            }
            else if (Position < 0 && williamsRValue < -50)
            {
                // Exit short position when returning to neutral territory
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exiting short position: Williams %R at {williamsRValue}");
            }
        }
    }
}