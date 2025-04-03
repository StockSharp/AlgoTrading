using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// N-day high/low breakout strategy.
    /// Enters long when price breaks above the N-day high.
    /// Enters short when price breaks below the N-day low.
    /// Exits when price crosses the moving average.
    /// </summary>
    public class NdayBreakoutStrategy : Strategy
    {
        private readonly StrategyParam<int> _lookbackPeriod;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        // Indicators for entry conditions
        private Highest _highest;
        private Lowest _lowest;
        private SMA _ma;

        // Values for tracking breakouts
        private decimal _nDayHigh;
        private decimal _nDayLow;
        private bool _isFormed;

        /// <summary>
        /// Period for looking back to determine the highest/lowest value.
        /// </summary>
        public int LookbackPeriod
        {
            get => _lookbackPeriod.Value;
            set => _lookbackPeriod.Value = value;
        }

        /// <summary>
        /// Period for the moving average used for exit signals.
        /// </summary>
        public int MaPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// The type of candles to use for strategy calculation.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NdayBreakoutStrategy()
        {
            _lookbackPeriod = Param(nameof(LookbackPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Lookback Period", "Number of days to determine the high/low range", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _maPeriod = Param(nameof(MaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for the moving average used as exit signal", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromDays(1)))
                .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters");
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

            // Initialize tracking variables
            _nDayHigh = 0;
            _nDayLow = decimal.MaxValue;
            _isFormed = false;

            // Create indicators
            _highest = new Highest { Length = LookbackPeriod };
            _lowest = new Lowest { Length = LookbackPeriod };
            _ma = new SMA { Length = MaPeriod };

            // Create and setup subscription for candles
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators to candles
            subscription
                .Bind(_highest, _lowest, _ma, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _highest);
                DrawIndicator(area, _lowest);
                DrawIndicator(area, _ma);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal maValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Wait until indicators are formed
            if (!_isFormed)
            {
                // Check if highest and lowest indicators are now formed
                if (_highest.IsFormed && _lowest.IsFormed)
                {
                    _nDayHigh = highestValue;
                    _nDayLow = lowestValue;
                    _isFormed = true;
                    LogInfo($"Indicators formed. Initial N-day high: {_nDayHigh}, N-day low: {_nDayLow}");
                }
                return;
            }

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            LogInfo($"Processing candle: High={candle.HighPrice}, Low={candle.LowPrice}, Close={candle.ClosePrice}");
            LogInfo($"Current N-day high: {_nDayHigh}, N-day low: {_nDayLow}, MA: {maValue}");

            // Entry logic - only trigger on breakouts
            if (candle.HighPrice > _nDayHigh && Position <= 0)
            {
                // Long entry - price breaks above the N-day high
                LogInfo($"Long entry signal: Price {candle.HighPrice} broke above N-day high {_nDayHigh}");
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (candle.LowPrice < _nDayLow && Position >= 0)
            {
                // Short entry - price breaks below the N-day low
                LogInfo($"Short entry signal: Price {candle.LowPrice} broke below N-day low {_nDayLow}");
                SellMarket(Volume + Math.Abs(Position));
            }

            // Exit logic
            if (Position > 0 && candle.ClosePrice < maValue)
            {
                // Exit long position when price crosses below MA
                LogInfo($"Long exit signal: Price {candle.ClosePrice} crossed below MA {maValue}");
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > maValue)
            {
                // Exit short position when price crosses above MA
                LogInfo($"Short exit signal: Price {candle.ClosePrice} crossed above MA {maValue}");
                BuyMarket(Math.Abs(Position));
            }

            // Update N-day high and low values for next candle
            _nDayHigh = highestValue;
            _nDayLow = lowestValue;
        }
    }
}
