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
    /// Strategy that trades based on CCI (Commodity Channel Index) Failure Swing pattern.
    /// A failure swing occurs when CCI reverses direction without crossing through centerline.
    /// </summary>
    public class CciFailureSwingStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _cciPeriod;
        private readonly StrategyParam<decimal> _oversoldLevel;
        private readonly StrategyParam<decimal> _overboughtLevel;
        private readonly StrategyParam<decimal> _stopLossPercent;

        private CommodityChannelIndex _cci;
        
        private decimal _prevCciValue;
        private decimal _prevPrevCciValue;
        private bool _inPosition;
        private Sides _positionSide;

        /// <summary>
        /// Candle type and timeframe for the strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Period for CCI calculation.
        /// </summary>
        public int CciPeriod
        {
            get => _cciPeriod.Value;
            set => _cciPeriod.Value = value;
        }

        /// <summary>
        /// Oversold level for CCI.
        /// </summary>
        public decimal OversoldLevel
        {
            get => _oversoldLevel.Value;
            set => _oversoldLevel.Value = value;
        }

        /// <summary>
        /// Overbought level for CCI.
        /// </summary>
        public decimal OverboughtLevel
        {
            get => _overboughtLevel.Value;
            set => _overboughtLevel.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage from entry price.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CciFailureSwingStrategy"/>.
        /// </summary>
        public CciFailureSwingStrategy()
        {
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                         .SetDisplay("Candle Type", "Type of candles to use for analysis", "General");
            
            _cciPeriod = Param(nameof(CciPeriod), 20)
                        .SetDisplay("CCI Period", "Period for CCI calculation", "CCI Settings")
                        .SetRange(5, 50);
            
            _oversoldLevel = Param(nameof(OversoldLevel), -100m)
                           .SetDisplay("Oversold Level", "CCI level considered oversold", "CCI Settings")
                           .SetRange(-200m, -50m);
            
            _overboughtLevel = Param(nameof(OverboughtLevel), 100m)
                             .SetDisplay("Overbought Level", "CCI level considered overbought", "CCI Settings")
                             .SetRange(50m, 200m);
            
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                              .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection")
                              .SetRange(0.5m, 5m);
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
            
            // Initialize indicators
            _cci = new CommodityChannelIndex { Length = CciPeriod };
            
            _prevCciValue = 0;
            _prevPrevCciValue = 0;
            _inPosition = false;
            _positionSide = Sides.None;
            
            // Create and setup subscription for candles
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicator and processor
            subscription
                .Bind(_cci, ProcessCandle)
                .Start();
            
            // Enable stop-loss protection
            StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
            
            // Setup chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _cci);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal cciValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Need at least 3 CCI values to detect failure swing
            if (_prevCciValue == 0 || _prevPrevCciValue == 0)
            {
                _prevPrevCciValue = _prevCciValue;
                _prevCciValue = cciValue;
                return;
            }
            
            // Detect Bullish Failure Swing:
            // 1. CCI falls below oversold level
            // 2. CCI rises without crossing centerline
            // 3. CCI pulls back but stays above previous low
            // 4. CCI breaks above the high point of first rise
            bool isBullishFailureSwing = _prevPrevCciValue < OversoldLevel &&
                                        _prevCciValue > _prevPrevCciValue &&
                                        cciValue < _prevCciValue &&
                                        cciValue > _prevPrevCciValue;
            
            // Detect Bearish Failure Swing:
            // 1. CCI rises above overbought level
            // 2. CCI falls without crossing centerline
            // 3. CCI bounces up but stays below previous high
            // 4. CCI breaks below the low point of first decline
            bool isBearishFailureSwing = _prevPrevCciValue > OverboughtLevel &&
                                         _prevCciValue < _prevPrevCciValue &&
                                         cciValue > _prevCciValue &&
                                         cciValue < _prevPrevCciValue;
            
            // Trading logic
            if (isBullishFailureSwing && !_inPosition)
            {
                // Enter long position
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                _inPosition = true;
                _positionSide = Sides.Buy;
                
                LogInfo($"Bullish CCI Failure Swing detected. CCI values: {_prevPrevCciValue:F2} -> {_prevCciValue:F2} -> {cciValue:F2}. Long entry at {candle.ClosePrice}");
            }
            else if (isBearishFailureSwing && !_inPosition)
            {
                // Enter short position
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                
                _inPosition = true;
                _positionSide = Sides.Sell;
                
                LogInfo($"Bearish CCI Failure Swing detected. CCI values: {_prevPrevCciValue:F2} -> {_prevCciValue:F2} -> {cciValue:F2}. Short entry at {candle.ClosePrice}");
            }
            
            // Exit conditions
            if (_inPosition)
            {
                // For long positions: exit when CCI crosses above 0
                if (_positionSide == Sides.Buy && cciValue > 0)
                {
                    SellMarket(Math.Abs(Position));
                    _inPosition = false;
                    _positionSide = Sides.None;
                    
                    LogInfo($"Exit signal for long position: CCI ({cciValue:F2}) crossed above 0. Closing at {candle.ClosePrice}");
                }
                // For short positions: exit when CCI crosses below 0
                else if (_positionSide == Sides.Sell && cciValue < 0)
                {
                    BuyMarket(Math.Abs(Position));
                    _inPosition = false;
                    _positionSide = Sides.None;
                    
                    LogInfo($"Exit signal for short position: CCI ({cciValue:F2}) crossed below 0. Closing at {candle.ClosePrice}");
                }
            }
            
            // Update CCI values for next iteration
            _prevPrevCciValue = _prevCciValue;
            _prevCciValue = cciValue;
        }
    }
}