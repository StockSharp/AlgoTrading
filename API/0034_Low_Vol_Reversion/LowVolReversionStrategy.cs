using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy that trades on mean reversion during periods of low volatility.
    /// It identifies periods of low ATR (Average True Range) and opens positions when price
    /// deviates from its moving average, expecting a return to the mean.
    /// </summary>
    public class LowVolReversionStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<int> _atrLookbackPeriod;
        private readonly StrategyParam<decimal> _atrThresholdPercent;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;

        private decimal _avgAtr;
        private int _lookbackCounter;

        /// <summary>
        /// Period for Moving Average calculation (default: 20)
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Period for ATR calculation (default: 14)
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// Lookback period for ATR average calculation (default: 20)
        /// </summary>
        public int AtrLookbackPeriod
        {
            get => _atrLookbackPeriod.Value;
            set => _atrLookbackPeriod.Value = value;
        }

        /// <summary>
        /// ATR threshold as percentage of average ATR (default: 50%)
        /// </summary>
        public decimal AtrThresholdPercent
        {
            get => _atrThresholdPercent.Value;
            set => _atrThresholdPercent.Value = value;
        }

        /// <summary>
        /// ATR multiplier for stop-loss calculation (default: 2.0)
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Type of candles used for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize the Low Volatility Reversion strategy
        /// </summary>
        public LowVolReversionStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetDisplayName("MA Period")
                .SetDescription("Period for Moving Average calculation")
                .SetGroup("Technical Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetDisplayName("ATR Period")
                .SetDescription("Period for ATR calculation")
                .SetGroup("Technical Parameters")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _atrLookbackPeriod = Param(nameof(AtrLookbackPeriod), 20)
                .SetDisplayName("ATR Lookback Period")
                .SetDescription("Lookback period for ATR average calculation")
                .SetGroup("Technical Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _atrThresholdPercent = Param(nameof(AtrThresholdPercent), 50m)
                .SetDisplayName("ATR Threshold %")
                .SetDescription("ATR threshold as percentage of average ATR")
                .SetGroup("Entry Parameters")
                .SetCanOptimize(true)
                .SetOptimize(30m, 70m, 10m);

            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                .SetDisplayName("ATR Multiplier")
                .SetDescription("ATR multiplier for stop-loss calculation")
                .SetGroup("Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplayName("Candle Type")
                .SetDescription("Type of candles to use")
                .SetGroup("Data");
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

            // Reset state variables
            _avgAtr = 0;
            _lookbackCounter = 0;

            // Create indicators
            var sma = new SimpleMovingAverage { Length = MAPeriod };
            var atr = new AverageTrueRange { Length = AtrPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(sma, atr, ProcessCandle)
                .Start();

            // Configure chart
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, sma);
                DrawIndicator(area, atr);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process candle and check for low volatility mean reversion signals
        /// </summary>
        private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Gather ATR values for average calculation
            if (_lookbackCounter < AtrLookbackPeriod)
            {
                // Still collecting ATR values for the average
                if (_lookbackCounter == 0)
                {
                    _avgAtr = atrValue;
                }
                else
                {
                    // Calculate running average
                    _avgAtr = (_avgAtr * _lookbackCounter + atrValue) / (_lookbackCounter + 1);
                }
                
                _lookbackCounter++;
                return;
            }
            else
            {
                // Update running average
                _avgAtr = (_avgAtr * (AtrLookbackPeriod - 1) + atrValue) / AtrLookbackPeriod;
            }

            // Calculate ATR threshold
            decimal atrThreshold = _avgAtr * (AtrThresholdPercent / 100);
            
            // Check if we're in a low volatility period
            bool isLowVolatility = atrValue < atrThreshold;
            
            if (!isLowVolatility)
            {
                // Not a low volatility period, skip trading
                return;
            }

            // Calculate price deviation from MA
            bool isPriceAboveMA = candle.ClosePrice > smaValue;
            bool isPriceBelowMA = candle.ClosePrice < smaValue;
            
            // Calculate stop-loss amount based on ATR
            decimal stopLossAmount = atrValue * AtrMultiplier;

            if (Position == 0)
            {
                // No position - check for entry signals
                if (isPriceBelowMA)
                {
                    // Price is below MA in low volatility period - buy (long)
                    BuyMarket(Volume);
                }
                else if (isPriceAboveMA)
                {
                    // Price is above MA in low volatility period - sell (short)
                    SellMarket(Volume);
                }
            }
            else if (Position > 0)
            {
                // Long position - check for exit signal
                if (candle.ClosePrice > smaValue)
                {
                    // Price has reached MA - exit long
                    SellMarket(Position);
                }
            }
            else if (Position < 0)
            {
                // Short position - check for exit signal
                if (candle.ClosePrice < smaValue)
                {
                    // Price has reached MA - exit short
                    BuyMarket(Math.Abs(Position));
                }
            }
        }
    }
}
