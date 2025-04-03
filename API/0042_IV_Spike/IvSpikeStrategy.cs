using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// IV Spike strategy based on implied volatility spikes
    /// This strategy enters long when IV increases by 50% and price is below MA,
    /// or short when IV increases by 50% and price is above MA
    /// </summary>
    public class IVSpikeStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _ivPeriod;
        private readonly StrategyParam<decimal> _ivSpikeThreshold;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _previousIV;

        /// <summary>
        /// MA Period
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// IV Period (for historical volatility calculation)
        /// </summary>
        public int IVPeriod
        {
            get => _ivPeriod.Value;
            set => _ivPeriod.Value = value;
        }

        /// <summary>
        /// IV Spike Threshold (minimum IV increase for signal generation)
        /// </summary>
        public decimal IVSpikeThreshold
        {
            get => _ivSpikeThreshold.Value;
            set => _ivSpikeThreshold.Value = value;
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
        /// Initialize <see cref="IVSpikeStrategy"/>.
        /// </summary>
        public IVSpikeStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _ivPeriod = Param(nameof(IVPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("IV Period", "Period for Implied Volatility calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _ivSpikeThreshold = Param(nameof(IVSpikeThreshold), 1.5m)
                .SetGreaterThan(1.0m)
                .SetDisplay("IV Spike Threshold", "Minimum IV increase multiplier (e.g., 1.5 = 50% increase)", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(1.2m, 2.0m, 0.1m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _previousIV = 0;
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
            var hv = new StandardDeviation { Length = IVPeriod }; // Using standard deviation as proxy for IV

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ma, hv, ProcessCandle)
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
                DrawIndicator(area, hv);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal ivValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Initialize previous IV on first candle
            if (_previousIV == 0 && ivValue > 0)
            {
                _previousIV = ivValue;
                return;
            }

            // Calculate IV change
            var ivChange = ivValue / _previousIV;
            
            // Log current values
            this.AddInfoLog($"Candle Close: {candle.ClosePrice}, MA: {maValue}, IV: {ivValue}, IV Change: {ivChange:P2}");

            // Trading logic:
            // Check for IV spike
            if (ivChange >= IVSpikeThreshold)
            {
                this.AddInfoLog($"IV Spike detected: {ivChange:P2}");

                // Long: IV spike and price below MA
                if (candle.ClosePrice < maValue && Position <= 0)
                {
                    this.AddInfoLog($"Buy Signal: IV Spike ({ivChange:P2}) and Price ({candle.ClosePrice}) < MA ({maValue})");
                    BuyMarket(Volume + Math.Abs(Position));
                }
                // Short: IV spike and price above MA
                else if (candle.ClosePrice > maValue && Position >= 0)
                {
                    this.AddInfoLog($"Sell Signal: IV Spike ({ivChange:P2}) and Price ({candle.ClosePrice}) > MA ({maValue})");
                    SellMarket(Volume + Math.Abs(Position));
                }
            }
            
            // Exit logic: IV declining (IV now < previous IV)
            if (ivValue < _previousIV)
            {
                if (Position > 0)
                {
                    this.AddInfoLog($"Exit Long: IV declining ({ivValue} < {_previousIV})");
                    SellMarket(Math.Abs(Position));
                }
                else if (Position < 0)
                {
                    this.AddInfoLog($"Exit Short: IV declining ({ivValue} < {_previousIV})");
                    BuyMarket(Math.Abs(Position));
                }
            }

            // Store current IV for next comparison
            _previousIV = ivValue;
        }
    }
}