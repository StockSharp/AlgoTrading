using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Stochastic Overbought/Oversold strategy that buys when Stochastic is oversold 
    /// and sells when Stochastic is overbought.
    /// </summary>
    public class StochasticOverboughtOversoldStrategy : Strategy
    {
        private readonly StrategyParam<int> _stochPeriod;
        private readonly StrategyParam<int> _kPeriod;
        private readonly StrategyParam<int> _dPeriod;
        private readonly StrategyParam<int> _overboughtLevel;
        private readonly StrategyParam<int> _oversoldLevel;
        private readonly StrategyParam<int> _neutralLevel;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;

        /// <summary>
        /// Stochastic %K period.
        /// </summary>
        public int StochPeriod
        {
            get => _stochPeriod.Value;
            set => _stochPeriod.Value = value;
        }

        /// <summary>
        /// %K smoothing period.
        /// </summary>
        public int KPeriod
        {
            get => _kPeriod.Value;
            set => _kPeriod.Value = value;
        }

        /// <summary>
        /// %D period (signal line).
        /// </summary>
        public int DPeriod
        {
            get => _dPeriod.Value;
            set => _dPeriod.Value = value;
        }

        /// <summary>
        /// Stochastic level considered overbought.
        /// </summary>
        public int OverboughtLevel
        {
            get => _overboughtLevel.Value;
            set => _overboughtLevel.Value = value;
        }

        /// <summary>
        /// Stochastic level considered oversold.
        /// </summary>
        public int OversoldLevel
        {
            get => _oversoldLevel.Value;
            set => _oversoldLevel.Value = value;
        }

        /// <summary>
        /// Stochastic neutral level for exiting positions.
        /// </summary>
        public int NeutralLevel
        {
            get => _neutralLevel.Value;
            set => _neutralLevel.Value = value;
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
        /// Stop-loss percentage from entry price.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StochasticOverboughtOversoldStrategy"/>.
        /// </summary>
        public StochasticOverboughtOversoldStrategy()
        {
            _stochPeriod = Param(nameof(StochPeriod), 14)
                .SetRange(5, 30)
                .SetDisplay("Stochastic Period", "Number of bars used in %K calculation", "Indicator Parameters")
                .SetCanOptimize(true);

            _kPeriod = Param(nameof(KPeriod), 3)
                .SetRange(1, 10)
                .SetDisplay("K Period", "Smoothing period for %K", "Indicator Parameters")
                .SetCanOptimize(true);

            _dPeriod = Param(nameof(DPeriod), 3)
                .SetRange(1, 10)
                .SetDisplay("D Period", "Smoothing period for %D (signal line)", "Indicator Parameters")
                .SetCanOptimize(true);

            _overboughtLevel = Param(nameof(OverboughtLevel), 80)
                .SetRange(70, 90)
                .SetDisplay("Overbought Level", "Stochastic level considered overbought", "Signal Parameters")
                .SetCanOptimize(true);

            _oversoldLevel = Param(nameof(OversoldLevel), 20)
                .SetRange(10, 30)
                .SetDisplay("Oversold Level", "Stochastic level considered oversold", "Signal Parameters")
                .SetCanOptimize(true);

            _neutralLevel = Param(nameof(NeutralLevel), 50)
                .SetRange(45, 55)
                .SetDisplay("Neutral Level", "Stochastic level for exiting positions", "Signal Parameters")
                .SetCanOptimize(true);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetRange(0.5m, 5.0m)
                .SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")
                .SetCanOptimize(true);
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

            // Create Stochastic Oscillator indicator
            var stochastic = new StochasticOscillator
            {
                KPeriod = StochPeriod,
                K = KPeriod,
                D = DPeriod
            };

            // Create candle subscription
            var subscription = SubscribeCandles(CandleType);

            // Bind Stochastic indicator to candles
            subscription
                .Bind(stochastic, ProcessCandle)
                .Start();

            // Enable position protection
            StartProtection(
                new Unit(0, UnitTypes.Absolute), // No take profit (will exit at neutral level)
                new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss at defined percentage
                false // No trailing stop
            );

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, stochastic);
                DrawOwnTrades(area);
            }
        }
        
        private void ProcessCandle(ICandleMessage candle, decimal stochasticK, decimal stochasticD)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            LogInfo($"Stochastic values: %K={stochasticK:F2}, %D={stochasticD:F2}, Position={Position}");

            // Trading logic based on %K (fast line) crossing extreme levels
            if (stochasticK <= OversoldLevel && Position <= 0)
            {
                // Stochastic indicates oversold condition - Buy signal
                if (Position < 0)
                {
                    // Close any existing short position
                    BuyMarket(Math.Abs(Position));
                    LogInfo($"Closed short position at Stochastic %K={stochasticK:F2}");
                }

                // Open new long position
                BuyMarket(Volume);
                LogInfo($"Buy signal: Stochastic %K={stochasticK:F2} is below oversold level {OversoldLevel}");
            }
            else if (stochasticK >= OverboughtLevel && Position >= 0)
            {
                // Stochastic indicates overbought condition - Sell signal
                if (Position > 0)
                {
                    // Close any existing long position
                    SellMarket(Position);
                    LogInfo($"Closed long position at Stochastic %K={stochasticK:F2}");
                }

                // Open new short position
                SellMarket(Volume);
                LogInfo($"Sell signal: Stochastic %K={stochasticK:F2} is above overbought level {OverboughtLevel}");
            }
            else if (Position > 0 && stochasticK >= NeutralLevel)
            {
                // Exit long position when Stochastic returns to neutral
                SellMarket(Position);
                LogInfo($"Exit long: Stochastic %K={stochasticK:F2} returned to neutral level {NeutralLevel}");
            }
            else if (Position < 0 && stochasticK <= NeutralLevel)
            {
                // Exit short position when Stochastic returns to neutral
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Stochastic %K={stochasticK:F2} returned to neutral level {NeutralLevel}");
            }
        }