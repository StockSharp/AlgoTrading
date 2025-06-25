using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy #77: Inside Bar Breakout strategy.
    /// The strategy looks for inside bar patterns (a bar with high lower than the previous bar's high and low higher than the previous bar's low)
    /// and enters positions on breakouts of the inside bar's high or low.
    /// </summary>
    public class InsideBarBreakoutStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;
        
        private ICandleMessage _previousCandle;
        private ICandleMessage _insideCandle;
        private bool _waitingForBreakout = false;

        /// <summary>
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public InsideBarBreakoutStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
                .SetNotNegative()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(0.5m, 2.0m, 0.5m);
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

            // Reset variables
            _previousCandle = null;
            _insideCandle = null;
            _waitingForBreakout = false;

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // First candle - just store it
            if (_previousCandle == null)
            {
                _previousCandle = candle;
                return;
            }

            // Check if we're waiting for a breakout of an inside bar
            if (_waitingForBreakout)
            {
                // Check for breakout of inside bar's high or low
                if (candle.HighPrice > _insideCandle.HighPrice)
                {
                    // Breakout above inside bar's high - bullish signal
                    if (Position <= 0)
                    {
                        CancelActiveOrders();
                        BuyMarket(Volume + Math.Abs(Position));
                        LogInfo($"Long entry at {candle.ClosePrice} on breakout above inside bar high {_insideCandle.HighPrice}");
                        
                        // Set stop-loss below inside bar's low
                        StartProtection(
                            takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on exit logic
                            stopLoss: new Unit(_insideCandle.LowPrice * (1 - StopLossPercent / 100), UnitTypes.Absolute)
                        );
                    }
                    _waitingForBreakout = false;
                }
                else if (candle.LowPrice < _insideCandle.LowPrice)
                {
                    // Breakout below inside bar's low - bearish signal
                    if (Position >= 0)
                    {
                        CancelActiveOrders();
                        SellMarket(Volume + Math.Abs(Position));
                        LogInfo($"Short entry at {candle.ClosePrice} on breakout below inside bar low {_insideCandle.LowPrice}");
                        
                        // Set stop-loss above inside bar's high
                        StartProtection(
                            takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on exit logic
                            stopLoss: new Unit(_insideCandle.HighPrice * (1 + StopLossPercent / 100), UnitTypes.Absolute)
                        );
                    }
                    _waitingForBreakout = false;
                }
            }

            // Check if current candle is an inside bar compared to previous candle
            bool isInsideBar = IsInsideBar(_previousCandle, candle);
            
            if (isInsideBar)
            {
                _insideCandle = candle;
                _waitingForBreakout = true;
                LogInfo($"Inside bar detected: High {candle.HighPrice} < Previous High {_previousCandle.HighPrice}, " +
                               $"Low {candle.LowPrice} > Previous Low {_previousCandle.LowPrice}");
            }

            // Update previous candle for next iteration
            _previousCandle = candle;

            // Exit logic if we have an open position but not waiting for a breakout
            if (!_waitingForBreakout)
            {
                // For long positions, exit if the price drops below the previous candle's low
                if (Position > 0 && candle.LowPrice < _previousCandle.LowPrice)
                {
                    SellMarket(Math.Abs(Position));
                    LogInfo($"Long exit at {candle.ClosePrice} (price below previous candle low {_previousCandle.LowPrice})");
                }
                // For short positions, exit if the price rises above the previous candle's high
                else if (Position < 0 && candle.HighPrice > _previousCandle.HighPrice)
                {
                    BuyMarket(Math.Abs(Position));
                    LogInfo($"Short exit at {candle.ClosePrice} (price above previous candle high {_previousCandle.HighPrice})");
                }
            }
        }

        private bool IsInsideBar(ICandleMessage previous, ICandleMessage current)
        {
            // An inside bar has its high lower than the previous candle's high
            // and its low higher than the previous candle's low
            return current.HighPrice < previous.HighPrice && current.LowPrice > previous.LowPrice;
        }
    }
}