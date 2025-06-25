using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Volume MA Cross strategy
    /// Long entry: Fast volume MA crosses above slow volume MA
    /// Short entry: Fast volume MA crosses below slow volume MA
    /// Exit: Reverse crossover
    /// </summary>
    public class VolumeMAXrossStrategy : Strategy
    {
        private readonly StrategyParam<int> _fastVolumeMALength;
        private readonly StrategyParam<int> _slowVolumeMALength;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _previousFastVolumeMA;
        private decimal _previousSlowVolumeMA;
        private bool _isFirstValue;

        /// <summary>
        /// Fast Volume MA Length
        /// </summary>
        public int FastVolumeMALength
        {
            get => _fastVolumeMALength.Value;
            set => _fastVolumeMALength.Value = value;
        }

        /// <summary>
        /// Slow Volume MA Length
        /// </summary>
        public int SlowVolumeMALength
        {
            get => _slowVolumeMALength.Value;
            set => _slowVolumeMALength.Value = value;
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
        /// Initialize <see cref="VolumeMAXrossStrategy"/>.
        /// </summary>
        public VolumeMAXrossStrategy()
        {
            _fastVolumeMALength = Param(nameof(FastVolumeMALength), 10)
                .SetGreaterThanZero()
                .SetDisplay("Fast Volume MA Length", "Period for Fast Volume Moving Average", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(5, 20, 5);

            _slowVolumeMALength = Param(nameof(SlowVolumeMALength), 50)
                .SetGreaterThanZero()
                .SetDisplay("Slow Volume MA Length", "Period for Slow Volume Moving Average", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(30, 100, 10);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _previousFastVolumeMA = 0;
            _previousSlowVolumeMA = 0;
            _isFirstValue = true;
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
            var fastVolumeMA = new SimpleMovingAverage { Length = FastVolumeMALength };
            var slowVolumeMA = new SimpleMovingAverage { Length = SlowVolumeMALength };
            var priceMA = new SimpleMovingAverage { Length = FastVolumeMALength }; // Use same period as fast Volume MA

            // Create volume input adapter to use volume as input for MAs
            var volumeAdapter = new DecimalIndicatorValue();

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Regular price MA binding for chart visualization
            subscription
                .Bind(priceMA, ProcessCandle)
                .Start();

            // Subscribe to candle updates and process volume data
            this.WhenCandlesChanged(subscription)
                .Do(candle => {
                    if (candle.State != CandleStates.Finished)
                        return;

                    // Create indicator value based on volume
                    volumeAdapter.Value = candle.TotalVolume;
                    
                    // Process volume through MAs
                    var fastMAValue = fastVolumeMA.Process(volumeAdapter).GetValue<decimal>();
                    var slowMAValue = slowVolumeMA.Process(volumeAdapter).GetValue<decimal>();
                    
                    // Process the volume MAs
                    ProcessVolumeMAs(candle, fastMAValue, slowMAValue, priceMA.GetCurrentValue());
                })
                .Apply(this);

            // Configure protection
            StartProtection(
                takeProfit: new Unit(3, UnitTypes.Percent),
                stopLoss: new Unit(2, UnitTypes.Percent)
            );

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, priceMA);
                DrawOwnTrades(area);
            }
        }
        
        private void ProcessCandle(ICandleMessage candle, decimal priceMAValue)
        {
            // This method is mainly for chart visualization
            // The actual trading logic is in ProcessVolumeMAs
        }

        private void ProcessVolumeMAs(ICandleMessage candle, decimal fastVolumeMAValue, decimal slowVolumeMAValue, decimal priceMAValue)
        {
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Skip the first values to initialize previous values
            if (_isFirstValue)
            {
                _previousFastVolumeMA = fastVolumeMAValue;
                _previousSlowVolumeMA = slowVolumeMAValue;
                _isFirstValue = false;
                return;
            }
            
            // Check for crossovers
            var crossAbove = _previousFastVolumeMA <= _previousSlowVolumeMA && fastVolumeMAValue > slowVolumeMAValue;
            var crossBelow = _previousFastVolumeMA >= _previousSlowVolumeMA && fastVolumeMAValue < slowVolumeMAValue;
            
            // Log current values
            LogInfo($"Candle Close: {candle.ClosePrice}, Price MA: {priceMAValue}");
            LogInfo($"Fast Volume MA: {fastVolumeMAValue}, Slow Volume MA: {slowVolumeMAValue}");
            LogInfo($"Cross Above: {crossAbove}, Cross Below: {crossBelow}");

            // Trading logic:
            // Long: Fast volume MA crosses above slow volume MA
            if (crossAbove && Position <= 0)
            {
                LogInfo($"Buy Signal: Fast Volume MA crossing above Slow Volume MA");
                BuyMarket(Volume + Math.Abs(Position));
            }
            // Short: Fast volume MA crosses below slow volume MA
            else if (crossBelow && Position >= 0)
            {
                LogInfo($"Sell Signal: Fast Volume MA crossing below Slow Volume MA");
                SellMarket(Volume + Math.Abs(Position));
            }
            
            // Exit logic: Reverse crossover
            if (Position > 0 && crossBelow)
            {
                LogInfo($"Exit Long: Fast Volume MA crossing below Slow Volume MA");
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && crossAbove)
            {
                LogInfo($"Exit Short: Fast Volume MA crossing above Slow Volume MA");
                BuyMarket(Math.Abs(Position));
            }

            // Store current values for next comparison
            _previousFastVolumeMA = fastVolumeMAValue;
            _previousSlowVolumeMA = slowVolumeMAValue;
        }
    }
}