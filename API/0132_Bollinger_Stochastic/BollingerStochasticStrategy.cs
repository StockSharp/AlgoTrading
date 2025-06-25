namespace StockSharp.Strategies.Samples
{
    using System;
    using System.Collections.Generic;
    
    using Ecng.Common;
    
    using StockSharp.Algo;
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Indicators;
    using StockSharp.Algo.Strategies;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
    
    /// <summary>
    /// Strategy that combines Bollinger Bands and Stochastic oscillator to identify
    /// potential mean-reversion trading opportunities when price is at extremes.
    /// </summary>
    public class BollingerStochasticStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _bollingerPeriod;
        private readonly StrategyParam<decimal> _bollingerDeviation;
        private readonly StrategyParam<int> _stochPeriod;
        private readonly StrategyParam<int> _stochK;
        private readonly StrategyParam<int> _stochD;
        private readonly StrategyParam<int> _stochOversold;
        private readonly StrategyParam<int> _stochOverbought;
        private readonly StrategyParam<decimal> _atrMultiplier;
        
        private Stochastic _stochastic;
        private BollingerBands _bollinger;
        private AverageTrueRange _atr;
        
        /// <summary>
        /// Data type for candles.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }
        
        /// <summary>
        /// Period for Bollinger Bands calculation.
        /// </summary>
        public int BollingerPeriod
        {
            get => _bollingerPeriod.Value;
            set => _bollingerPeriod.Value = value;
        }
        
        /// <summary>
        /// Standard deviation multiplier for Bollinger Bands.
        /// </summary>
        public decimal BollingerDeviation
        {
            get => _bollingerDeviation.Value;
            set => _bollingerDeviation.Value = value;
        }
        
        /// <summary>
        /// Period for Stochastic oscillator calculation.
        /// </summary>
        public int StochPeriod
        {
            get => _stochPeriod.Value;
            set => _stochPeriod.Value = value;
        }
        
        /// <summary>
        /// K period for Stochastic oscillator.
        /// </summary>
        public int StochK
        {
            get => _stochK.Value;
            set => _stochK.Value = value;
        }
        
        /// <summary>
        /// D period for Stochastic oscillator.
        /// </summary>
        public int StochD
        {
            get => _stochD.Value;
            set => _stochD.Value = value;
        }
        
        /// <summary>
        /// Stochastic oversold level.
        /// </summary>
        public int StochOversold
        {
            get => _stochOversold.Value;
            set => _stochOversold.Value = value;
        }
        
        /// <summary>
        /// Stochastic overbought level.
        /// </summary>
        public int StochOverbought
        {
            get => _stochOverbought.Value;
            set => _stochOverbought.Value = value;
        }
        
        /// <summary>
        /// ATR multiplier for stop-loss calculation.
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BollingerStochasticStrategy"/>.
        /// </summary>
        public BollingerStochasticStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                          .SetDisplay("Candle Type", "Type of candles to use", "General");
                          
            _bollingerPeriod = Param(nameof(BollingerPeriod), 20)
                               .SetRange(10, 50)
                               .SetDisplay("BB Period", "Period for Bollinger Bands calculation", "Bollinger Settings")
                               .SetCanOptimize(true);
                               
            _bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
                                  .SetRange(1.0m, 3.0m)
                                  .SetDisplay("BB Deviation", "Standard deviation multiplier for Bollinger Bands", "Bollinger Settings")
                                  .SetCanOptimize(true);
                                  
            _stochPeriod = Param(nameof(StochPeriod), 14)
                           .SetRange(5, 30)
                           .SetDisplay("Stoch Period", "Period for Stochastic oscillator calculation", "Stochastic Settings")
                           .SetCanOptimize(true);
                           
            _stochK = Param(nameof(StochK), 3)
                      .SetRange(1, 10)
                      .SetDisplay("Stoch %K", "K period for Stochastic oscillator", "Stochastic Settings")
                      .SetCanOptimize(true);
                      
            _stochD = Param(nameof(StochD), 3)
                      .SetRange(1, 10)
                      .SetDisplay("Stoch %D", "D period for Stochastic oscillator", "Stochastic Settings")
                      .SetCanOptimize(true);
                      
            _stochOversold = Param(nameof(StochOversold), 20)
                             .SetRange(5, 30)
                             .SetDisplay("Oversold Level", "Stochastic oversold level", "Stochastic Settings")
                             .SetCanOptimize(true);
                             
            _stochOverbought = Param(nameof(StochOverbought), 80)
                               .SetRange(70, 95)
                               .SetDisplay("Overbought Level", "Stochastic overbought level", "Stochastic Settings")
                               .SetCanOptimize(true);
                               
            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                            .SetRange(1.0m, 5.0m)
                            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management");
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
            
            // Initialize indicators
            _bollinger = new BollingerBands
            {
                Length = BollingerPeriod,
                Width = BollingerDeviation
            };
            
            _stochastic = new Stochastic
            {
                KPeriod = StochPeriod,
                DPeriod = StochD,
                KSmaPeriod = StochK
            };
            
            _atr = new AverageTrueRange
            {
                Length = 14
            };
            
            // Create candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind the indicators and candle processor
            subscription
                .BindEx(_bollinger, _stochastic, _atr, ProcessCandle)
                .Start();
                
            // Set up chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _bollinger);
                
                // Draw Stochastic in a separate area
                var stochArea = CreateChartArea();
                DrawIndicator(stochArea, _stochastic);
                
                DrawOwnTrades(area);
            }
        }
        
        /// <summary>
        /// Process incoming candle with indicator values.
        /// </summary>
        /// <param name="candle">Candle to process.</param>
        /// <param name="bollingerValue">Bollinger Bands value.</param>
        /// <param name="stochasticValue">Stochastic oscillator value.</param>
        /// <param name="atrValue">ATR value.</param>
        private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue stochasticValue, IIndicatorValue atrValue)
        {
            if (candle.State != CandleStates.Finished)
                return;
                
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
                
            // Extract values from indicators
            var middleBand = bollingerValue.GetValue<decimal>();
            var upperBand = _bollinger.GetUpBand();
            var lowerBand = _bollinger.GetLowBand();
            
            var k = stochasticValue.GetValue<decimal>();
            var d = _stochastic.D.Current;
            
            var atrValue_ = atrValue.GetValue<decimal>();
            
            // Calculate stop loss distance based on ATR
            var stopLossDistance = atrValue_ * AtrMultiplier;
            
            // Trading logic for long positions
            if (candle.ClosePrice < lowerBand && k < StochOversold)
            {
                // Price below lower Bollinger Band and Stochastic in oversold region - Long signal
                if (Position <= 0)
                {
                    BuyMarket(Volume + Math.Abs(Position));
                    LogInfo($"Buy signal: Price ({candle.ClosePrice}) below lower BB ({lowerBand:F4}) with oversold Stochastic ({k:F2})");
                    
                    // Set stop loss
                    var stopPrice = candle.ClosePrice - stopLossDistance;
                    RegisterOrder(CreateOrder(Sides.Sell, stopPrice, Math.Abs(Position + Volume)));
                }
            }
            // Trading logic for short positions
            else if (candle.ClosePrice > upperBand && k > StochOverbought)
            {
                // Price above upper Bollinger Band and Stochastic in overbought region - Short signal
                if (Position >= 0)
                {
                    SellMarket(Volume + Math.Abs(Position));
                    LogInfo($"Sell signal: Price ({candle.ClosePrice}) above upper BB ({upperBand:F4}) with overbought Stochastic ({k:F2})");
                    
                    // Set stop loss
                    var stopPrice = candle.ClosePrice + stopLossDistance;
                    RegisterOrder(CreateOrder(Sides.Buy, stopPrice, Math.Abs(Position + Volume)));
                }
            }
            
            // Exit conditions
            if (Position > 0)
            {
                // Exit long when price crosses above middle band
                if (candle.ClosePrice > middleBand)
                {
                    SellMarket(Math.Abs(Position));
                    LogInfo($"Exit long: Price ({candle.ClosePrice}) crossed above middle BB ({middleBand:F4})");
                }
            }
            else if (Position < 0)
            {
                // Exit short when price crosses below middle band
                if (candle.ClosePrice < middleBand)
                {
                    BuyMarket(Math.Abs(Position));
                    LogInfo($"Exit short: Price ({candle.ClosePrice}) crossed below middle BB ({middleBand:F4})");
                }
            }
        }
    }
}