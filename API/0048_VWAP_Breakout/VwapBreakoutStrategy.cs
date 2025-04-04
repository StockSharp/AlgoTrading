using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// VWAP Breakout strategy
    /// Long entry: Price breaks above VWAP
    /// Short entry: Price breaks below VWAP
    /// Exit when price crosses back through VWAP
    /// </summary>
    public class VWAPBreakoutStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private decimal _previousClosePrice;
        private decimal _previousVWAP;
        private bool _isFirstCandle;

        /// <summary>
        /// Candle type for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize <see cref="VWAPBreakoutStrategy"/>.
        /// </summary>
        public VWAPBreakoutStrategy()
        {
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _previousClosePrice = 0;
            _previousVWAP = 0;
            _isFirstCandle = true;
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

            // Create VWAP indicator
            var vwap = new VolumeWeightedMovingAverage();

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .BindEx(vwap, ProcessCandle)
                .Start();

            // Configure protection
            StartProtection(
                takeProfit: new Unit(3, UnitTypes.Percent),
                stopLoss: new Unit(2, UnitTypes.Percent)
            );

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, vwap);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwapValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Extract VWAP value from indicator result
            var vwapPrice = vwapValue.GetValue<decimal>();
            
            // Skip the first candle, just initialize values
            if (_isFirstCandle)
            {
                _previousClosePrice = candle.ClosePrice;
                _previousVWAP = vwapPrice;
                _isFirstCandle = false;
                return;
            }
            
            // Check for VWAP breakouts
            var breakoutUp = _previousClosePrice <= _previousVWAP && candle.ClosePrice > vwapPrice;
            var breakoutDown = _previousClosePrice >= _previousVWAP && candle.ClosePrice < vwapPrice;
            
            // Log current values
            LogInfo($"Candle Close: {candle.ClosePrice}, VWAP: {vwapPrice}");
            LogInfo($"Previous Close: {_previousClosePrice}, Previous VWAP: {_previousVWAP}");
            LogInfo($"Breakout Up: {breakoutUp}, Breakout Down: {breakoutDown}");

            // Trading logic:
            // Long: Price breaks above VWAP
            if (breakoutUp && Position <= 0)
            {
                LogInfo($"Buy Signal: Price ({candle.ClosePrice}) breaking above VWAP ({vwapPrice})");
                BuyMarket(Volume + Math.Abs(Position));
            }
            // Short: Price breaks below VWAP
            else if (breakoutDown && Position >= 0)
            {
                LogInfo($"Sell Signal: Price ({candle.ClosePrice}) breaking below VWAP ({vwapPrice})");
                SellMarket(Volume + Math.Abs(Position));
            }
            
            // Exit logic: Price crosses back through VWAP
            if (Position > 0 && candle.ClosePrice < vwapPrice)
            {
                LogInfo($"Exit Long: Price ({candle.ClosePrice}) < VWAP ({vwapPrice})");
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > vwapPrice)
            {
                LogInfo($"Exit Short: Price ({candle.ClosePrice}) > VWAP ({vwapPrice})");
                BuyMarket(Math.Abs(Position));
            }

            // Store current values for next comparison
            _previousClosePrice = candle.ClosePrice;
            _previousVWAP = vwapPrice;
        }
    }
}