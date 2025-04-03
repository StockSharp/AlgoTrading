using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Volume Divergence strategy
    /// Long entry: Price falls but volume increases (possible accumulation)
    /// Short entry: Price rises but volume increases (possible distribution)
    /// Exit: Price crosses MA
    /// </summary>
    public class VolumeDivergenceStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _previousClose;
        private decimal _previousVolume;
        private bool _isFirstCandle;

        /// <summary>
        /// MA Period
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// ATR Period
        /// </summary>
        public int ATRPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
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
        /// Initialize <see cref="VolumeDivergenceStrategy"/>.
        /// </summary>
        public VolumeDivergenceStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _atrPeriod = Param(nameof(ATRPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "Period for Average True Range calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(7, 28, 7);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _previousClose = 0;
            _previousVolume = 0;
            _isFirstCandle = true;
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
            var atr = new AverageTrueRange { Length = ATRPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ma, atr, ProcessCandle)
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
                DrawIndicator(area, atr);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Skip the first candle, just initialize values
            if (_isFirstCandle)
            {
                _previousClose = candle.ClosePrice;
                _previousVolume = candle.TotalVolume;
                _isFirstCandle = false;
                return;
            }
            
            // Calculate price change and volume change
            var priceDown = candle.ClosePrice < _previousClose;
            var priceUp = candle.ClosePrice > _previousClose;
            var volumeUp = candle.TotalVolume > _previousVolume;
            
            // Identify divergences
            var bullishDivergence = priceDown && volumeUp;  // Price down but volume up (accumulation)
            var bearishDivergence = priceUp && volumeUp;    // Price up but volume up too much (distribution)
            
            // Log current values
            this.AddInfoLog($"Candle Close: {candle.ClosePrice}, Previous Close: {_previousClose}, MA: {maValue}");
            this.AddInfoLog($"Volume: {candle.TotalVolume}, Previous Volume: {_previousVolume}");
            this.AddInfoLog($"Bullish Divergence: {bullishDivergence}, Bearish Divergence: {bearishDivergence}");

            // Trading logic:
            // Long: Price down but volume up (accumulation)
            if (bullishDivergence && Position <= 0)
            {
                this.AddInfoLog($"Buy Signal: Price down but volume up (possible accumulation)");
                BuyMarket(Volume + Math.Abs(Position));
            }
            // Short: Price up but volume up too much (distribution)
            else if (bearishDivergence && Position >= 0)
            {
                this.AddInfoLog($"Sell Signal: Price up but volume up too much (possible distribution)");
                SellMarket(Volume + Math.Abs(Position));
            }
            
            // Exit logic: Price crosses MA
            if (Position > 0 && candle.ClosePrice < maValue)
            {
                this.AddInfoLog($"Exit Long: Price ({candle.ClosePrice}) < MA ({maValue})");
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > maValue)
            {
                this.AddInfoLog($"Exit Short: Price ({candle.ClosePrice}) > MA ({maValue})");
                BuyMarket(Math.Abs(Position));
            }

            // Store current values for next comparison
            _previousClose = candle.ClosePrice;
            _previousVolume = candle.TotalVolume;
        }
    }
}