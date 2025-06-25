using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Hull Moving Average + Stochastic Oscillator strategy.
    /// Strategy enters when HMA trend direction changes with Stochastic confirming oversold/overbought conditions.
    /// </summary>
    public class HullMaStochasticStrategy : Strategy
    {
        private readonly StrategyParam<int> _hmaPeriod;
        private readonly StrategyParam<int> _stochPeriod;
        private readonly StrategyParam<int> _stochK;
        private readonly StrategyParam<int> _stochD;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossAtr;

        // Indicators
        private HullMovingAverage _hma;
        private StochasticOscillator _stochastic;
        private AverageTrueRange _atr;

        // Previous HMA value for trend detection
        private decimal _prevHmaValue;

        /// <summary>
        /// Hull Moving Average period.
        /// </summary>
        public int HmaPeriod
        {
            get => _hmaPeriod.Value;
            set => _hmaPeriod.Value = value;
        }

        /// <summary>
        /// Stochastic period.
        /// </summary>
        public int StochPeriod
        {
            get => _stochPeriod.Value;
            set => _stochPeriod.Value = value;
        }

        /// <summary>
        /// Stochastic %K period.
        /// </summary>
        public int StochK
        {
            get => _stochK.Value;
            set => _stochK.Value = value;
        }

        /// <summary>
        /// Stochastic %D period.
        /// </summary>
        public int StochD
        {
            get => _stochD.Value;
            set => _stochD.Value = value;
        }

        /// <summary>
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss in ATR multiples.
        /// </summary>
        public decimal StopLossAtr
        {
            get => _stopLossAtr.Value;
            set => _stopLossAtr.Value = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public HullMaStochasticStrategy()
        {
            _hmaPeriod = Param(nameof(HmaPeriod), 9)
                .SetGreaterThanZero()
                .SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(4, 30, 2);

            _stochPeriod = Param(nameof(StochPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("Stochastic Period", "Stochastic oscillator period", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(5, 30, 5);

            _stochK = Param(nameof(StochK), 3)
                .SetGreaterThanZero()
                .SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1, 10, 1);

            _stochD = Param(nameof(StochD), 3)
                .SetGreaterThanZero()
                .SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1, 10, 1);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossAtr = Param(nameof(StopLossAtr), 2m)
                .SetGreaterThanZero()
                .SetDisplay("Stop Loss ATR", "Stop loss in ATR multiples", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1m, 4m, 0.5m);
        }

        /// <inheritdoc />
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            return [(Security, CandleType)];
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Initialize the previous HMA value
            _prevHmaValue = 0;

            // Create indicators
            _hma = new HullMovingAverage { Length = HmaPeriod };

            _stochastic = new StochasticOscillator
            {
                K = StochK,
                D = StochD,
                KPeriod = StochPeriod
            };

            _atr = new AverageTrueRange { Length = 14 };

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .BindEx(_hma, _stochastic, _atr, ProcessCandle)
                .Start();

            // Setup chart
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _hma);
                
                var secondArea = CreateChartArea();
                if (secondArea != null)
                {
                    DrawIndicator(secondArea, _stochastic);
                }
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(
            ICandleMessage candle, 
            IIndicatorValue hmaValue, 
            IIndicatorValue stochasticValue, 
            IIndicatorValue atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Get indicator values
            decimal hma = hmaValue.GetValue<decimal>();
            decimal stochK = ((StochasticOscillatorValue)stochasticValue).K;
            decimal atr = atrValue.GetValue<decimal>();

            // Skip first candle after initialization
            if (_prevHmaValue == 0)
            {
                _prevHmaValue = hma;
                return;
            }

            // Detect HMA trend direction
            bool hmaIncreasing = hma > _prevHmaValue;
            bool hmaDecreasing = hma < _prevHmaValue;

            // Calculate stop loss based on ATR
            decimal stopLoss = StopLossAtr * atr;

            // Trading logic:
            // Buy when HMA starts increasing (trend changes up) and Stochastic shows oversold condition
            if (hmaIncreasing && !hmaDecreasing && stochK < 20 && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}, Stochastic %K={stochK}");
            }
            // Sell when HMA starts decreasing (trend changes down) and Stochastic shows overbought condition
            else if (hmaDecreasing && !hmaIncreasing && stochK > 80 && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}, Stochastic %K={stochK}");
            }
            // Exit when HMA trend changes direction
            else if (Position > 0 && hmaDecreasing)
            {
                SellMarket(Math.Abs(Position));
                LogInfo($"Long exit: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}");
            }
            else if (Position < 0 && hmaIncreasing)
            {
                BuyMarket(Math.Abs(Position));
                LogInfo($"Short exit: Price={candle.ClosePrice}, HMA={hma}, Prev HMA={_prevHmaValue}");
            }

            // Save current HMA value for next candle
            _prevHmaValue = hma;

            // Set stop loss based on ATR (if position is open)
            if (Position != 0)
            {
                var stopLossUnit = new Unit(stopLoss, UnitTypes.Absolute);
                StartProtection(new Unit(0, UnitTypes.Absolute), stopLossUnit);
            }
        }
    }
}