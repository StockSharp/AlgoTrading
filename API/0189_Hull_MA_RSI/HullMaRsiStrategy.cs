using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Hull Moving Average (HMA) for trend direction and RSI for entry signals.
    /// Buys when HMA is rising and RSI is in oversold territory.
    /// Sells when HMA is falling and RSI is in overbought territory.
    /// </summary>
    public class HullMaRsiStrategy : Strategy
    {
        private readonly StrategyParam<int> _hullPeriod;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<decimal> _rsiOversold;
        private readonly StrategyParam<decimal> _rsiOverbought;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossAtr;

        private HullMovingAverage _hullMa;
        private RelativeStrengthIndex _rsi;
        private AverageTrueRange _atr;
        
        private decimal _prevHull;
        private decimal _currHull;

        /// <summary>
        /// Period for Hull Moving Average.
        /// </summary>
        public int HullPeriod
        {
            get => _hullPeriod.Value;
            set => _hullPeriod.Value = value;
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
        /// Level below which RSI is considered oversold.
        /// </summary>
        public decimal RsiOversold
        {
            get => _rsiOversold.Value;
            set => _rsiOversold.Value = value;
        }

        /// <summary>
        /// Level above which RSI is considered overbought.
        /// </summary>
        public decimal RsiOverbought
        {
            get => _rsiOverbought.Value;
            set => _rsiOverbought.Value = value;
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
        /// Stop loss multiplier in terms of ATR.
        /// </summary>
        public decimal StopLossAtr
        {
            get => _stopLossAtr.Value;
            set => _stopLossAtr.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HullMaRsiStrategy"/>.
        /// </summary>
        public HullMaRsiStrategy()
        {
            _hullPeriod = Param(nameof(HullPeriod), 9)
                .SetDigits()
                .SetRange(5, 20, 1)
                .SetCanOptimize(true)
                .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators");

            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetDigits()
                .SetRange(7, 21, 1)
                .SetCanOptimize(true)
                .SetDisplay("RSI Period", "Period for Relative Strength Index", "Indicators");

            _rsiOversold = Param(nameof(RsiOversold), 30m)
                .SetRange(20m, 40m, 5m)
                .SetCanOptimize(true)
                .SetDisplay("RSI Oversold", "Level below which RSI is considered oversold", "Indicators");

            _rsiOverbought = Param(nameof(RsiOverbought), 70m)
                .SetRange(60m, 80m, 5m)
                .SetCanOptimize(true)
                .SetDisplay("RSI Overbought", "Level above which RSI is considered overbought", "Indicators");

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossAtr = Param(nameof(StopLossAtr), 2m)
                .SetRange(1m, 5m, 0.5m)
                .SetCanOptimize(true)
                .SetDisplay("Stop-Loss ATR", "Stop-loss multiplier in terms of ATR", "Risk Management");
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

            // Initialize indicators
            _hullMa = new HullMovingAverage
            {
                Length = HullPeriod
            };

            _rsi = new RelativeStrengthIndex
            {
                Length = RsiPeriod
            };

            _atr = new AverageTrueRange
            {
                Length = 14 // Standard ATR period
            };

            // Reset state variables
            _prevHull = 0;
            _currHull = 0;

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(_hullMa, _rsi, _atr, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _hullMa);
                
                var rsiArea = CreateChartArea();
                DrawIndicator(rsiArea, _rsi);
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal hullValue, decimal rsiValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Wait until strategy and indicators are ready
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Update Hull MA values
            _prevHull = _currHull;
            _currHull = hullValue;
            
            // Determine Hull MA direction
            var isHullRising = _currHull > _prevHull;
            
            // Get current price
            var currentPrice = candle.ClosePrice;
            
            // Calculate stop-loss distance
            var stopDistance = atrValue * StopLossAtr;

            // Buy condition: Hull MA rising and RSI shows oversold
            if (isHullRising && rsiValue < RsiOversold && Position <= 0)
            {
                // Cancel existing orders before entering new position
                CancelActiveOrders();
                
                // Enter long position
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                // Set stop loss
                var stopPrice = currentPrice - stopDistance;
                RegisterOrder(this.CreateOrder(Sides.Sell, stopPrice, volume));
                
                LogInfo($"Long entry signal: Hull MA rising ({_prevHull} to {_currHull}) with RSI oversold ({rsiValue})");
            }
            // Sell condition: Hull MA falling and RSI shows overbought
            else if (!isHullRising && rsiValue > RsiOverbought && Position >= 0)
            {
                // Cancel existing orders before entering new position
                CancelActiveOrders();
                
                // Enter short position
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                
                // Set stop loss
                var stopPrice = currentPrice + stopDistance;
                RegisterOrder(this.CreateOrder(Sides.Buy, stopPrice, volume));
                
                LogInfo($"Short entry signal: Hull MA falling ({_prevHull} to {_currHull}) with RSI overbought ({rsiValue})");
            }
            // Exit signals based on RSI reversal
            else if ((Position > 0 && rsiValue > 50) || (Position < 0 && rsiValue < 50))
            {
                // Close position on RSI reversal to neutral zone
                ClosePosition();
                
                LogInfo($"Exit signal: RSI returned to neutral zone. RSI: {rsiValue}");
            }
        }
    }
}