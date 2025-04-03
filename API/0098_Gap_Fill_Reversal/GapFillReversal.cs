using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Gap Fill Reversal Strategy that trades gaps followed by reversal candles.
    /// It enters when a gap is followed by a candle in the opposite direction of the gap.
    /// </summary>
    public class GapFillReversalStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<decimal> _minGapPercent;

        private ICandleMessage _previousCandle;
        private ICandleMessage _currentCandle;

        /// <summary>
        /// Candle type and timeframe for strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss percent from entry price.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Minimum gap size as percentage for trade signal.
        /// </summary>
        public decimal MinGapPercent
        {
            get => _minGapPercent.Value;
            set => _minGapPercent.Value = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GapFillReversalStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                         .SetDisplay("Candle Type", "Type of candles for strategy calculation", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                              .SetRange(0.1m, 5m)
                              .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management");

            _minGapPercent = Param(nameof(MinGapPercent), 0.5m)
                            .SetRange(0.1m, 3m)
                            .SetDisplay("Min Gap %", "Minimum gap size as percentage for trade signal", "Trading Parameters");
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

            // Reset candle storage
            _previousCandle = null;
            _currentCandle = null;

            // Create subscription and bind to process candles
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(ProcessCandle)
                .Start();

            // Setup protection with stop loss
            StartProtection(
                takeProfit: null,
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
                isStopTrailing: false
            );

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Shift candles
            _previousCandle = _currentCandle;
            _currentCandle = candle;

            if (_previousCandle == null)
                return;

            // Check for a gap
            var hasGapUp = _currentCandle.OpenPrice > _previousCandle.ClosePrice;
            var hasGapDown = _currentCandle.OpenPrice < _previousCandle.ClosePrice;

            // Calculate gap size as a percentage
            decimal gapSize = 0;
            if (hasGapUp)
                gapSize = (_currentCandle.OpenPrice - _previousCandle.ClosePrice) / _previousCandle.ClosePrice * 100;
            else if (hasGapDown)
                gapSize = (_previousCandle.ClosePrice - _currentCandle.OpenPrice) / _previousCandle.ClosePrice * 100;

            // Check if gap is large enough
            if (gapSize < MinGapPercent)
                return;

            // Check for a gap up followed by a bearish candle (potential reversal)
            var isGapUpWithReversal = hasGapUp && _currentCandle.ClosePrice < _currentCandle.OpenPrice;

            // Check for a gap down followed by a bullish candle (potential reversal)
            var isGapDownWithReversal = hasGapDown && _currentCandle.ClosePrice > _currentCandle.OpenPrice;

            // Check for long entry condition
            if (isGapDownWithReversal && Position <= 0)
            {
                LogInfo($"Gap down of {gapSize:F2}% with bullish reversal candle. Going long.");
                BuyMarket(Volume + Math.Abs(Position));
            }
            // Check for short entry condition
            else if (isGapUpWithReversal && Position >= 0)
            {
                LogInfo($"Gap up of {gapSize:F2}% with bearish reversal candle. Going short.");
                SellMarket(Volume + Math.Abs(Position));
            }
            // Check for exit conditions
            else if ((Position > 0 && candle.ClosePrice > _previousCandle.ClosePrice) || 
                     (Position < 0 && candle.ClosePrice < _previousCandle.ClosePrice))
            {
                LogInfo("Gap filled. Exiting position.");
                ClosePosition();
            }
        }
    }
}