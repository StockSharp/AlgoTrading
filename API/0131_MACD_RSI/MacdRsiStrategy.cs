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
    /// Strategy that combines MACD and RSI indicators to identify potential trading opportunities.
    /// It looks for trend direction with MACD and enters on extreme RSI values in the trend direction.
    /// </summary>
    public class MacdRsiStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _macdFast;
        private readonly StrategyParam<int> _macdSlow;
        private readonly StrategyParam<int> _macdSignal;
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
        /// Fast period for MACD calculation.
        /// </summary>
        public int MacdFast
        {
            get => _macdFast.Value;
            set => _macdFast.Value = value;
        }
        
        /// <summary>
        /// Slow period for MACD calculation.
        /// </summary>
        public int MacdSlow
        {
            get => _macdSlow.Value;
            set => _macdSlow.Value = value;
        }
        
        /// <summary>
        /// Signal period for MACD calculation.
        /// </summary>
        public int MacdSignal
        {
            get => _macdSignal.Value;
            set => _macdSignal.Value = value;
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
        /// Initializes a new instance of the <see cref="MacdRsiStrategy"/>.
        /// </summary>
        public MacdRsiStrategy()
        {
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
                          .SetDisplay("Candle Type", "Type of candles to use", "General");
                          
            _macdFast = Param(nameof(MacdFast), 12)
                        .SetRange(5, 30)
                        .SetDisplay("MACD Fast", "Fast period for MACD calculation", "MACD Settings")
                        .SetCanOptimize(true);
                        
            _macdSlow = Param(nameof(MacdSlow), 26)
                        .SetRange(10, 50)
                        .SetDisplay("MACD Slow", "Slow period for MACD calculation", "MACD Settings")
                        .SetCanOptimize(true);
                        
            _macdSignal = Param(nameof(MacdSignal), 9)
                          .SetRange(3, 20)
                          .SetDisplay("MACD Signal", "Signal period for MACD calculation", "MACD Settings")
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
            var macd = new MovingAverageConvergenceDivergence
            {
                FastMa = new ExponentialMovingAverage { Length = MacdFast },
                SlowMa = new ExponentialMovingAverage { Length = MacdSlow },
                SignalMa = new ExponentialMovingAverage { Length = MacdSignal }
            };
            
            var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
            
            // Create candle subscription
            var subscription = SubscribeCandles(CandleType);
            
            // When both indicators are ready, process the candle
            subscription
                .Bind(macd, rsi, ProcessCandle)
                .Start();
                
            // Set up chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // Draw MACD in a separate area
                var macdArea = CreateChartArea();
                DrawIndicator(macdArea, macd);
                
                // Draw RSI in a separate area
                var rsiArea = CreateChartArea();
                DrawIndicator(rsiArea, rsi);
                
                DrawOwnTrades(area);
            }
        }
        
        /// <summary>
        /// Process incoming candle with MACD and RSI values.
        /// </summary>
        /// <param name="candle">Candle to process.</param>
        /// <param name="macdValue">MACD line value.</param>
        /// <param name="signalValue">MACD signal line value.</param>
        /// <param name="rsiValue">RSI value.</param>
        private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal rsiValue)
        {
            if (candle.State != CandleStates.Finished)
                return;
                
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
                
            // Trading logic: Combine MACD trend with RSI extreme values
            
            // MACD above signal line indicates uptrend
            bool isUptrend = macdValue > signalValue;
            
            // Check for entry conditions
            if (isUptrend && rsiValue < RsiOversold)
            {
                // Bullish trend with oversold RSI - Long signal
                if (Position <= 0)
                {
                    BuyMarket(Volume + Math.Abs(Position));
                    LogInfo($"Buy signal: MACD uptrend ({macdValue:F4} > {signalValue:F4}) with oversold RSI ({rsiValue:F2})");
                }
            }
            else if (!isUptrend && rsiValue > RsiOverbought)
            {
                // Bearish trend with overbought RSI - Short signal
                if (Position >= 0)
                {
                    SellMarket(Volume + Math.Abs(Position));
                    LogInfo($"Sell signal: MACD downtrend ({macdValue:F4} < {signalValue:F4}) with overbought RSI ({rsiValue:F2})");
                }
            }
            
            // Check for exit conditions
            if (Position > 0 && !isUptrend)
            {
                // Exit long when MACD crosses below signal line
                SellMarket(Math.Abs(Position));
                LogInfo($"Exit long: MACD crossed below signal ({macdValue:F4} < {signalValue:F4})");
            }
            else if (Position < 0 && isUptrend)
            {
                // Exit short when MACD crosses above signal line
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: MACD crossed above signal ({macdValue:F4} > {signalValue:F4})");
            }
        }
    }
}