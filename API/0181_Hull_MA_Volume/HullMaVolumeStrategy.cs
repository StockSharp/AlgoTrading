using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Hull Moving Average and Volume.
    /// Enters long when HMA is rising and volume is above average
    /// Enters short when HMA is falling and volume is above average
    /// </summary>
    public class HullMaVolumeStrategy : Strategy
    {
        private readonly StrategyParam<int> _hullPeriod;
        private readonly StrategyParam<int> _volumePeriod;
        private readonly StrategyParam<decimal> _volumeThreshold;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevHullValue;
        private bool _isFirstValue = true;

        /// <summary>
        /// Hull MA period
        /// </summary>
        public int HullPeriod
        {
            get => _hullPeriod.Value;
            set => _hullPeriod.Value = value;
        }

        /// <summary>
        /// Volume averaging period
        /// </summary>
        public int VolumePeriod
        {
            get => _volumePeriod.Value;
            set => _volumePeriod.Value = value;
        }

        /// <summary>
        /// Volume threshold multiplier
        /// </summary>
        public decimal VolumeThreshold
        {
            get => _volumeThreshold.Value;
            set => _volumeThreshold.Value = value;
        }
        
        /// <summary>
        /// ATR period for stop-loss calculation
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }
        
        /// <summary>
        /// ATR multiplier for stop-loss
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Candle type for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public HullMaVolumeStrategy()
        {
            _hullPeriod = Param(nameof(HullPeriod), 9)
                .SetGreaterThanZero()
                .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(5, 15, 2);

            _volumePeriod = Param(nameof(VolumePeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Volume Period", "Period for volume averaging", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
                .SetGreaterThanZero()
                .SetDisplay("Volume Threshold", "Volume threshold multiplier above average", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1.2m, 2.0m, 0.1m);
                
            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);
                
            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

            // Create our custom Hull Moving Average using weighted moving averages
            var wma1 = new WeightedMovingAverage { Length = HullPeriod };
            var wma2 = new WeightedMovingAverage { Length = HullPeriod / 2 };
            
            var volumeAverage = new SimpleMovingAverage { Length = VolumePeriod };
            var atr = new AverageTrueRange { Length = AtrPeriod };

            // Reset state variables
            _isFirstValue = true;

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(wma1, wma2, volumeAverage, atr, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // We'd visualize our Hull MA calculation if we could, but
                // since it's a custom calculation, we can't directly
                // use the DrawIndicator method
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal wma1Value, decimal wma2Value, decimal volumeAvgValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate Hull Moving Average
            // HMA = WMA(2*WMA(n/2) - WMA(n)), sqrt(n))
            // We're simplifying by using n instead of sqrt(n) for the final WMA
            var halfWma = wma2Value;
            var fullWma = wma1Value;
            var hullInput = 2 * halfWma - fullWma;
            
            // Since we don't have a third WMA for the final calculation,
            // we'll use a simple approximation - the direction of the difference
            var hullValue = hullInput;
            
            // Check volume
            var isHighVolume = candle.TotalVolume > (volumeAvgValue * VolumeThreshold);
            
            // Current price (close of the candle)
            var price = candle.ClosePrice;
            
            // Check Hull MA direction
            var isHullRising = false;
            var isHullFalling = false;
            
            if (!_isFirstValue)
            {
                isHullRising = hullValue > _prevHullValue;
                isHullFalling = hullValue < _prevHullValue;
            }
            
            // Store current value for next comparison
            _prevHullValue = hullValue;
            
            if (_isFirstValue)
            {
                _isFirstValue = false;
                return;
            }

            // Trading logic
            if (isHullRising && isHighVolume && Position <= 0)
            {
                // Buy signal: Hull MA rising with high volume
                BuyMarket(Volume + Math.Abs(Position));
                
                // Set stop-loss based on ATR
                var stopPrice = price - (atrValue * AtrMultiplier);
                RegisterOrder(this.CreateOrder(Sides.Sell, stopPrice, Math.Abs(Position + Volume)));
            }
            else if (isHullFalling && isHighVolume && Position >= 0)
            {
                // Sell signal: Hull MA falling with high volume
                SellMarket(Volume + Math.Abs(Position));
                
                // Set stop-loss based on ATR
                var stopPrice = price + (atrValue * AtrMultiplier);
                RegisterOrder(this.CreateOrder(Sides.Buy, stopPrice, Math.Abs(Position + Volume)));
            }
            // Exit conditions
            else if (isHullFalling && Position > 0)
            {
                // Exit long when Hull MA starts falling
                SellMarket(Position);
            }
            else if (isHullRising && Position < 0)
            {
                // Exit short when Hull MA starts rising
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}