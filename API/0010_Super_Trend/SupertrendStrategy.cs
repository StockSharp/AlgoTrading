using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Supertrend indicator.
    /// It enters long position when price is above Supertrend line and short position when price is below Supertrend line.
    /// </summary>
    public class SupertrendStrategy : Strategy
    {
        private readonly StrategyParam<int> _period;
        private readonly StrategyParam<decimal> _multiplier;
        private readonly StrategyParam<DataType> _candleType;

        // Current state tracking
        private bool _prevIsPriceAboveSupertrend;
        private decimal _prevSupertrendValue;

        /// <summary>
        /// Period for Supertrend calculation.
        /// </summary>
        public int Period
        {
            get => _period.Value;
            set => _period.Value = value;
        }

        /// <summary>
        /// Multiplier for Supertrend calculation.
        /// </summary>
        public decimal Multiplier
        {
            get => _multiplier.Value;
            set => _multiplier.Value = value;
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
        /// Initialize the Supertrend strategy.
        /// </summary>
        public SupertrendStrategy()
        {
            _period = Param(nameof(Period), 10)
                .SetDisplay("Period", "Period for Supertrend calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 2);

            _multiplier = Param(nameof(Multiplier), 3.0m)
                .SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(2.0m, 4.0m, 0.5m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles to use", "General");
                
            _prevIsPriceAboveSupertrend = false;
            _prevSupertrendValue = 0;
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

            // Create custom supertrend indicator
            // Since StockSharp doesn't have a built-in Supertrend indicator,
            // we'll use ATR to calculate the basic components
            var atr = new AverageTrueRange { Length = Period };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            // We'll process candles manually and calculate Supertrend in the handler
            subscription
                .Bind(atr, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate Supertrend components
            var medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
            var basicUpperBand = medianPrice + Multiplier * atrValue;
            var basicLowerBand = medianPrice - Multiplier * atrValue;

            // We need to track previous values to implement the Supertrend logic
            decimal supertrendValue;

            // If this is the first processed candle, initialize values
            if (_prevSupertrendValue == 0)
            {
                supertrendValue = candle.ClosePrice > medianPrice ? basicLowerBand : basicUpperBand;
                _prevSupertrendValue = supertrendValue;
                _prevIsPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
                return;
            }

            // Determine current Supertrend value based on previous value and current price
            if (_prevSupertrendValue <= candle.HighPrice)
            {
                // Previous Supertrend was resistance
                supertrendValue = Math.Max(basicLowerBand, _prevSupertrendValue);
            }
            else if (_prevSupertrendValue >= candle.LowPrice)
            {
                // Previous Supertrend was support
                supertrendValue = Math.Min(basicUpperBand, _prevSupertrendValue);
            }
            else
            {
                // Price crossed the Supertrend
                supertrendValue = candle.ClosePrice > _prevSupertrendValue ? basicLowerBand : basicUpperBand;
            }

            // Check if price is above or below Supertrend
            var isPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
            
            // Detect crossovers
            var isCrossedAbove = isPriceAboveSupertrend && !_prevIsPriceAboveSupertrend;
            var isCrossedBelow = !isPriceAboveSupertrend && _prevIsPriceAboveSupertrend;

            // Trading logic
            if (isCrossedAbove && Position <= 0)
            {
                // Price crossed above Supertrend - Buy signal
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                LogInfo($"Buy signal: Price ({candle.ClosePrice}) crossed above Supertrend ({supertrendValue})");
            }
            else if (isCrossedBelow && Position >= 0)
            {
                // Price crossed below Supertrend - Sell signal
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                LogInfo($"Sell signal: Price ({candle.ClosePrice}) crossed below Supertrend ({supertrendValue})");
            }
            // Exit logic for existing positions
            else if (isCrossedBelow && Position > 0)
            {
                // Exit long position
                SellMarket(Position);
                LogInfo($"Exit long: Price ({candle.ClosePrice}) crossed below Supertrend ({supertrendValue})");
            }
            else if (isCrossedAbove && Position < 0)
            {
                // Exit short position
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Price ({candle.ClosePrice}) crossed above Supertrend ({supertrendValue})");
            }

            // Update state for the next candle
            _prevSupertrendValue = supertrendValue;
            _prevIsPriceAboveSupertrend = isPriceAboveSupertrend;
        }
    }
}