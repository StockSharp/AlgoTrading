using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Williams %R Hook Reversal Strategy.
    /// Enters long when Williams %R forms an upward hook from oversold conditions.
    /// Enters short when Williams %R forms a downward hook from overbought conditions.
    /// </summary>
    public class WilliamsRHookReversalStrategy : Strategy
    {
        private readonly StrategyParam<int> _willRPeriod;
        private readonly StrategyParam<decimal> _oversoldLevel;
        private readonly StrategyParam<decimal> _overboughtLevel;
        private readonly StrategyParam<decimal> _exitLevel;
        private readonly StrategyParam<Unit> _stopLoss;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevWillR;

        /// <summary>
        /// Period for Williams %R calculation.
        /// </summary>
        public int WillRPeriod
        {
            get => _willRPeriod.Value;
            set => _willRPeriod.Value = value;
        }

        /// <summary>
        /// Oversold level for Williams %R (typically -80).
        /// </summary>
        public decimal OversoldLevel
        {
            get => _oversoldLevel.Value;
            set => _oversoldLevel.Value = value;
        }

        /// <summary>
        /// Overbought level for Williams %R (typically -20).
        /// </summary>
        public decimal OverboughtLevel
        {
            get => _overboughtLevel.Value;
            set => _overboughtLevel.Value = value;
        }

        /// <summary>
        /// Exit level for Williams %R (neutral zone).
        /// </summary>
        public decimal ExitLevel
        {
            get => _exitLevel.Value;
            set => _exitLevel.Value = value;
        }

        /// <summary>
        /// Stop loss percentage from entry price.
        /// </summary>
        public Unit StopLoss
        {
            get => _stopLoss.Value;
            set => _stopLoss.Value = value;
        }

        /// <summary>
        /// Type of candles to use.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilliamsRHookReversalStrategy"/>.
        /// </summary>
        public WilliamsRHookReversalStrategy()
        {
            _willRPeriod = Param(nameof(WillRPeriod), 14)
                .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Williams %R Settings")
                .SetRange(7, 21, 7)
                .SetCanOptimize(true);
                
            _oversoldLevel = Param(nameof(OversoldLevel), -80m)
                .SetDisplay("Oversold Level", "Oversold level for Williams %R (typically -80)", "Williams %R Settings")
                .SetRange(-90m, -70m, 5m)
                .SetCanOptimize(true);
                
            _overboughtLevel = Param(nameof(OverboughtLevel), -20m)
                .SetDisplay("Overbought Level", "Overbought level for Williams %R (typically -20)", "Williams %R Settings")
                .SetRange(-30m, -10m, 5m)
                .SetCanOptimize(true);
                
            _exitLevel = Param(nameof(ExitLevel), -50m)
                .SetDisplay("Exit Level", "Exit level for Williams %R (neutral zone)", "Williams %R Settings")
                .SetRange(-60m, -40m, 5m)
                .SetCanOptimize(true);
                
            _stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
                .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
                .SetRange(1m, 3m, 0.5m)
                .SetCanOptimize(true);
                
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");
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

            // Enable position protection using stop-loss
            StartProtection(
                takeProfit: null,
                stopLoss: StopLoss,
                isStopTrailing: false,
                useMarketOrders: true
            );

            // Initialize previous Williams %R value
            _prevWillR = 0;
            
            // Create Williams %R indicator
            var williamsR = new WilliamsR { Length = WillRPeriod };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicator and process candles
            subscription
                .Bind(williamsR, ProcessCandle)
                .Start();
                
            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, williamsR);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process candle with Williams %R value.
        /// </summary>
        /// <param name="candle">Candle.</param>
        /// <param name="willRValue">Williams %R value.</param>
        private void ProcessCandle(ICandleMessage candle, decimal willRValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // If this is the first calculation, just store the value
            if (_prevWillR == 0)
            {
                _prevWillR = willRValue;
                return;
            }

            // Check for Williams %R hooks
            bool oversoldHookUp = _prevWillR < OversoldLevel && willRValue > _prevWillR;
            bool overboughtHookDown = _prevWillR > OverboughtLevel && willRValue < _prevWillR;
            
            // Long entry: Williams %R forms an upward hook from oversold
            if (oversoldHookUp && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: Williams %R upward hook from oversold ({_prevWillR} -> {willRValue})");
            }
            // Short entry: Williams %R forms a downward hook from overbought
            else if (overboughtHookDown && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: Williams %R downward hook from overbought ({_prevWillR} -> {willRValue})");
            }
            
            // Exit conditions based on Williams %R reaching neutral zone
            if (willRValue > ExitLevel && Position < 0)
            {
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Williams %R reached neutral zone ({willRValue} > {ExitLevel})");
            }
            else if (willRValue < ExitLevel && Position > 0)
            {
                SellMarket(Position);
                LogInfo($"Exit long: Williams %R reached neutral zone ({willRValue} < {ExitLevel})");
            }
            
            // Update previous Williams %R value
            _prevWillR = willRValue;
        }
    }
}