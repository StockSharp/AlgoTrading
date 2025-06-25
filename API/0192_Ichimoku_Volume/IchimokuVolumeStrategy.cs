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
    /// Strategy based on Ichimoku Cloud and Volume.
    /// Enters long when price is above Kumo cloud, Tenkan-sen crosses above Kijun-sen, and volume is above average.
    /// Enters short when price is below Kumo cloud, Tenkan-sen crosses below Kijun-sen, and volume is above average.
    /// Exits when price crosses the cloud in the opposite direction.
    /// </summary>
    public class IchimokuVolumeStrategy : Strategy
    {
        private readonly StrategyParam<int> _tenkanPeriod;
        private readonly StrategyParam<int> _kijunPeriod;
        private readonly StrategyParam<int> _senkouSpanBPeriod;
        private readonly StrategyParam<int> _volumePeriod;
        private readonly StrategyParam<DataType> _candleType;

        private IchimokuCloud _ichimoku;
        private Volume _volumeIndicator;
        private SimpleMovingAverage _volumeAverage;
        
        private decimal _prevTenkan;
        private decimal _prevKijun;
        private bool _aboveCloud;
        private bool _belowCloud;

        /// <summary>
        /// Tenkan-sen period.
        /// </summary>
        public int TenkanPeriod
        {
            get => _tenkanPeriod.Value;
            set => _tenkanPeriod.Value = value;
        }

        /// <summary>
        /// Kijun-sen period.
        /// </summary>
        public int KijunPeriod
        {
            get => _kijunPeriod.Value;
            set => _kijunPeriod.Value = value;
        }

        /// <summary>
        /// Senkou Span B period.
        /// </summary>
        public int SenkouSpanBPeriod
        {
            get => _senkouSpanBPeriod.Value;
            set => _senkouSpanBPeriod.Value = value;
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
        /// Candle type for strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IchimokuVolumeStrategy"/>.
        /// </summary>
        public IchimokuVolumeStrategy()
        {
            _tenkanPeriod = Param(nameof(TenkanPeriod), 9)
                .SetDisplayName("Tenkan-sen Period")
                .SetDescription("Period for Tenkan-sen calculation (Conversion Line)")
                .SetCategories("Indicators");

            _kijunPeriod = Param(nameof(KijunPeriod), 26)
                .SetDisplayName("Kijun-sen Period")
                .SetDescription("Period for Kijun-sen calculation (Base Line)")
                .SetCategories("Indicators");

            _senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
                .SetDisplayName("Senkou Span B Period")
                .SetDescription("Period for Senkou Span B calculation (2nd Leading Span)")
                .SetCategories("Indicators");

            _volumePeriod = Param(nameof(VolumePeriod), 20)
                .SetDisplayName("Volume Period")
                .SetDescription("Period for volume average calculation")
                .SetCategories("Indicators");

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                .SetDisplayName("Candle Type")
                .SetDescription("Timeframe of data for strategy")
                .SetCategories("General");
        }

        /// <inheritdoc />
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            return [(Security, CandleType)];
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Create Ichimoku indicator
            _ichimoku = new IchimokuCloud
            {
                TenkanPeriod = TenkanPeriod,
                KijunPeriod = KijunPeriod,
                SenkouSpanBPeriod = SenkouSpanBPeriod
            };

            // Create volume indicators
            _volumeIndicator = new Volume();
            _volumeAverage = new SimpleMovingAverage { Length = VolumePeriod };

            // Initialize variables
            _prevTenkan = 0;
            _prevKijun = 0;
            _aboveCloud = false;
            _belowCloud = false;

            // Enable position protection with Kijun-sen as stop loss
            StartProtection(new Unit(3, UnitTypes.Percent), new Unit(2, UnitTypes.Percent));

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Process candles with indicators
            subscription
                .BindEx(_ichimoku, ProcessCandle)
                .Start();

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _ichimoku);
                DrawOwnTrades(area);

                // Draw volume in separate area
                var volumeArea = CreateChartArea();
                if (volumeArea != null)
                {
                    DrawIndicator(volumeArea, _volumeIndicator);
                    DrawIndicator(volumeArea, _volumeAverage);
                }
            }
        }

        private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Process volume indicators
            var volumeValue = _volumeIndicator.Process(candle).GetValue<decimal>();
            var avgVolume = _volumeAverage.Process(volumeValue).GetValue<decimal>();

            // Extract values from Ichimoku Cloud indicator
            var tenkan = ichimokuValue[IchimokuCloud.TenkanLine];
            var kijun = ichimokuValue[IchimokuCloud.KijunLine];
            var senkouA = ichimokuValue[IchimokuCloud.SenkouSpanA];
            var senkouB = ichimokuValue[IchimokuCloud.SenkouSpanB];
            
            // Determine cloud boundaries
            var upperCloud = Math.Max(senkouA, senkouB);
            var lowerCloud = Math.Min(senkouA, senkouB);

            // Check price position relative to cloud
            var priceAboveCloud = candle.ClosePrice > upperCloud;
            var priceBelowCloud = candle.ClosePrice < lowerCloud;

            // Detect Tenkan/Kijun cross
            bool tenkanCrossedAboveKijun = _prevTenkan <= _prevKijun && tenkan > kijun;
            bool tenkanCrossedBelowKijun = _prevTenkan >= _prevKijun && tenkan < kijun;

            // Check if volume is above average (high volume)
            bool isHighVolume = volumeValue > avgVolume;

            // Check if strategy is ready for trading
            if (!IsFormedAndOnlineAndAllowTrading())
            {
                // Store current values for next candle
                _prevTenkan = tenkan;
                _prevKijun = kijun;
                _aboveCloud = priceAboveCloud;
                _belowCloud = priceBelowCloud;
                return;
            }

            // Trading logic
            if (priceAboveCloud && tenkanCrossedAboveKijun && isHighVolume && Position <= 0)
            {
                // Price above cloud, Tenkan crossed above Kijun with high volume - go long
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (priceBelowCloud && tenkanCrossedBelowKijun && isHighVolume && Position >= 0)
            {
                // Price below cloud, Tenkan crossed below Kijun with high volume - go short
                SellMarket(Volume + Math.Abs(Position));
            }
            else if (Position > 0 && !priceAboveCloud && _aboveCloud)
            {
                // Exit long position when price falls below cloud
                ClosePosition();
            }
            else if (Position < 0 && !priceBelowCloud && _belowCloud)
            {
                // Exit short position when price rises above cloud
                ClosePosition();
            }

            // Store current values for next candle
            _prevTenkan = tenkan;
            _prevKijun = kijun;
            _aboveCloud = priceAboveCloud;
            _belowCloud = priceBelowCloud;
        }
    }
}