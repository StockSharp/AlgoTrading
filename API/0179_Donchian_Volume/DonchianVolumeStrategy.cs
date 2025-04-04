using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Donchian Channels and Volume.
    /// Enters long when price breaks above Donchian high with above-average volume
    /// Enters short when price breaks below Donchian low with above-average volume
    /// </summary>
    public class DonchianVolumeStrategy : Strategy
    {
        private readonly StrategyParam<int> _donchianPeriod;
        private readonly StrategyParam<int> _volumePeriod;
        private readonly StrategyParam<decimal> _volumeThreshold;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevHigh;
        private decimal _prevLow;
        private decimal _avgVolume;

        /// <summary>
        /// Donchian Channels period
        /// </summary>
        public int DonchianPeriod
        {
            get => _donchianPeriod.Value;
            set => _donchianPeriod.Value = value;
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
        /// Stop-loss percentage
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
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
        public DonchianVolumeStrategy()
        {
            _donchianPeriod = Param(nameof(DonchianPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Donchian Period", "Period for Donchian Channels", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

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

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

            // Create indicators
            var donchianHigh = new Highest { Length = DonchianPeriod };
            var donchianLow = new Lowest { Length = DonchianPeriod };
            var volumeAverage = new SimpleMovingAverage { Length = VolumePeriod };

            // Initialize variables
            _prevHigh = 0;
            _prevLow = decimal.MaxValue;
            _avgVolume = 0;

            // Enable position protection with stop-loss
            StartProtection(
                takeProfit: new Unit(0), // No take profit
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(donchianHigh, donchianLow, volumeAverage, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // We don't have built-in Donchian Channels indicator,
                // but we could visualize high and low as separate lines
                DrawIndicator(area, donchianHigh);
                DrawIndicator(area, donchianLow);
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue, decimal volumeAvgValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Get Donchian Channels values
            var highestHigh = highValue;
            var lowestLow = lowValue;
            
            // Get volume average and check if current volume is above threshold
            _avgVolume = volumeAvgValue;
            var isHighVolume = candle.TotalVolume > (_avgVolume * VolumeThreshold);
            
            // Current price (close of the candle)
            var price = candle.ClosePrice;
            
            // Check for breakouts
            var isHighBreakout = price > _prevHigh && price >= highestHigh;
            var isLowBreakout = price < _prevLow && price <= lowestLow;
            
            // Store current values for next comparison
            _prevHigh = highestHigh;
            _prevLow = lowestLow;

            // Trading logic
            if (isHighBreakout && isHighVolume && Position <= 0)
            {
                // Buy signal: price breaks above Donchian high with high volume
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (isLowBreakout && isHighVolume && Position >= 0)
            {
                // Sell signal: price breaks below Donchian low with high volume
                SellMarket(Volume + Math.Abs(Position));
            }
            // Exit conditions
            else if (price <= lowestLow && Position > 0)
            {
                // Exit long when price drops to lowest low
                SellMarket(Position);
            }
            else if (price >= highestHigh && Position < 0)
            {
                // Exit short when price rises to highest high
                BuyMarket(Math.Abs(Position));
            }
            // Middle line exit (Donchian middle)
            else
            {
                var middleLine = (highestHigh + lowestLow) / 2;
                
                if (price < middleLine && Position > 0)
                {
                    // Exit long when price falls below middle line
                    SellMarket(Position);
                }
                else if (price > middleLine && Position < 0)
                {
                    // Exit short when price rises above middle line
                    BuyMarket(Math.Abs(Position));
                }
            }
        }
    }
}