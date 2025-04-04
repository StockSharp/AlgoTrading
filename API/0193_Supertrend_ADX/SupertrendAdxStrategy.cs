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
    /// Strategy based on Supertrend and ADX indicators.
    /// Enters long when price is above Supertrend and ADX > 25.
    /// Enters short when price is below Supertrend and ADX > 25.
    /// Exits when price crosses Supertrend in the opposite direction.
    /// </summary>
    public class SupertrendAdxStrategy : Strategy
    {
        private readonly StrategyParam<int> _supertrendPeriod;
        private readonly StrategyParam<decimal> _supertrendMultiplier;
        private readonly StrategyParam<int> _adxPeriod;
        private readonly StrategyParam<DataType> _candleType;

        private AverageTrueRange _atr;
        private AverageDirectionalMovementIndex _adx;
        
        private decimal _supertrendValue;
        private decimal _prevClose;
        private Sides _supertrendDirection;

        /// <summary>
        /// Supertrend ATR period.
        /// </summary>
        public int SupertrendPeriod
        {
            get => _supertrendPeriod.Value;
            set => _supertrendPeriod.Value = value;
        }

        /// <summary>
        /// Supertrend multiplier.
        /// </summary>
        public decimal SupertrendMultiplier
        {
            get => _supertrendMultiplier.Value;
            set => _supertrendMultiplier.Value = value;
        }

        /// <summary>
        /// ADX indicator period.
        /// </summary>
        public int AdxPeriod
        {
            get => _adxPeriod.Value;
            set => _adxPeriod.Value = value;
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
        /// Initializes a new instance of the <see cref="SupertrendAdxStrategy"/>.
        /// </summary>
        public SupertrendAdxStrategy()
        {
            _supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
                .SetDisplayName("Supertrend Period")
                .SetDescription("Period for ATR calculation in Supertrend")
                .SetCategories("Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 14, 1);

            _supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
                .SetDisplayName("Supertrend Multiplier")
                .SetDescription("ATR multiplier for Supertrend calculation")
                .SetCategories("Indicators")
                .SetCanOptimize(true)
                .SetOptimize(2m, 4m, 0.5m);

            _adxPeriod = Param(nameof(AdxPeriod), 14)
                .SetDisplayName("ADX Period")
                .SetDescription("Period for Average Directional Movement Index")
                .SetCategories("Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
                .SetDisplayName("Candle Type")
                .SetDescription("Timeframe of data for strategy")
                .SetCategories("General");
        }

        /// <inheritdoc />
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            return new[] { (Security, CandleType) };
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Create indicators
            _atr = new AverageTrueRange { Length = SupertrendPeriod };
            _adx = new AverageDirectionalMovementIndex { Length = AdxPeriod };

            // Initialize Supertrend variables
            _supertrendValue = 0;
            _prevClose = 0;
            _supertrendDirection = Sides.Buy; // Default to bullish

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Process candles with indicators
            subscription
                .Bind(_atr, _adx, ProcessCandle)
                .Start();

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // Create custom line for Supertrend visualization
                var supertrendLine = area.CreateIndicator<Supertrend>("Supertrend");
                
                DrawIndicator(area, _adx);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal atr, decimal adx)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Calculate Supertrend
            var medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
            var upperBand = medianPrice + SupertrendMultiplier * atr;
            var lowerBand = medianPrice - SupertrendMultiplier * atr;

            // Initialize Supertrend on first candle
            if (_supertrendValue == 0)
            {
                _supertrendValue = medianPrice;
                _prevClose = candle.ClosePrice;
                return;
            }

            // Update Supertrend value based on previous direction and current price
            if (_supertrendDirection == Sides.Buy)
            {
                // Previous trend was up
                _supertrendValue = Math.Max(lowerBand, _supertrendValue);
                
                // Check for trend change
                if (candle.ClosePrice < _supertrendValue)
                {
                    _supertrendDirection = Sides.Sell;
                    _supertrendValue = upperBand;
                }
            }
            else
            {
                // Previous trend was down
                _supertrendValue = Math.Min(upperBand, _supertrendValue);
                
                // Check for trend change
                if (candle.ClosePrice > _supertrendValue)
                {
                    _supertrendDirection = Sides.Buy;
                    _supertrendValue = lowerBand;
                }
            }

            // Check if strategy is ready for trading
            if (!IsFormedAndOnlineAndAllowTrading())
            {
                _prevClose = candle.ClosePrice;
                return;
            }

            // Trading logic
            if (adx > 25)
            {
                // Strong trend detected
                if (_supertrendDirection == Sides.Buy && candle.ClosePrice > _supertrendValue && Position <= 0)
                {
                    // Bullish Supertrend with strong ADX - go long
                    BuyMarket(Volume + Math.Abs(Position));
                }
                else if (_supertrendDirection == Sides.Sell && candle.ClosePrice < _supertrendValue && Position >= 0)
                {
                    // Bearish Supertrend with strong ADX - go short
                    SellMarket(Volume + Math.Abs(Position));
                }
            }

            // Exit logic based on Supertrend changes
            if (Position > 0 && _supertrendDirection == Sides.Sell)
            {
                // Exit long position when Supertrend turns bearish
                ClosePosition();
            }
            else if (Position < 0 && _supertrendDirection == Sides.Buy)
            {
                // Exit short position when Supertrend turns bullish
                ClosePosition();
            }

            // Update trailing stops based on Supertrend
            if (Position != 0)
            {
                StartProtection(
                    new Unit(0), // No take profit - let Supertrend handle exit
                    new Unit(_supertrendValue, UnitTypes.Absolute),
                    Position > 0 ? false : true); // Trailing stop only for long positions
            }

            _prevClose = candle.ClosePrice;