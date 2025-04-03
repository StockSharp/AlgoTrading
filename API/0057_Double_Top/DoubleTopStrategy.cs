using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Double Top reversal strategy: looks for two similar tops with confirmation.
    /// This pattern often indicates a trend reversal from bullish to bearish.
    /// </summary>
    public class DoubleTopStrategy : Strategy
    {
        private readonly StrategyParam<int> _distanceParam;
        private readonly StrategyParam<decimal> _similarityPercent;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;

        private decimal? _firstTopHigh;
        private decimal? _secondTopHigh;
        private int _barsSinceFirstTop;
        private bool _patternConfirmed;
        
        private Highest _highestIndicator;

        /// <summary>
        /// Distance between tops in bars.
        /// </summary>
        public int Distance
        {
            get => _distanceParam.Value;
            set => _distanceParam.Value = value;
        }

        /// <summary>
        /// Maximum percent difference between two tops to consider them similar.
        /// </summary>
        public decimal SimilarityPercent
        {
            get => _similarityPercent.Value;
            set => _similarityPercent.Value = value;
        }

        /// <summary>
        /// Type of candles to use.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage above the higher of the two tops.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleTopStrategy"/>.
        /// </summary>
        public DoubleTopStrategy()
        {
            _distanceParam = Param(nameof(Distance), 5)
                .SetRange(3, 15)
                .SetDisplay("Distance between tops", "Number of bars between two tops", "Pattern Parameters")
                .SetCanOptimize(true);

            _similarityPercent = Param(nameof(SimilarityPercent), 2.0m)
                .SetRange(0.5m, 5.0m)
                .SetDisplay("Similarity %", "Maximum percentage difference between two tops", "Pattern Parameters")
                .SetCanOptimize(true);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
                .SetRange(0.5m, 3.0m)
                .SetDisplay("Stop Loss %", "Percentage above top for stop-loss", "Risk Management")
                .SetCanOptimize(true);
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

            _firstTopHigh = null;
            _secondTopHigh = null;
            _barsSinceFirstTop = 0;
            _patternConfirmed = false;

            // Create indicator to find highest values
            _highestIndicator = new Highest { Length = Distance * 2 };

            // Subscribe to candles
            var subscription = SubscribeCandles(CandleType);

            // Bind candle processing 
            subscription
                .Bind(ProcessCandle)
                .Start();

            // Enable position protection
            StartProtection(
                new Unit(0, UnitTypes.Absolute), // No take profit (manual exit)
                new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss at defined percentage
                false // No trailing
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
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Process the candle with the Highest indicator
            var highestValue = _highestIndicator.Process(candle).GetValue<decimal>();

            // If strategy is not ready yet, return
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Already in position, no need to search for new patterns
            if (Position < 0)
                return;

            // If we have a confirmed pattern and price falls below support
            if (_patternConfirmed && candle.ClosePrice < candle.OpenPrice)
            {
                // Sell signal - Double Top with confirmation candle
                SellMarket(Volume);
                LogInfo($"Double Top signal: Sell at {candle.ClosePrice}, Stop Loss at {Math.Max(_firstTopHigh.Value, _secondTopHigh.Value) * (1 + StopLossPercent / 100)}");
                
                // Reset pattern detection
                _patternConfirmed = false;
                _firstTopHigh = null;
                _secondTopHigh = null;
                _barsSinceFirstTop = 0;
                return;
            }

            // Pattern detection logic
            if (_firstTopHigh == null)
            {
                // Looking for first top
                if (candle.HighPrice == highestValue)
                {
                    _firstTopHigh = candle.HighPrice;
                    _barsSinceFirstTop = 0;
                    LogInfo($"Potential first top detected at price {_firstTopHigh}");
                }
            }
            else
            {
                _barsSinceFirstTop++;

                // If we're at the appropriate distance, check for second top
                if (_barsSinceFirstTop >= Distance && _secondTopHigh == null)
                {
                    // Check if current high is close to first top
                    var priceDifference = Math.Abs((candle.HighPrice - _firstTopHigh.Value) / _firstTopHigh.Value * 100);
                    
                    if (priceDifference <= SimilarityPercent)
                    {
                        _secondTopHigh = candle.HighPrice;
                        _patternConfirmed = true;
                        LogInfo($"Double Top pattern confirmed. First: {_firstTopHigh}, Second: {_secondTopHigh}");
                    }
                }

                // If too much time has passed, reset pattern search
                if (_barsSinceFirstTop > Distance * 3 || (_secondTopHigh != null && _barsSinceFirstTop > Distance * 4))
                {
                    _firstTopHigh = null;
                    _secondTopHigh = null;
                    _barsSinceFirstTop = 0;
                    _patternConfirmed = false;
                }
            }
        }
    }
}