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
    /// Strategy that combines Ichimoku Cloud and RSI indicators to identify
    /// potential trading opportunities in trending markets with RSI confirmation.
    /// </summary>
    public class IchimokuRsiStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _tenkanPeriod;
        private readonly StrategyParam<int> _kijunPeriod;
        private readonly StrategyParam<int> _senkouSpanBPeriod;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<int> _rsiOversold;
        private readonly StrategyParam<int> _rsiOverbought;
        private readonly StrategyParam<decimal> _stopLossPercent;
        
        /// <summary>
        /// Data type for candles.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }
        
        /// <summary>
        /// Tenkan-sen (Conversion Line) period.
        /// </summary>
        public int TenkanPeriod
        {
            get => _tenkanPeriod.Value;
            set => _tenkanPeriod.Value = value;
        }
        
        /// <summary>
        /// Kijun-sen (Base Line) period.
        /// </summary>
        public int KijunPeriod
        {
            get => _kijunPeriod.Value;
            set => _kijunPeriod.Value = value;
        }
        
        /// <summary>
        /// Senkou Span B (2nd Leading Span) period.
        /// </summary>
        public int SenkouSpanBPeriod
        {
            get => _senkouSpanBPeriod.Value;
            set => _senkouSpanBPeriod.Value = value;
        }
        
        /// <summary>
        /// Period for RSI calculation.
        /// </summary>
        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set => _rsiPeriod.Value = value;
        }
        
        /// <summary>
        /// RSI oversold level.
        /// </summary>
        public int RsiOversold
        {
            get => _rsiOversold.Value;
            set => _rsiOversold.Value = value;
        }
        
        /// <summary>
        /// RSI overbought level.
        /// </summary>
        public int RsiOverbought
        {
            get => _rsiOverbought.Value;
            set => _rsiOverbought.Value = value;
        }
        
        /// <summary>
        /// Stop loss percentage from entry price.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IchimokuRsiStrategy"/>.
        /// </summary>
        public IchimokuRsiStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
                          .SetDisplay("Candle Type", "Type of candles to use", "General");
                          
            _tenkanPeriod = Param(nameof(TenkanPeriod), 9)
                            .SetRange(5, 30)
                            .SetDisplay("Tenkan Period", "Tenkan-sen (Conversion Line) period", "Ichimoku Settings")
                            .SetCanOptimize(true);
                            
            _kijunPeriod = Param(nameof(KijunPeriod), 26)
                           .SetRange(10, 50)
                           .SetDisplay("Kijun Period", "Kijun-sen (Base Line) period", "Ichimoku Settings")
                           .SetCanOptimize(true);
                           
            _senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
                                 .SetRange(30, 100)
                                 .SetDisplay("Senkou Span B Period", "Senkou Span B (2nd Leading Span) period", "Ichimoku Settings")
                                 .SetCanOptimize(true);
                                 
            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                         .SetRange(5, 30)
                         .SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")
                         .SetCanOptimize(true);
                         
            _rsiOversold = Param(nameof(RsiOversold), 30)
                           .SetRange(10, 40)
                           .SetDisplay("RSI Oversold", "RSI oversold level", "RSI Settings")
                           .SetCanOptimize(true);
                           
            _rsiOverbought = Param(nameof(RsiOverbought), 70)
                             .SetRange(60, 90)
                             .SetDisplay("RSI Overbought", "RSI overbought level", "RSI Settings")
                             .SetCanOptimize(true);
                             
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                               .SetRange(0.5m, 5m)
                               .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management");
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
            
            // Set up stop loss protection
            StartProtection(
                new Unit(0), // No take profit
                new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss based on parameter
            );
            
            // Create indicators
            var ichimoku = new Ichimoku
            {
                TenkanPeriod = TenkanPeriod,
                KijunPeriod = KijunPeriod,
                SenkouSpanBPeriod = SenkouSpanBPeriod
            };
            
            var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
            
            // Create candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind the indicators and candle processor
            subscription
                .BindEx(ichimoku, rsi, ProcessCandle)
                .Start();
                
            // Set up chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, ichimoku);
                
                // Draw RSI in a separate area
                var rsiArea = CreateChartArea();
                DrawIndicator(rsiArea, rsi);
                
                DrawOwnTrades(area);
            }
        }
        
        /// <summary>
        /// Process incoming candle with indicator values.
        /// </summary>
        /// <param name="candle">Candle to process.</param>
        /// <param name="ichimokuValue">Ichimoku value.</param>
        /// <param name="rsiValue">RSI value.</param>
        private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue rsiValue)
        {
            if (candle.State != CandleStates.Finished)
                return;
                
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
                
            // Extract values from Ichimoku indicator
            var ichimokuComplex = (IComplexValue)ichimokuValue;
            var tenkan = ichimokuComplex.InnerValues[0].GetValue<decimal>();        // Tenkan-sen (Conversion Line)
            var kijun = ichimokuComplex.InnerValues[1].GetValue<decimal>();         // Kijun-sen (Base Line)
            var senkouSpanA = ichimokuComplex.InnerValues[2].GetValue<decimal>();   // Senkou Span A (1st Leading Span)
            var senkouSpanB = ichimokuComplex.InnerValues[3].GetValue<decimal>();   // Senkou Span B (2nd Leading Span)
            var chikouSpan = ichimokuComplex.InnerValues[4].GetValue<decimal>();    // Chikou Span (Lagging Span)
            
            // Extract RSI value
            var rsiIndicatorValue = rsiValue.GetValue<decimal>();
            
            // Check cloud status (Kumo)
            bool priceAboveCloud = candle.ClosePrice > Math.Max(senkouSpanA, senkouSpanB);
            bool priceBelowCloud = candle.ClosePrice < Math.Min(senkouSpanA, senkouSpanB);
            bool bullishCloud = senkouSpanA > senkouSpanB;
            
            // Trading logic for long positions
            if (priceAboveCloud && tenkan > kijun && bullishCloud && rsiIndicatorValue < RsiOverbought)
            {
                // Price above cloud with bullish TK cross and bullish cloud, and RSI not overbought - Long signal
                if (Position <= 0)
                {
                    BuyMarket(Volume + Math.Abs(Position));
                    LogInfo($"Buy signal: Price above cloud, Tenkan > Kijun ({tenkan:F4} > {kijun:F4}), Bullish cloud, RSI = {rsiIndicatorValue:F2}");
                }
            }
            // Trading logic for short positions
            else if (priceBelowCloud && tenkan < kijun && !bullishCloud && rsiIndicatorValue > RsiOversold)
            {
                // Price below cloud with bearish TK cross and bearish cloud, and RSI not oversold - Short signal
                if (Position >= 0)
                {
                    SellMarket(Volume + Math.Abs(Position));
                    LogInfo($"Sell signal: Price below cloud, Tenkan < Kijun ({tenkan:F4} < {kijun:F4}), Bearish cloud, RSI = {rsiIndicatorValue:F2}");
                }
            }
            
            // Exit conditions
            if (Position > 0)
            {
                // Exit long if price crosses below Kijun-sen (Base Line)
                if (candle.ClosePrice < kijun)
                {
                    SellMarket(Math.Abs(Position));
                    LogInfo($"Exit long: Price ({candle.ClosePrice}) crossed below Kijun-sen ({kijun:F4})");
                }
                // Also exit if RSI becomes overbought
                else if (rsiIndicatorValue > RsiOverbought)
                {
                    SellMarket(Math.Abs(Position));
                    LogInfo($"Exit long: RSI overbought ({rsiIndicatorValue:F2})");
                }
            }
            else if (Position < 0)
            {
                // Exit short if price crosses above Kijun-sen (Base Line)
                if (candle.ClosePrice > kijun)
                {
                    BuyMarket(Math.Abs(Position));
                    LogInfo($"Exit short: Price ({candle.ClosePrice}) crossed above Kijun-sen ({kijun:F4})");
                }
                // Also exit if RSI becomes oversold
                else if (rsiIndicatorValue < RsiOversold)
                {
                    BuyMarket(Math.Abs(Position));
                    LogInfo($"Exit short: RSI oversold ({rsiIndicatorValue:F2})");
                }
            }
        }
    }
}