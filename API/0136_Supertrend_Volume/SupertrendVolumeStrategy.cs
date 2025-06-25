namespace StockSharp.Strategies.Samples
{
    using System;
    using System.Collections.Generic;
    
    using Ecng.Common;
    
    using StockSharp.Algo;
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Indicators;
    using StockSharp.Algo.Strategies;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
    
    /// <summary>
    /// Strategy that combines the Supertrend indicator with volume analysis to identify
    /// strong trend-following trading opportunities.
    /// </summary>
    public class SupertrendVolumeStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _supertrendPeriod;
        private readonly StrategyParam<decimal> _supertrendMultiplier;
        private readonly StrategyParam<int> _volumePeriod;
        private readonly StrategyParam<decimal> _volumeThreshold;
        
        private AverageTrueRange _atr;
        private SimpleMovingAverage _volumeSma;
        
        // Additional variables for Supertrend calculation
        private decimal? _upperBand;
        private decimal? _lowerBand;
        private decimal? _supertrend;
        private bool? _isBullish;
        
        /// <summary>
        /// Data type for candles.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }
        
        /// <summary>
        /// Period for Supertrend ATR calculation.
        /// </summary>
        public int SupertrendPeriod
        {
            get => _supertrendPeriod.Value;
            set => _supertrendPeriod.Value = value;
        }
        
        /// <summary>
        /// Multiplier for Supertrend ATR calculation.
        /// </summary>
        public decimal SupertrendMultiplier
        {
            get => _supertrendMultiplier.Value;
            set => _supertrendMultiplier.Value = value;
        }
        
        /// <summary>
        /// Period for volume moving average calculation.
        /// </summary>
        public int VolumePeriod
        {
            get => _volumePeriod.Value;
            set => _volumePeriod.Value = value;
        }
        
        /// <summary>
        /// Volume threshold multiplier for volume confirmation.
        /// </summary>
        public decimal VolumeThreshold
        {
            get => _volumeThreshold.Value;
            set => _volumeThreshold.Value = value;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SupertrendVolumeStrategy"/>.
        /// </summary>
        public SupertrendVolumeStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                          .SetDisplay("Candle Type", "Type of candles to use", "General");
                          
            _supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
                                .SetRange(5, 30)
                                .SetDisplay("Supertrend Period", "Period for Supertrend ATR calculation", "Supertrend Settings")
                                .SetCanOptimize(true);
                                
            _supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
                                    .SetRange(1.0m, 5.0m)
                                    .SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend ATR calculation", "Supertrend Settings")
                                    .SetCanOptimize(true);
                                    
            _volumePeriod = Param(nameof(VolumePeriod), 20)
                            .SetRange(5, 50)
                            .SetDisplay("Volume Period", "Period for volume moving average calculation", "Volume Settings")
                            .SetCanOptimize(true);
                            
            _volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
                               .SetRange(1.0m, 3.0m)
                               .SetDisplay("Volume Threshold", "Volume threshold multiplier for volume confirmation", "Volume Settings")
                               .SetCanOptimize(true);
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
            _atr = new AverageTrueRange { Length = SupertrendPeriod };
            _volumeSma = new SimpleMovingAverage { Length = VolumePeriod };
            
            // Reset Supertrend variables
            _upperBand = null;
            _lowerBand = null;
            _supertrend = null;
            _isBullish = null;
            
            // Create candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind the indicators and candle processor
            subscription
                .Bind(_atr, ProcessCandle)
                .Start();
                
            // Set up chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // ATR area
                var atrArea = CreateChartArea();
                DrawIndicator(atrArea, _atr);
                
                DrawOwnTrades(area);
            }
        }
        
        /// <summary>
        /// Process incoming candle with ATR value.
        /// </summary>
        /// <param name="candle">Candle to process.</param>
        /// <param name="atrValue">ATR value.</param>
        private void ProcessCandle(ICandleMessage candle, decimal atrValue)
        {
            if (candle.State != CandleStates.Finished)
                return;
                
            // Calculate volume SMA
            var volumeCandle = new CandleMessage
            {
                OpenPrice = candle.TotalVolume,
                HighPrice = candle.TotalVolume,
                LowPrice = candle.TotalVolume,
                ClosePrice = candle.TotalVolume,
                TotalVolume = candle.TotalVolume,
                OpenTime = candle.OpenTime,
                State = candle.State
            };
            
            var volumeSmaValue = _volumeSma.Process(volumeCandle).GetValue<decimal>();
            
            if (!IsFormedAndOnlineAndAllowTrading() || !_atr.IsFormed || !_volumeSma.IsFormed)
                return;
                
            // Calculate Supertrend
            CalculateSupertrend(candle, atrValue);
            
            if (_supertrend == null || _isBullish == null)
                return;
                
            // Check if current volume is above threshold compared to average volume
            bool volumeConfirmation = candle.TotalVolume > volumeSmaValue * VolumeThreshold;
            
            // Trading logic
            if (volumeConfirmation)
            {
                if (_isBullish.Value && candle.ClosePrice > _supertrend.Value)
                {
                    // Bullish Supertrend with volume confirmation - Long signal
                    if (Position <= 0)
                    {
                        BuyMarket(Volume + Math.Abs(Position));
                        LogInfo($"Buy signal: Bullish Supertrend ({_supertrend.Value:F4}) with volume confirmation ({candle.TotalVolume} > {volumeSmaValue * VolumeThreshold})");
                    }
                }
                else if (!_isBullish.Value && candle.ClosePrice < _supertrend.Value)
                {
                    // Bearish Supertrend with volume confirmation - Short signal
                    if (Position >= 0)
                    {
                        SellMarket(Volume + Math.Abs(Position));
                        LogInfo($"Sell signal: Bearish Supertrend ({_supertrend.Value:F4}) with volume confirmation ({candle.TotalVolume} > {volumeSmaValue * VolumeThreshold})");
                    }
                }
            }
            
            // Exit logic
            if ((Position > 0 && !_isBullish.Value && candle.ClosePrice < _supertrend.Value) ||
                (Position < 0 && _isBullish.Value && candle.ClosePrice > _supertrend.Value))
            {
                if (Position > 0)
                {
                    SellMarket(Math.Abs(Position));
                    LogInfo($"Exit long: Supertrend turned bearish ({_supertrend.Value:F4})");
                }
                else if (Position < 0)
                {
                    BuyMarket(Math.Abs(Position));
                    LogInfo($"Exit short: Supertrend turned bullish ({_supertrend.Value:F4})");
                }
            }
        }
        
        /// <summary>
        /// Calculate Supertrend indicator values.
        /// </summary>
        /// <param name="candle">Candle to process.</param>
        /// <param name="atrValue">ATR value.</param>
        private void CalculateSupertrend(ICandleMessage candle, decimal atrValue)
        {
            // Calculate basic price
            var basicPrice = (candle.HighPrice + candle.LowPrice) / 2;
            
            // Calculate upper and lower bands
            var newUpperBand = basicPrice + (SupertrendMultiplier * atrValue);
            var newLowerBand = basicPrice - (SupertrendMultiplier * atrValue);
            
            // Initialize values on first iteration
            if (_upperBand == null || _lowerBand == null || _supertrend == null || _isBullish == null)
            {
                _upperBand = newUpperBand;
                _lowerBand = newLowerBand;
                _supertrend = newUpperBand;
                _isBullish = false;
                return;
            }
            
            // Update upper band
            if (newUpperBand < _upperBand || candle.ClosePrice > _upperBand)
                _upperBand = newUpperBand;
            
            // Update lower band
            if (newLowerBand > _lowerBand || candle.ClosePrice < _lowerBand)
                _lowerBand = newLowerBand;
            
            // Update Supertrend and trend direction
            if (_supertrend == _upperBand)
            {
                // Previous trend was bearish
                if (candle.ClosePrice > _upperBand)
                {
                    // Trend changed to bullish
                    _supertrend = _lowerBand;
                    _isBullish = true;
                }
                else
                {
                    // Trend remains bearish
                    _supertrend = _upperBand;
                    _isBullish = false;
                }
            }
            else
            {
                // Previous trend was bullish
                if (candle.ClosePrice < _lowerBand)
                {
                    // Trend changed to bearish
                    _supertrend = _upperBand;
                    _isBullish = false;
                }
                else
                {
                    // Trend remains bullish
                    _supertrend = _lowerBand;
                    _isBullish = true;
                }
            }
        }
        
        /// <inheritdoc />
        protected override void OnStopped()
        {
            _upperBand = null;
            _lowerBand = null;
            _supertrend = null;
            _isBullish = null;
            
            base.OnStopped();
        }
    }
}