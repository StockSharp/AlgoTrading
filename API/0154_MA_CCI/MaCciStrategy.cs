using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy combining Moving Average and CCI indicators.
    /// Buys when price is above MA and CCI is oversold.
    /// Sells when price is below MA and CCI is overbought.
    /// </summary>
    public class MaCciStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _cciPeriod;
        private readonly StrategyParam<decimal> _overboughtLevel;
        private readonly StrategyParam<decimal> _oversoldLevel;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// MA period.
        /// </summary>
        public int MaPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// CCI period.
        /// </summary>
        public int CciPeriod
        {
            get => _cciPeriod.Value;
            set => _cciPeriod.Value = value;
        }

        /// <summary>
        /// CCI overbought level.
        /// </summary>
        public decimal OverboughtLevel
        {
            get => _overboughtLevel.Value;
            set => _overboughtLevel.Value = value;
        }

        /// <summary>
        /// CCI oversold level.
        /// </summary>
        public decimal OversoldLevel
        {
            get => _oversoldLevel.Value;
            set => _oversoldLevel.Value = value;
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

        /// <summary>
        /// Initialize strategy.
        /// </summary>
        public MaCciStrategy()
        {
            _maPeriod = Param(nameof(MaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _cciPeriod = Param(nameof(CciPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _overboughtLevel = Param(nameof(OverboughtLevel), 100m)
                .SetDisplay("Overbought Level", "CCI level considered overbought", "Trading Levels")
                .SetCanOptimize(true)
                .SetOptimize(80, 150, 25);

            _oversoldLevel = Param(nameof(OversoldLevel), -100m)
                .SetDisplay("Oversold Level", "CCI level considered oversold", "Trading Levels")
                .SetCanOptimize(true)
                .SetOptimize(-150, -80, 25);

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 5.0m, 1.0m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
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
            var ma = new SMA { Length = MaPeriod };
            var cci = new CCI { Length = CciPeriod };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Bind indicators to candles
            subscription
                .Bind(ma, cci, ProcessCandle)
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
                DrawIndicator(area, ma);
                
                // Create second area for CCI
                var cciArea = CreateChartArea();
                DrawIndicator(cciArea, cci);
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal cciValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Trading logic
            if (candle.ClosePrice > maValue && cciValue < OversoldLevel && Position <= 0)
            {
                // Price above MA and CCI is oversold - Buy
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
            }
            else if (candle.ClosePrice < maValue && cciValue > OverboughtLevel && Position >= 0)
            {
                // Price below MA and CCI is overbought - Sell
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
            }
            else if (Position > 0 && candle.ClosePrice < maValue)
            {
                // Exit long position when price crosses below MA
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > maValue)
            {
                // Exit short position when price crosses above MA
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}
