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
    /// Strategy based on Parabolic SAR and Stochastic Oscillator.
    /// Enters long when price is above SAR and Stochastic %K < 20 (oversold).
    /// Enters short when price is below SAR and Stochastic %K > 80 (overbought).
    /// Exits when price crosses SAR in the opposite direction.
    /// </summary>
    public class ParabolicSarStochasticStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _sarAcceleration;
        private readonly StrategyParam<decimal> _sarMaxAcceleration;
        private readonly StrategyParam<int> _stochPeriod;
        private readonly StrategyParam<int> _stochKPeriod;
        private readonly StrategyParam<int> _stochDPeriod;
        private readonly StrategyParam<DataType> _candleType;

        private ParabolicSar _parabolicSar;
        private Stochastic _stochastic;

        /// <summary>
        /// Parabolic SAR acceleration factor.
        /// </summary>
        public decimal SarAcceleration
        {
            get => _sarAcceleration.Value;
            set => _sarAcceleration.Value = value;
        }

        /// <summary>
        /// Parabolic SAR maximum acceleration factor.
        /// </summary>
        public decimal SarMaxAcceleration
        {
            get => _sarMaxAcceleration.Value;
            set => _sarMaxAcceleration.Value = value;
        }

        /// <summary>
        /// Stochastic Oscillator period.
        /// </summary>
        public int StochPeriod
        {
            get => _stochPeriod.Value;
            set => _stochPeriod.Value = value;
        }

        /// <summary>
        /// Stochastic %K period.
        /// </summary>
        public int StochKPeriod
        {
            get => _stochKPeriod.Value;
            set => _stochKPeriod.Value = value;
        }

        /// <summary>
        /// Stochastic %D period.
        /// </summary>
        public int StochDPeriod
        {
            get => _stochDPeriod.Value;
            set => _stochDPeriod.Value = value;
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
        /// Initializes a new instance of the <see cref="ParabolicSarStochasticStrategy"/>.
        /// </summary>
        public ParabolicSarStochasticStrategy()
        {
            _sarAcceleration = Param(nameof(SarAcceleration), 0.02m)
                .SetDisplayName("SAR Acceleration")
                .SetDescription("Acceleration factor for Parabolic SAR")
                .SetCategories("Indicators")
                .SetCanOptimize(true)
                .SetOptimize(0.01m, 0.05m, 0.01m);

            _sarMaxAcceleration = Param(nameof(SarMaxAcceleration), 0.2m)
                .SetDisplayName("SAR Max Acceleration")
                .SetDescription("Maximum acceleration factor for Parabolic SAR")
                .SetCategories("Indicators")
                .SetCanOptimize(true)
                .SetOptimize(0.1m, 0.3m, 0.05m);

            _stochPeriod = Param(nameof(StochPeriod), 14)
                .SetDisplayName("Stochastic Period")
                .SetDescription("Period for Stochastic Oscillator calculation")
                .SetCategories("Indicators")
                .SetCanOptimize(true);

            _stochKPeriod = Param(nameof(StochKPeriod), 3)
                .SetDisplayName("Stochastic %K Period")
                .SetDescription("Period for %K line calculation")
                .SetCategories("Indicators");

            _stochDPeriod = Param(nameof(StochDPeriod), 3)
                .SetDisplayName("Stochastic %D Period")
                .SetDescription("Period for %D line calculation")
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

            // Create indicators
            _parabolicSar = new ParabolicSar
            {
                Acceleration = SarAcceleration,
                MaxAcceleration = SarMaxAcceleration
            };

            _stochastic = new Stochastic
            {
                K = StochKPeriod,
                D = StochDPeriod,
                Length = StochPeriod
            };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Process candles with indicators
            subscription
                .Bind(_parabolicSar, _stochastic, ProcessCandle)
                .Start();

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _parabolicSar);
                DrawOwnTrades(area);

                // Stochastic in separate area
                var stochArea = CreateChartArea();
                if (stochArea != null)
                {
                    DrawIndicator(stochArea, _stochastic);
                }
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal sar, decimal k, decimal d)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready for trading
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Trading logic
            if (candle.ClosePrice > sar && k < 20 && Position <= 0)
            {
                // Price above SAR with stochastic oversold - go long
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (candle.ClosePrice < sar && k > 80 && Position >= 0)
            {
                // Price below SAR with stochastic overbought - go short
                SellMarket(Volume + Math.Abs(Position));
            }
            
            // Exit logic based on SAR crossing
            if (Position > 0 && candle.ClosePrice < sar)
            {
                // Exit long position when price crosses below SAR
                ClosePosition();
            }
            else if (Position < 0 && candle.ClosePrice > sar)
            {
                // Exit short position when price crosses above SAR
                ClosePosition();
            }

            // Use SAR as trailing stop
            if (Position != 0)
            {
                StartProtection(
                    new Unit(0), // No take profit - use SAR for exit
                    new Unit(sar, UnitTypes.Absolute)
                );
            }
        }
    }
}