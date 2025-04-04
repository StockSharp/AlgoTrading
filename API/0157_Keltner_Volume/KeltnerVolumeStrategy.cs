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
    /// Implementation of strategy #157 - Keltner Channels + Volume.
    /// Buy when price breaks above upper Keltner Channel with above average volume.
    /// Sell when price breaks below lower Keltner Channel with above average volume.
    /// </summary>
    public class KeltnerVolumeStrategy : Strategy
    {
        private readonly StrategyParam<int> _emaPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _multiplier;
        private readonly StrategyParam<int> _volumeAvgPeriod;
        private readonly StrategyParam<Unit> _stopLoss;
        private readonly StrategyParam<DataType> _candleType;

        // For volume tracking
        private decimal _averageVolume;
        private int _volumeCounter;

        // Last price flags for detecting crossovers
        private decimal _lastPrice;
        
        /// <summary>
        /// EMA period for Keltner Channels.
        /// </summary>
        public int EmaPeriod
        {
            get => _emaPeriod.Value;
            set => _emaPeriod.Value = value;
        }

        /// <summary>
        /// ATR period for Keltner Channels.
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// Multiplier for Keltner Channels (how many ATRs from EMA).
        /// </summary>
        public decimal Multiplier
        {
            get => _multiplier.Value;
            set => _multiplier.Value = value;
        }

        /// <summary>
        /// Volume average period.
        /// </summary>
        public int VolumeAvgPeriod
        {
            get => _volumeAvgPeriod.Value;
            set => _volumeAvgPeriod.Value = value;
        }

        /// <summary>
        /// Stop-loss value.
        /// </summary>
        public Unit StopLoss
        {
            get => _stopLoss.Value;
            set => _stopLoss.Value = value;
        }

        /// <summary>
        /// Candle type used for strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize <see cref="KeltnerVolumeStrategy"/>.
        /// </summary>
        public KeltnerVolumeStrategy()
        {
            _emaPeriod = Param(nameof(EmaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("EMA Period", "EMA period for center line", "Keltner Parameters");

            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "ATR period for channel width", "Keltner Parameters");

            _multiplier = Param(nameof(Multiplier), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("ATR Multiplier", "Multiplier for ATR to define channel width", "Keltner Parameters");

            _volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Volume Average Period", "Period for volume moving average", "Volume Parameters");

            _stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Atr))
                .SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management");

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Candle type for strategy", "General");

            _averageVolume = 0;
            _volumeCounter = 0;
            _lastPrice = 0;
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
            var ema = new ExponentialMovingAverage { Length = EmaPeriod };
            var atr = new AverageTrueRange { Length = AtrPeriod };
            
            // Custom Keltner Channels calculation will be done in the processing method
            // as we need both EMA and ATR values together

            // Reset volume tracking
            _averageVolume = 0;
            _volumeCounter = 0;
            _lastPrice = 0;

            // Setup candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicators to candles
            subscription
                .Bind(ema, atr, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                // EMA and bands will be drawn in the indicator handler
                DrawIndicator(area, ema);
                DrawOwnTrades(area);
            }

            // Start protective orders
            StartProtection(StopLoss);
        }

        private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate Keltner Channels
            var upperBand = emaValue + (Multiplier * atrValue);
            var lowerBand = emaValue - (Multiplier * atrValue);

            // Update average volume calculation
            var currentVolume = candle.TotalVolume;
            
            if (_volumeCounter < VolumeAvgPeriod)
            {
                _volumeCounter++;
                _averageVolume = ((_averageVolume * (_volumeCounter - 1)) + currentVolume) / _volumeCounter;
            }
            else
            {
                _averageVolume = (_averageVolume * (VolumeAvgPeriod - 1) + currentVolume) / VolumeAvgPeriod;
            }

            // Check if volume is above average
            var isVolumeAboveAverage = currentVolume > _averageVolume;

            LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, EMA: {emaValue}, " +
                   $"Upper Band: {upperBand}, Lower Band: {lowerBand}, " +
                   $"Volume: {currentVolume}, Avg Volume: {_averageVolume}");

            // Check crossovers - only valid after we have a last price
            var currentPrice = candle.ClosePrice;
            
            // Skip if this is the first processed candle
            if (_lastPrice != 0)
            {
                // Trading rules
                // Check Upper Band breakout with volume confirmation
                if (currentPrice > upperBand && _lastPrice <= upperBand && isVolumeAboveAverage && Position <= 0)
                {
                    // Buy signal - price breaks above upper band with high volume
                    var volume = Volume + Math.Abs(Position);
                    BuyMarket(volume);
                    
                    LogInfo($"Buy signal: Price breaks above upper band with high volume. Volume: {volume}");
                }
                // Check Lower Band breakdown with volume confirmation
                else if (currentPrice < lowerBand && _lastPrice >= lowerBand && isVolumeAboveAverage && Position >= 0)
                {
                    // Sell signal - price breaks below lower band with high volume
                    var volume = Volume + Math.Abs(Position);
                    SellMarket(volume);
                    
                    LogInfo($"Sell signal: Price breaks below lower band with high volume. Volume: {volume}");
                }
                // Exit conditions
                else if (currentPrice < emaValue && Position > 0)
                {
                    // Exit long when price moves below EMA (middle line)
                    SellMarket(Position);
                    
                    LogInfo($"Exit long position: Price moved below EMA. Position: {Position}");
                }
                else if (currentPrice > emaValue && Position < 0)
                {
                    // Exit short when price moves above EMA (middle line)
                    BuyMarket(Math.Abs(Position));
                    
                    LogInfo($"Exit short position: Price moved above EMA. Position: {Position}");
                }
            }
            
            // Update last price
            _lastPrice = currentPrice;
        }
    }
}
