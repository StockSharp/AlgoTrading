using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy that uses Donchian Channels to identify breakouts
    /// and volume confirmation to filter signals.
    /// Enters positions when price breaks above/below Donchian Channel with increased volume.
    /// </summary>
    public class DonchianVolumeStrategy : Strategy
    {
        private readonly StrategyParam<int> _donchianPeriod;
        private readonly StrategyParam<int> _volumePeriod;
        private readonly StrategyParam<decimal> _volumeMultiplier;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        private decimal _averageVolume;

        /// <summary>
        /// Donchian Channels period.
        /// </summary>
        public int DonchianPeriod
        {
            get => _donchianPeriod.Value;
            set => _donchianPeriod.Value = value;
        }

        /// <summary>
        /// Volume averaging period.
        /// </summary>
        public int VolumePeriod
        {
            get => _volumePeriod.Value;
            set => _volumePeriod.Value = value;
        }

        /// <summary>
        /// Volume multiplier for breakout confirmation.
        /// </summary>
        public decimal VolumeMultiplier
        {
            get => _volumeMultiplier.Value;
            set => _volumeMultiplier.Value = value;
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
        /// Strategy constructor.
        /// </summary>
        public DonchianVolumeStrategy()
        {
            _donchianPeriod = Param(nameof(DonchianPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Donchian Period", "Period of the Donchian Channel", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _volumePeriod = Param(nameof(VolumePeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Volume Period", "Period for volume averaging", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
                .SetGreaterThanZero()
                .SetDisplay("Volume Multiplier", "Multiplier for average volume to confirm breakout", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 2.0m, 0.5m);

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

            _averageVolume = 0;

            // Create indicators
            var donchianHigh = new Highest
            {
                Length = DonchianPeriod
            };

            var donchianLow = new Lowest
            {
                Length = DonchianPeriod
            };

            var volumeAverage = new SimpleMovingAverage
            {
                Length = VolumePeriod
            };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);

            subscription
                .BindEx(volumeAverage, ProcessVolumeAverage)
                .Start();

            subscription
                .Bind(donchianHigh, donchianLow, ProcessDonchian)
                .Start();

            // Setup position protection
            StartProtection(
                takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage
            );

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // Create a composite indicator for visualization purposes
                var donchian = new DonchianChannel
                {
                    Length = DonchianPeriod
                };
                
                DrawIndicator(area, donchian);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process volume average.
        /// </summary>
        private void ProcessVolumeAverage(IIndicatorValue volumeAvgValue)
        {
            if (volumeAvgValue.IsFinal)
            {
                _averageVolume = volumeAvgValue.GetValue<decimal>();
            }
        }

        /// <summary>
        /// Process Donchian Channel values.
        /// </summary>
        private void ProcessDonchian(ICandleMessage candle, decimal highestValue, decimal lowestValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading() || _averageVolume <= 0)
                return;

            // Calculate middle line of Donchian Channel
            var middleLine = (highestValue + lowestValue) / 2;

            // Check if volume condition is met
            var isVolumeHighEnough = candle.TotalVolume > _averageVolume * VolumeMultiplier;

            if (isVolumeHighEnough)
            {
                // Long entry: price breaks above highest high with increased volume
                if (candle.ClosePrice > highestValue && Position <= 0)
                {
                    var volume = Volume + Math.Abs(Position);
                    BuyMarket(volume);
                }
                // Short entry: price breaks below lowest low with increased volume
                else if (candle.ClosePrice < lowestValue && Position >= 0)
                {
                    var volume = Volume + Math.Abs(Position);
                    SellMarket(volume);
                }
            }

            // Exit conditions based on middle line
            if (Position > 0 && candle.ClosePrice < middleLine)
            {
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > middleLine)
            {
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}