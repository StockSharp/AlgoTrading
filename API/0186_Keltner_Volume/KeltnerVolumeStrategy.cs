using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Keltner Channels + Volume strategy.
    /// Strategy enters trades when price breaks out of Keltner Channels with increased volume.
    /// </summary>
    public class KeltnerVolumeStrategy : Strategy
    {
        private readonly StrategyParam<int> _emaPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<int> _volumeLookback;
        private readonly StrategyParam<decimal> _volumeThreshold;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossAtr;

        // Indicators
        private KeltnerChannel _keltner;
        private SimpleMovingAverage _volumeSma;
        private AverageTrueRange _atr;

        /// <summary>
        /// EMA period for Keltner Channel center line.
        /// </summary>
        public int EmaPeriod
        {
            get => _emaPeriod.Value;
            set => _emaPeriod.Value = value;
        }

        /// <summary>
        /// ATR period for Keltner Channel bands.
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// ATR multiplier for Keltner Channel bands.
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Volume lookback period for the average volume calculation.
        /// </summary>
        public int VolumeLookback
        {
            get => _volumeLookback.Value;
            set => _volumeLookback.Value = value;
        }

        /// <summary>
        /// Volume threshold multiplier over average volume.
        /// </summary>
        public decimal VolumeThreshold
        {
            get => _volumeThreshold.Value;
            set => _volumeThreshold.Value = value;
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
        /// Stop-loss in ATR multiples.
        /// </summary>
        public decimal StopLossAtr
        {
            get => _stopLossAtr.Value;
            set => _stopLossAtr.Value = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeltnerVolumeStrategy()
        {
            _emaPeriod = Param(nameof(EmaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("EMA Period", "EMA period for Keltner middle line", "Keltner Channels")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "ATR period for Keltner bands", "Keltner Channels")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("ATR Multiplier", "ATR multiplier for Keltner bands", "Keltner Channels")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

            _volumeLookback = Param(nameof(VolumeLookback), 20)
                .SetGreaterThanZero()
                .SetDisplay("Volume Lookback", "Periods to calculate average volume", "Volume")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
                .SetGreaterThanZero()
                .SetDisplay("Volume Threshold", "Multiplier above average volume to trigger entry", "Volume")
                .SetCanOptimize(true)
                .SetOptimize(1.2m, 2.5m, 0.3m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossAtr = Param(nameof(StopLossAtr), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss ATR", "Stop loss in ATR multiples", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);
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

            // Create indicators
            _keltner = new KeltnerChannel
            {
                Length = EmaPeriod,
                ATRLength = AtrPeriod,
                Multiplier = AtrMultiplier
            };

            _volumeSma = new SimpleMovingAverage
            {
                Length = VolumeLookback
            };

            _atr = new AverageTrueRange
            {
                Length = AtrPeriod
            };

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(_keltner, _atr, ProcessCandle)
                .Start();
                
            // Subscribe to process volume indicator separately
            subscription
                .BindEx(_volumeSma, ProcessVolume)
                .Start();

            // Setup chart
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _keltner);
                DrawOwnTrades(area);
            }
        }

        // This method will be called for each candle to update the volume indicator
        private void ProcessVolume(ICandleMessage candle, IIndicatorValue volumeSmaValue)
        {
            // Process volume data if needed
        }

        private void ProcessCandle(
            ICandleMessage candle, 
            (decimal middle, decimal upper, decimal lower) keltnerValues,
            decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Get Keltner Channel values
            decimal middle = keltnerValues.middle;
            decimal upper = keltnerValues.upper;
            decimal lower = keltnerValues.lower;
            
            // Get ATR value for stop loss
            decimal atr = atrValue;

            // Calculate volume increase
            decimal averageVolume = _volumeSma.GetCurrentValue();
            bool isHighVolume = candle.TotalVolume > averageVolume * VolumeThreshold;
            
            // Determine price breakout of Keltner Channels
            bool priceAboveUpper = candle.ClosePrice > upper;
            bool priceBelowLower = candle.ClosePrice < lower;

            // Trading logic:
            // Buy when price breaks above upper Keltner band with high volume
            if (priceAboveUpper && isHighVolume && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: Price={candle.ClosePrice}, Upper Keltner={upper}, Volume={candle.TotalVolume}, Avg Volume={averageVolume}");
            }
            // Sell when price breaks below lower Keltner band with high volume
            else if (priceBelowLower && isHighVolume && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: Price={candle.ClosePrice}, Lower Keltner={lower}, Volume={candle.TotalVolume}, Avg Volume={averageVolume}");
            }
            // Exit long position when price falls below middle line
            else if (Position > 0 && candle.ClosePrice < middle)
            {
                SellMarket(Math.Abs(Position));
                LogInfo($"Long exit: Price={candle.ClosePrice}, Middle Keltner={middle}");
            }
            // Exit short position when price rises above middle line
            else if (Position < 0 && candle.ClosePrice > middle)
            {
                BuyMarket(Math.Abs(Position));
                LogInfo($"Short exit: Price={candle.ClosePrice}, Middle Keltner={middle}");
            }

            // Set ATR-based stop loss for new positions
            if (Position != 0)
            {
                decimal stopLossValue = StopLossAtr * atr;
                decimal stopLossPercent = stopLossValue / candle.ClosePrice * 100;
                
                StartProtection(
                    takeProfit: new Unit(0, UnitTypes.Absolute),  // No take profit, use strategy rules
                    stopLoss: new Unit(stopLossPercent, UnitTypes.Percent)
                );
            }
        }
    }
}