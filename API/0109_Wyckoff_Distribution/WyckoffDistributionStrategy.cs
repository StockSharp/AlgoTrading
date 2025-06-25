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
    /// Strategy based on Wyckoff Distribution pattern, which identifies a period of institutional distribution
    /// that leads to a downward price movement.
    /// </summary>
    public class WyckoffDistributionStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _volumeAvgPeriod;
        private readonly StrategyParam<int> _highestPeriod;
        private readonly StrategyParam<decimal> _stopLossPercent;

        private SimpleMovingAverage _ma;
        private SimpleMovingAverage _volumeAvg;
        private Highest _highest;
        private Lowest _lowest;
        
        private enum WyckoffPhase
        {
            None,
            PhaseA,  // Buying climax, automatic reaction, secondary test
            PhaseB,  // Distribution, top building
            PhaseC,  // Upthrust, test of resistance
            PhaseD,  // Sign of weakness, failed test
            PhaseE   // Markdown, price decline
        }
        
        private WyckoffPhase _currentPhase = WyckoffPhase.None;
        private decimal _lastRangeHigh;
        private decimal _lastRangeLow;
        private int _sidewaysCount;
        private decimal _upthrustHigh;
        private bool _positionOpened;

        /// <summary>
        /// Candle type and timeframe for the strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Period for moving average calculation.
        /// </summary>
        public int MaPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Period for volume average calculation.
        /// </summary>
        public int VolumeAvgPeriod
        {
            get => _volumeAvgPeriod.Value;
            set => _volumeAvgPeriod.Value = value;
        }

        /// <summary>
        /// Period for highest/lowest calculation.
        /// </summary>
        public int HighestPeriod
        {
            get => _highestPeriod.Value;
            set => _highestPeriod.Value = value;
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
        /// Initializes a new instance of the <see cref="WyckoffDistributionStrategy"/>.
        /// </summary>
        public WyckoffDistributionStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                         .SetDisplay("Candle Type", "Type of candles to use for analysis", "General");
            
            _maPeriod = Param(nameof(MaPeriod), 20)
                       .SetDisplay("MA Period", "Period for moving average calculation", "Trend")
                       .SetRange(10, 50);
            
            _volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
                              .SetDisplay("Volume Avg Period", "Period for volume average calculation", "Volume")
                              .SetRange(10, 50);
            
            _highestPeriod = Param(nameof(HighestPeriod), 20)
                            .SetDisplay("High/Low Period", "Period for high/low calculation", "Range")
                            .SetRange(10, 50);
            
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                              .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection")
                              .SetRange(1m, 5m);
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
            
            // Initialize indicators
            _ma = new SimpleMovingAverage { Length = MaPeriod };
            _volumeAvg = new SimpleMovingAverage { Length = VolumeAvgPeriod };
            _highest = new Highest { Length = HighestPeriod };
            _lowest = new Lowest { Length = HighestPeriod };
            
            _currentPhase = WyckoffPhase.None;
            _sidewaysCount = 0;
            _positionOpened = false;
            
            // Create and setup subscription for candles
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators and processor
            subscription
                .Bind(_ma, _volumeAvg, _highest, _lowest, ProcessCandle)
                .Start();
            
            // Enable stop-loss protection
            StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
            
            // Setup chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _ma);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal ma, decimal volumeAvg, decimal highest, decimal lowest)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Update range values
            _lastRangeHigh = highest;
            _lastRangeLow = lowest;
            
            // Determine candle characteristics
            bool isBullish = candle.ClosePrice > candle.OpenPrice;
            bool isBearish = candle.ClosePrice < candle.OpenPrice;
            bool highVolume = candle.TotalVolume > volumeAvg * 1.5m;
            bool priceAboveMA = candle.ClosePrice > ma;
            bool priceBelowMA = candle.ClosePrice < ma;
            bool isNarrowRange = (candle.HighPrice - candle.LowPrice) < (highest - lowest) * 0.3m;
            
            // State machine for Wyckoff Distribution phases
            switch (_currentPhase)
            {
                case WyckoffPhase.None:
                    // Look for Phase A: Buying climax (high volume, wide range up bar)
                    if (isBullish && highVolume && candle.ClosePrice > highest)
                    {
                        _currentPhase = WyckoffPhase.PhaseA;
                        LogInfo($"Wyckoff Phase A detected: Buying climax at {candle.ClosePrice}");
                    }
                    break;
                    
                case WyckoffPhase.PhaseA:
                    // Look for automatic reaction (pullback from buying climax)
                    if (isBearish && candle.ClosePrice < ma)
                    {
                        _currentPhase = WyckoffPhase.PhaseB;
                        LogInfo($"Entering Wyckoff Phase B: Automatic reaction at {candle.ClosePrice}");
                        _sidewaysCount = 0;
                    }
                    break;
                    
                case WyckoffPhase.PhaseB:
                    // Phase B is characterized by sideways movement (distribution)
                    if (isNarrowRange && candle.ClosePrice > _lastRangeLow && candle.ClosePrice < _lastRangeHigh)
                    {
                        _sidewaysCount++;
                        
                        // After sufficient sideways movement, look for Phase C
                        if (_sidewaysCount >= 5)
                        {
                            _currentPhase = WyckoffPhase.PhaseC;
                            LogInfo($"Entering Wyckoff Phase C: Distribution complete after {_sidewaysCount} sideways candles");
                        }
                    }
                    else
                    {
                        _sidewaysCount = 0; // Reset if we don't see sideways movement
                    }
                    break;
                    
                case WyckoffPhase.PhaseC:
                    // Phase C includes an upthrust (price briefly goes above resistance)
                    if (candle.HighPrice > _lastRangeHigh && candle.ClosePrice < _lastRangeHigh)
                    {
                        _upthrustHigh = candle.HighPrice;
                        _currentPhase = WyckoffPhase.PhaseD;
                        LogInfo($"Entering Wyckoff Phase D: Upthrust detected at {_upthrustHigh}");
                    }
                    break;
                    
                case WyckoffPhase.PhaseD:
                    // Phase D shows sign of weakness (strong move down with volume)
                    if (isBearish && highVolume && priceBelowMA)
                    {
                        _currentPhase = WyckoffPhase.PhaseE;
                        LogInfo($"Entering Wyckoff Phase E: Sign of weakness detected at {candle.ClosePrice}");
                    }
                    break;
                    
                case WyckoffPhase.PhaseE:
                    // Phase E is the markdown phase where we enter our position
                    if (isBearish && priceBelowMA && !_positionOpened)
                    {
                        // Enter short position
                        var volume = Volume + Math.Abs(Position);
                        SellMarket(volume);
                        
                        _positionOpened = true;
                        LogInfo($"Wyckoff Distribution complete. Short entry at {candle.ClosePrice}");
                    }
                    break;
            }
            
            // Exit conditions
            if (_positionOpened && Position < 0)
            {
                // Exit when price drops below previous low (target achieved)
                if (candle.LowPrice < _lastRangeLow)
                {
                    BuyMarket(Math.Abs(Position));
                    _positionOpened = false;
                    _currentPhase = WyckoffPhase.None; // Reset the pattern detection
                    
                    LogInfo($"Exit signal: Price broke below range low ({_lastRangeLow}). Closed short position at {candle.ClosePrice}");
                }
                // Exit also if price rises back above MA (failed pattern)
                else if (priceAboveMA)
                {
                    BuyMarket(Math.Abs(Position));
                    _positionOpened = false;
                    _currentPhase = WyckoffPhase.None; // Reset the pattern detection
                    
                    LogInfo($"Exit signal: Price rose above MA. Pattern may have failed. Closed short position at {candle.ClosePrice}");
                }
            }
        }
    }
}