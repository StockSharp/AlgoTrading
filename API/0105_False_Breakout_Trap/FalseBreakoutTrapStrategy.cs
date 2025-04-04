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
    /// Strategy that trades false breakouts of support and resistance levels.
    /// </summary>
    public class FalseBreakoutTrapStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _lookbackPeriod;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<decimal> _stopLossPercent;

        private SimpleMovingAverage _ma;
        private Highest _highest;
        private Lowest _lowest;
        
        private decimal _lastHighestValue;
        private decimal _lastLowestValue;
        private bool _breakoutDetected;
        private Sides _breakoutSide;
        private decimal _breakoutPrice;

        /// <summary>
        /// Candle type and timeframe for the strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Period for high/low range detection.
        /// </summary>
        public int LookbackPeriod
        {
            get => _lookbackPeriod.Value;
            set => _lookbackPeriod.Value = value;
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
        /// Stop-loss percentage from entry price.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FalseBreakoutTrapStrategy"/>.
        /// </summary>
        public FalseBreakoutTrapStrategy()
        {
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                         .SetDisplay("Candle Type", "Type of candles to use for analysis", "General");
            
            _lookbackPeriod = Param(nameof(LookbackPeriod), 20)
                             .SetDisplay("Lookback Period", "Period for high/low range detection", "Range")
                             .SetRange(5, 50);
            
            _maPeriod = Param(nameof(MaPeriod), 20)
                       .SetDisplay("MA Period", "Period for moving average calculation", "Trend")
                       .SetRange(5, 50);
            
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
            _ma = new SimpleMovingAverage { Length = MaPeriod };
            _highest = new Highest { Length = LookbackPeriod };
            _lowest = new Lowest { Length = LookbackPeriod };
            
            _breakoutDetected = false;
            _breakoutSide = Sides.None;
            
            // Create and setup subscription for candles
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators and processor
            subscription
                .Bind(_ma, _highest, _lowest, ProcessCandle)
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

        private void ProcessCandle(ICandleMessage candle, decimal ma, decimal highest, decimal lowest)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Store the last highest and lowest values
            _lastHighestValue = highest;
            _lastLowestValue = lowest;
            
            // First, check if we're already tracking a potential false breakout
            if (_breakoutDetected)
            {
                // Check for false breakout confirmation
                if (_breakoutSide == Sides.Buy)
                {
                    // A false upside breakout is confirmed when price falls back below MA
                    if (candle.ClosePrice < ma)
                    {
                        // Enter short position
                        var volume = Volume + Math.Abs(Position);
                        SellMarket(volume);
                        
                        this.AddInfoLog($"False upside breakout confirmed. Short entry at {candle.ClosePrice}. Resistance level: {_lastHighestValue}");
                        
                        // Reset breakout detection
                        _breakoutDetected = false;
                        _breakoutSide = Sides.None;
                    }
                }
                else if (_breakoutSide == Sides.Sell)
                {
                    // A false downside breakout is confirmed when price rises back above MA
                    if (candle.ClosePrice > ma)
                    {
                        // Enter long position
                        var volume = Volume + Math.Abs(Position);
                        BuyMarket(volume);
                        
                        this.AddInfoLog($"False downside breakout confirmed. Long entry at {candle.ClosePrice}. Support level: {_lastLowestValue}");
                        
                        // Reset breakout detection
                        _breakoutDetected = false;
                        _breakoutSide = Sides.None;
                    }
                }
                
                // If the breakout continues beyond our threshold, abandon the false breakout idea
                if (_breakoutSide == Sides.Buy && candle.ClosePrice > _breakoutPrice * 1.01m)
                {
                    this.AddInfoLog($"Breakout appears genuine, not a false breakout. Abandoning the setup.");
                    _breakoutDetected = false;
                    _breakoutSide = Sides.None;
                }
                else if (_breakoutSide == Sides.Sell && candle.ClosePrice < _breakoutPrice * 0.99m)
                {
                    this.AddInfoLog($"Breakout appears genuine, not a false breakout. Abandoning the setup.");
                    _breakoutDetected = false;
                    _breakoutSide = Sides.None;
                }
            }
            else
            {
                // Check for potential breakout
                if (candle.HighPrice > _lastHighestValue)
                {
                    // Potential upside breakout
                    _breakoutDetected = true;
                    _breakoutSide = Sides.Buy;
                    _breakoutPrice = candle.ClosePrice;
                    
                    this.AddInfoLog($"Potential upside breakout detected at {candle.HighPrice}. Watching for false breakout pattern.");
                }
                else if (candle.LowPrice < _lastLowestValue)
                {
                    // Potential downside breakout
                    _breakoutDetected = true;
                    _breakoutSide = Sides.Sell;
                    _breakoutPrice = candle.ClosePrice;
                    
                    this.AddInfoLog($"Potential downside breakout detected at {candle.LowPrice}. Watching for false breakout pattern.");
                }
            }
            
            // Exit conditions based on price crossing the moving average
            if (Position > 0 && candle.ClosePrice < ma)
            {
                // Exit long position when price falls below MA
                SellMarket(Math.Abs(Position));
                this.AddInfoLog($"Exit signal: Price below MA. Closed long position at {candle.ClosePrice}");
            }
            else if (Position < 0 && candle.ClosePrice > ma)
            {
                // Exit short position when price rises above MA
                BuyMarket(Math.Abs(Position));
                this.AddInfoLog($"Exit signal: Price above MA. Closed short position at {candle.ClosePrice}");
            }
        }
    }
}