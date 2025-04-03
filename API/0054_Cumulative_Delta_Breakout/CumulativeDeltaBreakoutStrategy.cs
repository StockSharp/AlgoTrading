using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Cumulative Delta Breakout strategy
    /// Long entry: Cumulative Delta breaks above its N-period highest
    /// Short entry: Cumulative Delta breaks below its N-period lowest
    /// Exit: Cumulative Delta changes sign (crosses zero)
    /// </summary>
    public class CumulativeDeltaBreakoutStrategy : Strategy
    {
        private readonly StrategyParam<int> _lookbackPeriod;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _cumulativeDelta;
        private decimal _highestDelta;
        private decimal _lowestDelta;
        private int _barCount;

        /// <summary>
        /// Lookback Period for highest/lowest delta
        /// </summary>
        public int LookbackPeriod
        {
            get => _lookbackPeriod.Value;
            set => _lookbackPeriod.Value = value;
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
        /// Initialize <see cref="CumulativeDeltaBreakoutStrategy"/>.
        /// </summary>
        public CumulativeDeltaBreakoutStrategy()
        {
            _lookbackPeriod = Param(nameof(LookbackPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Lookback Period", "Period for calculating highest/lowest delta", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _cumulativeDelta = 0;
            _highestDelta = decimal.MinValue;
            _lowestDelta = decimal.MaxValue;
            _barCount = 0;
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

            // Create subscription for both candles and ticks
            var candleSubscription = SubscribeCandles(CandleType);
            var tickSubscription = new Subscription(DataType.Ticks, Security);
            
            // Bind candle processing
            candleSubscription
                .Bind(ProcessCandle)
                .Start();
                
            // Subscribe to ticks to compute delta
            this.WhenNewTrades(tickSubscription)
                .Do(trade => {
                    // Calculate delta: positive for buy trades, negative for sell trades
                    var delta = trade.Side == Sides.Buy ? trade.Volume : -trade.Volume;
                    
                    // Add to cumulative delta
                    _cumulativeDelta += delta;
                })
                .Apply(this);
                
            // Start the tick subscription
            Subscribe(tickSubscription);

            // Configure protection
            StartProtection(
                takeProfit: new Unit(3, UnitTypes.Percent),
                stopLoss: new Unit(2, UnitTypes.Percent)
            );

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, candleSubscription);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
                
            // Increment bar counter
            _barCount++;
            
            // Update highest and lowest values if within lookback period
            if (_barCount <= LookbackPeriod)
            {
                _highestDelta = Math.Max(_highestDelta, _cumulativeDelta);
                _lowestDelta = Math.Min(_lowestDelta, _cumulativeDelta);
                
                // Need at least lookback period bars before trading
                if (_barCount < LookbackPeriod)
                    return;
            }
            else
            {
                // After lookback period, use rolling window for highest/lowest
                // This is a simple approximation - in real implementation you might 
                // want to use a more sophisticated rolling window calculation
                _highestDelta = Math.Max(_highestDelta * 0.95m, _cumulativeDelta); // Decay old values
                _lowestDelta = Math.Min(_lowestDelta * 1.05m, _cumulativeDelta);   // Decay old values
            }
            
            // Log current values
            this.AddInfoLog($"Candle Close: {candle.ClosePrice}, Cumulative Delta: {_cumulativeDelta}");
            this.AddInfoLog($"Highest Delta: {_highestDelta}, Lowest Delta: {_lowestDelta}");

            // Trading logic:
            // Long: Cumulative Delta breaks above highest
            if (_cumulativeDelta > _highestDelta && Position <= 0)
            {
                this.AddInfoLog($"Buy Signal: Cumulative Delta ({_cumulativeDelta}) breaking above highest ({_highestDelta})");
                BuyMarket(Volume + Math.Abs(Position));
                
                // Update highest after breakout
                _highestDelta = _cumulativeDelta;
            }
            // Short: Cumulative Delta breaks below lowest
            else if (_cumulativeDelta < _lowestDelta && Position >= 0)
            {
                this.AddInfoLog($"Sell Signal: Cumulative Delta ({_cumulativeDelta}) breaking below lowest ({_lowestDelta})");
                SellMarket(Volume + Math.Abs(Position));
                
                // Update lowest after breakout
                _lowestDelta = _cumulativeDelta;
            }
            
            // Exit logic: Cumulative Delta crosses zero
            if (Position > 0 && _cumulativeDelta < 0)
            {
                this.AddInfoLog($"Exit Long: Cumulative Delta ({_cumulativeDelta}) < 0");
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && _cumulativeDelta > 0)
            {
                this.AddInfoLog($"Exit Short: Cumulative Delta ({_cumulativeDelta}) > 0");
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}