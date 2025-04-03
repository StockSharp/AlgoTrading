using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Volume Spike strategy
    /// Long entry: Volume increases 2x above previous candle and price is above MA
    /// Short entry: Volume increases 2x above previous candle and price is below MA
    /// Exit when volume falls below average volume
    /// </summary>
    public class VolumeSpikeStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _volAvgPeriod;
        private readonly StrategyParam<decimal> _volumeSpikeMultiplier;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _previousVolume;

        /// <summary>
        /// MA Period
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Volume Average Period
        /// </summary>
        public int VolAvgPeriod
        {
            get => _volAvgPeriod.Value;
            set => _volAvgPeriod.Value = value;
        }

        /// <summary>
        /// Volume Spike Multiplier
        /// </summary>
        public decimal VolumeSpikeMultiplier
        {
            get => _volumeSpikeMultiplier.Value;
            set => _volumeSpikeMultiplier.Value = value;
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
        /// Initialize <see cref="VolumeSpikeStrategy"/>.
        /// </summary>
        public VolumeSpikeStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _volAvgPeriod = Param(nameof(VolAvgPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Volume Average Period", "Period for Average Volume calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _volumeSpikeMultiplier = Param(nameof(VolumeSpikeMultiplier), 2.0m)
                .SetGreaterThan(1.0m)
                .SetDisplay("Volume Spike Multiplier", "Minimum volume increase multiplier to generate signal", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _previousVolume = 0;
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
            var ma = new SimpleMovingAverage { Length = MAPeriod };
            var volumeMA = new SimpleMovingAverage { Length = VolAvgPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ma, volumeMA, ProcessCandle)
                .Start();

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
                DrawIndicator(area, ma);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal volumeMAValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Skip first candle, just store volume
            if (_previousVolume == 0)
            {
                _previousVolume = candle.TotalVolume;
                return;
            }

            // Calculate volume change
            var volumeChange = candle.TotalVolume / _previousVolume;
            
            // Log current values
            this.AddInfoLog($"Candle Close: {candle.ClosePrice}, MA: {maValue}, Volume: {candle.TotalVolume}");
            this.AddInfoLog($"Previous Volume: {_previousVolume}, Volume Change: {volumeChange:P2}, Average Volume: {volumeMAValue}");

            // Trading logic:
            // Check for volume spike
            if (volumeChange >= VolumeSpikeMultiplier)
            {
                this.AddInfoLog($"Volume Spike detected: {volumeChange:P2}");

                // Long: Volume spike and price above MA
                if (candle.ClosePrice > maValue && Position <= 0)
                {
                    this.AddInfoLog($"Buy Signal: Volume Spike ({volumeChange:P2}) and Price ({candle.ClosePrice}) > MA ({maValue})");
                    BuyMarket(Volume + Math.Abs(Position));
                }
                // Short: Volume spike and price below MA
                else if (candle.ClosePrice < maValue && Position >= 0)
                {
                    this.AddInfoLog($"Sell Signal: Volume Spike ({volumeChange:P2}) and Price ({candle.ClosePrice}) < MA ({maValue})");
                    SellMarket(Volume + Math.Abs(Position));
                }
            }
            
            // Exit logic: Volume falls below average
            if (candle.TotalVolume < volumeMAValue)
            {
                if (Position > 0)
                {
                    this.AddInfoLog($"Exit Long: Volume ({candle.TotalVolume}) < Average Volume ({volumeMAValue})");
                    SellMarket(Math.Abs(Position));
                }
                else if (Position < 0)
                {
                    this.AddInfoLog($"Exit Short: Volume ({candle.TotalVolume}) < Average Volume ({volumeMAValue})");
                    BuyMarket(Math.Abs(Position));
                }
            }

            // Store current volume for next comparison
            _previousVolume = candle.TotalVolume;
        }
    }
}