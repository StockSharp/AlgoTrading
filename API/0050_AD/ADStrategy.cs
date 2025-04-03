using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Accumulation/Distribution (A/D) Strategy
    /// Long entry: A/D rising and price above MA
    /// Short entry: A/D falling and price below MA
    /// Exit: A/D changes direction
    /// </summary>
    public class ADStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _previousADValue;
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
        /// Candle type for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize <see cref="ADStrategy"/>.
        /// </summary>
        public ADStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _previousADValue = 0;
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
            var ad = new AccumulationDistributionLine();

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            // We need to bind both indicators but handle with one callback
            subscription
                .Bind(ma, ad, ProcessCandle)
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
                DrawIndicator(area, ad);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal adValue)
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
                _previousADValue = adValue;
                _isFirstCandle = false;
                return;
            }
            
            // Check for A/D direction
            var adRising = adValue > _previousADValue;
            var adFalling = adValue < _previousADValue;
            
            // Log current values
            this.AddInfoLog($"Candle Close: {candle.ClosePrice}, MA: {maValue}, A/D: {adValue}");
            this.AddInfoLog($"Previous A/D: {_previousADValue}, A/D Rising: {adRising}, A/D Falling: {adFalling}");

            // Trading logic:
            // Long: A/D rising and price above MA
            if (adRising && candle.ClosePrice > maValue && Position <= 0)
            {
                this.AddInfoLog($"Buy Signal: A/D rising and Price ({candle.ClosePrice}) > MA ({maValue})");
                BuyMarket(Volume + Math.Abs(Position));
            }
            // Short: A/D falling and price below MA
            else if (adFalling && candle.ClosePrice < maValue && Position >= 0)
            {
                this.AddInfoLog($"Sell Signal: A/D falling and Price ({candle.ClosePrice}) < MA ({maValue})");
                SellMarket(Volume + Math.Abs(Position));
            }
            
            // Exit logic: A/D changes direction
            if (Position > 0 && adFalling)
            {
                this.AddInfoLog($"Exit Long: A/D changing direction (falling)");
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && adRising)
            {
                this.AddInfoLog($"Exit Short: A/D changing direction (rising)");
                BuyMarket(Math.Abs(Position));
            }

            // Store current A/D value for next comparison
            _previousADValue = adValue;
        }
    }
}