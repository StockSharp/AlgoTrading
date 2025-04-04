using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Hull MA Reversal Strategy.
    /// Enters long when Hull MA changes direction from down to up.
    /// Enters short when Hull MA changes direction from up to down.
    /// </summary>
    public class HullMaReversalStrategy : Strategy
    {
        private readonly StrategyParam<int> _hmaPeriod;
        private readonly StrategyParam<Unit> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevHmaValue;
        private decimal _prevPrevHmaValue;
        private AverageTrueRange _atr;

        /// <summary>
        /// Period for Hull Moving Average.
        /// </summary>
        public int HmaPeriod
        {
            get => _hmaPeriod.Value;
            set => _hmaPeriod.Value = value;
        }

        /// <summary>
        /// ATR multiplier for stop-loss calculation.
        /// </summary>
        public Unit AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
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
        /// Initializes a new instance of the <see cref="HullMaReversalStrategy"/>.
        /// </summary>
        public HullMaReversalStrategy()
        {
            _hmaPeriod = Param(nameof(HmaPeriod), 9)
                .SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicator Settings")
                .SetRange(5, 20, 1)
                .SetCanOptimize(true);
                
            _atrMultiplier = Param(nameof(AtrMultiplier), new Unit(2, UnitTypes.Times))
                .SetDisplay("ATR Multiplier", "Multiplier for ATR stop-loss", "Risk Management")
                .SetRange(1.5m, 3.0m, 0.5m)
                .SetCanOptimize(true);
                
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
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

            // Initialize previous values
            _prevHmaValue = 0;
            _prevPrevHmaValue = 0;
            
            // Create indicators
            var hma = new HullMovingAverage { Length = HmaPeriod };
            _atr = new AverageTrueRange { Length = 14 };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators and process candles
            subscription
                .Bind(hma, _atr, ProcessCandle)
                .Start();
                
            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, hma);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process candle with indicator values.
        /// </summary>
        /// <param name="candle">Candle.</param>
        /// <param name="hmaValue">Hull MA value.</param>
        /// <param name="atrValue">ATR value.</param>
        private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // If this is one of the first calculations, just store the values
            if (_prevHmaValue == 0)
            {
                _prevHmaValue = hmaValue;
                return;
            }
            
            if (_prevPrevHmaValue == 0)
            {
                _prevPrevHmaValue = _prevHmaValue;
                _prevHmaValue = hmaValue;
                return;
            }
            
            // Check for Hull MA direction change
            bool directionChangedUp = _prevHmaValue < _prevPrevHmaValue && hmaValue > _prevHmaValue;
            bool directionChangedDown = _prevHmaValue > _prevPrevHmaValue && hmaValue < _prevHmaValue;
            
            // Long entry: Hull MA changed direction from down to up
            if (directionChangedUp && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: Hull MA direction changed up ({_prevPrevHmaValue} -> {_prevHmaValue} -> {hmaValue})");
                
                // Set stop-loss based on ATR
                decimal stopPrice = candle.ClosePrice - (atrValue * AtrMultiplier.Value);
                StartProtection(null, new Unit(candle.ClosePrice - stopPrice, UnitTypes.Absolute), false, true);
            }
            // Short entry: Hull MA changed direction from up to down
            else if (directionChangedDown && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: Hull MA direction changed down ({_prevPrevHmaValue} -> {_prevHmaValue} -> {hmaValue})");
                
                // Set stop-loss based on ATR
                decimal stopPrice = candle.ClosePrice + (atrValue * AtrMultiplier.Value);
                StartProtection(null, new Unit(stopPrice - candle.ClosePrice, UnitTypes.Absolute), false, true);
            }
            
            // Update previous values
            _prevPrevHmaValue = _prevHmaValue;
            _prevHmaValue = hmaValue;
        }
    }
}