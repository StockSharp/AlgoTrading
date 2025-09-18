namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pending stop scalping strategy converted from the MQL4 expert "AK-47 Scalper EA".
/// Places directional stop orders around the market, manages timed trading windows, and trails protective stops.
/// </summary>
public class Ak47ScalperStrategy : Strategy
{
        /// <summary>
        /// Trading direction supported by the strategy.
        /// </summary>
        public enum TradeDirection
        {
                /// <summary>
                /// Place a sell-stop order below the market and manage a short position.
                /// </summary>
                SellStop,

                /// <summary>
                /// Place a buy-stop order above the market and manage a long position.
                /// </summary>
                BuyStop,
        }

        private readonly StrategyParam<bool> _useRiskPercent;
        private readonly StrategyParam<decimal> _riskPercent;
        private readonly StrategyParam<decimal> _fixedVolume;
        private readonly StrategyParam<decimal> _stopLossPips;
        private readonly StrategyParam<decimal> _takeProfitPips;
        private readonly StrategyParam<decimal> _maxSpreadPoints;
        private readonly StrategyParam<bool> _useTimeFilter;
        private readonly StrategyParam<int> _startHour;
        private readonly StrategyParam<int> _startMinute;
        private readonly StrategyParam<int> _endHour;
        private readonly StrategyParam<int> _endMinute;
        private readonly StrategyParam<TradeDirection> _direction;
        private readonly StrategyParam<DataType> _candleType;

        private decimal _pipSize;
        private Order? _pendingOrder;
        private Order? _stopLossOrder;
        private Order? _takeProfitOrder;
        private decimal? _pendingStopLossPrice;
        private decimal? _pendingTakeProfitPrice;
        private decimal _entryPrice;
        private decimal _bestBid;
        private decimal _bestAsk;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ak47ScalperStrategy"/> class.
        /// </summary>
        public Ak47ScalperStrategy()
        {
                _useRiskPercent = Param(nameof(UseRiskPercent), true)
                        .SetDisplay("Use risk percent", "Calculate volume from the configured risk percentage.", "Money Management");

                _riskPercent = Param(nameof(RiskPercent), 3m)
                        .SetGreaterOrEqualZero()
                        .SetDisplay("Risk percent", "Percentage of free capital converted to trade volume.", "Money Management");

                _fixedVolume = Param(nameof(FixedVolume), 0.01m)
                        .SetGreaterThanZero()
                        .SetDisplay("Fixed volume", "Base lot size used when risk percent is disabled.", "Money Management");

                _stopLossPips = Param(nameof(StopLossPips), 3.5m)
                        .SetGreaterOrEqualZero()
                        .SetDisplay("Stop loss (pips)", "Protective stop distance measured in pips.", "Risk");

                _takeProfitPips = Param(nameof(TakeProfitPips), 7m)
                        .SetGreaterOrEqualZero()
                        .SetDisplay("Take profit (pips)", "Profit target distance measured in pips.", "Risk");

                _maxSpreadPoints = Param(nameof(MaxSpreadPoints), 5m)
                        .SetGreaterOrEqualZero()
                        .SetDisplay("Max spread (points)", "Maximum allowed spread expressed in price points.", "Filters");

                _useTimeFilter = Param(nameof(UseTimeFilter), true)
                        .SetDisplay("Use time filter", "Restrict trading to the configured session window.", "Schedule");

                _startHour = Param(nameof(StartHour), 2)
                        .SetDisplay("Start hour", "Hour when the trading session opens.", "Schedule");

                _startMinute = Param(nameof(StartMinute), 30)
                        .SetDisplay("Start minute", "Minute when the trading session opens.", "Schedule");

                _endHour = Param(nameof(EndHour), 21)
                        .SetDisplay("End hour", "Hour when the trading session closes.", "Schedule");

                _endMinute = Param(nameof(EndMinute), 0)
                        .SetDisplay("End minute", "Minute when the trading session closes.", "Schedule");

                _direction = Param(nameof(Direction), TradeDirection.SellStop)
                        .SetDisplay("Direction", "Pending order direction reproduced from the original EA.", "Trading");

                _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
                        .SetDisplay("Candle type", "Primary timeframe used to drive management events.", "General");
        }

        /// <summary>
        /// When <c>true</c>, the trade volume is derived from <see cref="RiskPercent"/>; otherwise <see cref="FixedVolume"/> is used.
        /// </summary>
        public bool UseRiskPercent
        {
                get => _useRiskPercent.Value;
                set => _useRiskPercent.Value = value;
        }

        /// <summary>
        /// Risk percentage applied to the free capital for volume calculation.
        /// </summary>
        public decimal RiskPercent
        {
                get => _riskPercent.Value;
                set => _riskPercent.Value = value;
        }

        /// <summary>
        /// Base lot size used when the percentage risk model is disabled.
        /// </summary>
        public decimal FixedVolume
        {
                get => _fixedVolume.Value;
                set => _fixedVolume.Value = value;
        }

        /// <summary>
        /// Stop-loss distance measured in pips.
        /// </summary>
        public decimal StopLossPips
        {
                get => _stopLossPips.Value;
                set => _stopLossPips.Value = value;
        }

        /// <summary>
        /// Take-profit distance measured in pips (set to zero to disable).
        /// </summary>
        public decimal TakeProfitPips
        {
                get => _takeProfitPips.Value;
                set => _takeProfitPips.Value = value;
        }

        /// <summary>
        /// Maximum allowed spread expressed in price points.
        /// </summary>
        public decimal MaxSpreadPoints
        {
                get => _maxSpreadPoints.Value;
                set => _maxSpreadPoints.Value = value;
        }

        /// <summary>
        /// Enables the trading session filter.
        /// </summary>
        public bool UseTimeFilter
        {
                get => _useTimeFilter.Value;
                set => _useTimeFilter.Value = value;
        }

        /// <summary>
        /// Hour when the trading session starts.
        /// </summary>
        public int StartHour
        {
                get => _startHour.Value;
                set => _startHour.Value = value;
        }

        /// <summary>
        /// Minute when the trading session starts.
        /// </summary>
        public int StartMinute
        {
                get => _startMinute.Value;
                set => _startMinute.Value = value;
        }

        /// <summary>
        /// Hour when the trading session ends.
        /// </summary>
        public int EndHour
        {
                get => _endHour.Value;
                set => _endHour.Value = value;
        }

        /// <summary>
        /// Minute when the trading session ends.
        /// </summary>
        public int EndMinute
        {
                get => _endMinute.Value;
                set => _endMinute.Value = value;
        }

        /// <summary>
        /// Direction of the pending order created by the strategy.
        /// </summary>
        public TradeDirection Direction
        {
                get => _direction.Value;
                set => _direction.Value = value;
        }

        /// <summary>
        /// Candle type used to drive the management cycle.
        /// </summary>
        public DataType CandleType
        {
                get => _candleType.Value;
                set => _candleType.Value = value;
        }

        /// <inheritdoc />
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
                return [(Security, CandleType)];
        }

        /// <inheritdoc />
        protected override void OnReseted()
        {
                base.OnReseted();

                _pipSize = 0m;
                _pendingOrder = null;
                _stopLossOrder = null;
                _takeProfitOrder = null;
                _pendingStopLossPrice = null;
                _pendingTakeProfitPrice = null;
                _entryPrice = 0m;
                _bestBid = 0m;
                _bestAsk = 0m;
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                _pipSize = CalculatePipSize();

                StartProtection();

                var subscription = SubscribeCandles(CandleType);
                subscription
                        .Bind(ProcessCandle)
                        .Start();

                // Track the best bid/ask in order to reproduce the spread filter and trailing logic.
                SubscribeOrderBook()
                        .Bind(depth =>
                        {
                                _bestBid = depth.GetBestBid()?.Price ?? _bestBid;
                                _bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
                        })
                        .Start();
        }

        private void ProcessCandle(ICandleMessage candle)
        {
                // Process only fully formed candles to stay aligned with the original EA tick logic.
                if (candle.State != CandleStates.Finished)
                        return;

                if (!IsFormedAndOnlineAndAllowTrading())
                        return;

                // Adjust protective orders before looking for new entries.
                ManageTrailingStops(candle);

                if (Position != 0m)
                        return;

                if (_pendingOrder != null && _pendingOrder.State == OrderStates.Active)
                {
                        UpdatePendingOrder(candle);
                        return;
                }

                if (!IsWithinTradingWindow(candle.OpenTime))
                {
                        CancelPendingOrder();
                        return;
                }

                if (!IsSpreadAcceptable())
                        return;

                PlacePendingOrder(candle);
        }

        /// <inheritdoc />
        protected override void OnOwnTradeReceived(MyTrade trade)
        {
                base.OnOwnTradeReceived(trade);

                if (trade.Order == null)
                        return;

                if (trade.Order == _pendingOrder)
                {
                        // Determine the resulting position volume for the protection orders.
                        var volume = Math.Abs(Position);
                        if (volume <= 0m)
                                volume = trade.Trade.Volume;

                        _entryPrice = trade.Trade.Price;

                        CancelProtectionOrders();

                        var stopLossPrice = CalculateStopLossPrice(Direction, _entryPrice);
                        var takeProfitPrice = CalculateTakeProfitPrice(Direction, _entryPrice);

                        // Recreate the stop-loss order that mirrors the MetaTrader behaviour.
                        if (StopLossPips > 0m && stopLossPrice.HasValue)
                                _stopLossOrder = Direction == TradeDirection.BuyStop
                                        ? SellStop(volume, stopLossPrice.Value)
                                        : BuyStop(volume, stopLossPrice.Value);

                        // Recreate the take-profit order if the user configured one.
                        if (TakeProfitPips > 0m && takeProfitPrice.HasValue)
                                _takeProfitOrder = Direction == TradeDirection.BuyStop
                                        ? SellLimit(volume, takeProfitPrice.Value)
                                        : BuyLimit(volume, takeProfitPrice.Value);

                        _pendingOrder = null;
                        _pendingStopLossPrice = null;
                        _pendingTakeProfitPrice = null;
                }
                else if (trade.Order == _stopLossOrder || trade.Order == _takeProfitOrder)
                {
                        CancelProtectionOrders();
                        _entryPrice = 0m;
                        CancelPendingOrder();
                }
        }

        private void PlacePendingOrder(ICandleMessage candle)
        {
                var volume = CalculateVolume();
                if (volume <= 0m)
                        return;

                var ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;
                var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;

                // Pending orders are placed half the stop distance away, exactly like in the EA.
                var halfStop = StopLossPips > 0m ? StopLossPips * _pipSize / 2m : 0m;
                decimal price;

                if (Direction == TradeDirection.SellStop)
                {
                        price = NormalizePrice(bid - halfStop);
                        var stopLoss = StopLossPips > 0m ? NormalizePrice(ask + halfStop) : (decimal?)null;
                        var takeProfit = TakeProfitPips > 0m ? NormalizePrice(price - TakeProfitPips * _pipSize) : (decimal?)null;

                        if (price <= 0m)
                                return;

                        _pendingOrder = SellStop(volume, price);
                        _pendingStopLossPrice = stopLoss;
                        _pendingTakeProfitPrice = takeProfit;
                }
                else
                {
                        price = NormalizePrice(ask + halfStop);
                        var stopLoss = StopLossPips > 0m ? NormalizePrice(bid - halfStop) : (decimal?)null;
                        var takeProfit = TakeProfitPips > 0m ? NormalizePrice(price + TakeProfitPips * _pipSize) : (decimal?)null;

                        if (price <= 0m)
                                return;

                        _pendingOrder = BuyStop(volume, price);
                        _pendingStopLossPrice = stopLoss;
                        _pendingTakeProfitPrice = takeProfit;
                }
        }

        private void UpdatePendingOrder(ICandleMessage candle)
        {
                if (_pendingOrder == null || _pendingOrder.State != OrderStates.Active)
                        return;

                if (StopLossPips <= 0m)
                        return;

                var ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;
                var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;
                var halfStop = StopLossPips * _pipSize / 2m;
                var step = Security?.PriceStep ?? _pipSize;

                decimal desiredPrice;
                decimal? newStopLoss;
                decimal? newTakeProfit;

                if (Direction == TradeDirection.SellStop)
                {
                        desiredPrice = NormalizePrice(bid - halfStop);
                        newStopLoss = NormalizePrice(ask + halfStop);
                        newTakeProfit = TakeProfitPips > 0m ? NormalizePrice(desiredPrice - TakeProfitPips * _pipSize) : null;

                        if (Math.Abs(desiredPrice - _pendingOrder.Price) <= step / 2m)
                                return;

                        var volume = _pendingOrder.Balance > 0m ? _pendingOrder.Balance : _pendingOrder.Volume;
                        CancelPendingOrder();
                        _pendingOrder = SellStop(volume, desiredPrice);
                }
                else
                {
                        desiredPrice = NormalizePrice(ask + halfStop);
                        newStopLoss = NormalizePrice(bid - halfStop);
                        newTakeProfit = TakeProfitPips > 0m ? NormalizePrice(desiredPrice + TakeProfitPips * _pipSize) : null;

                        if (Math.Abs(desiredPrice - _pendingOrder.Price) <= step / 2m)
                                return;

                        var volume = _pendingOrder.Balance > 0m ? _pendingOrder.Balance : _pendingOrder.Volume;
                        CancelPendingOrder();
                        _pendingOrder = BuyStop(volume, desiredPrice);
                }

                _pendingStopLossPrice = newStopLoss;
                _pendingTakeProfitPrice = newTakeProfit;
        }

        private void ManageTrailingStops(ICandleMessage candle)
        {
                if (Position == 0m || StopLossPips <= 0m)
                        return;

                if (_stopLossOrder == null || _stopLossOrder.State != OrderStates.Active)
                        return;

                var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;
                var volume = Math.Abs(Position);

                if (volume <= 0m)
                        return;

                var step = Security?.PriceStep ?? _pipSize;

                if (Position > 0m)
                {
                        var desiredStop = NormalizePrice(bid - StopLossPips * _pipSize);
                        if (desiredStop <= 0m)
                                return;

                        if (desiredStop <= _stopLossOrder.Price + step / 2m)
                                return;

                        CancelStopOrder();
                        _stopLossOrder = SellStop(volume, desiredStop);
                }
                else if (Position < 0m)
                {
                        var desiredStop = NormalizePrice(bid + StopLossPips * _pipSize);
                        if (desiredStop <= 0m)
                                return;

                        if (desiredStop >= _stopLossOrder.Price - step / 2m)
                                return;

                        CancelStopOrder();
                        _stopLossOrder = BuyStop(volume, desiredStop);
                }
        }

        private void CancelPendingOrder()
        {
                if (_pendingOrder != null && _pendingOrder.State == OrderStates.Active)
                        CancelOrder(_pendingOrder);

                _pendingOrder = null;
                _pendingStopLossPrice = null;
                _pendingTakeProfitPrice = null;
        }

        private void CancelProtectionOrders()
        {
                CancelStopOrder();

                if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
                        CancelOrder(_takeProfitOrder);

                _takeProfitOrder = null;
        }

        private void CancelStopOrder()
        {
                if (_stopLossOrder != null && _stopLossOrder.State == OrderStates.Active)
                        CancelOrder(_stopLossOrder);

                _stopLossOrder = null;
        }

        private decimal CalculateVolume()
        {
                if (!UseRiskPercent)
                        return FixedVolume;

                if (Portfolio == null)
                        return FixedVolume;

                var balance = Portfolio.CurrentValue ?? Portfolio.BeginValue;
                if (balance == null || balance.Value <= 0m)
                        return FixedVolume;

                if (RiskPercent <= 0m)
                        return FixedVolume;

                if (StopLossPips <= 0m)
                        return FixedVolume;

                var stepPrice = Security?.StepPrice;
                var priceStep = Security?.PriceStep;
                if (stepPrice == null || stepPrice.Value <= 0m || priceStep == null || priceStep.Value <= 0m)
                        return FixedVolume;

                var pipValue = stepPrice.Value * (_pipSize / priceStep.Value);
                if (pipValue <= 0m)
                        return FixedVolume;

                var riskAmount = balance.Value * RiskPercent / 100m;
                if (riskAmount <= 0m)
                        return FixedVolume;

                var stopAmount = StopLossPips * pipValue;
                if (stopAmount <= 0m)
                        return FixedVolume;

                var rawVolume = riskAmount / stopAmount;
                if (rawVolume <= 0m)
                        return FixedVolume;

                var lotStep = FixedVolume;
                if (lotStep <= 0m)
                        lotStep = 0.01m;

                var multiplier = Math.Floor((double)(rawVolume / lotStep));
                if (multiplier < 1d)
                        multiplier = 1d;

                var volume = lotStep * (decimal)multiplier;

                var minVolume = Security?.MinVolume ?? volume;
                var maxVolume = Security?.MaxVolume ?? volume;

                volume = Math.Max(volume, minVolume);
                volume = Math.Min(volume, maxVolume);

                return volume;
        }

        private decimal? CalculateStopLossPrice(TradeDirection direction, decimal entryPrice)
        {
                if (StopLossPips <= 0m)
                        return null;

                return direction == TradeDirection.BuyStop
                        ? NormalizePrice(entryPrice - StopLossPips * _pipSize)
                        : NormalizePrice(entryPrice + StopLossPips * _pipSize);
        }

        private decimal? CalculateTakeProfitPrice(TradeDirection direction, decimal entryPrice)
        {
                if (TakeProfitPips <= 0m)
                        return null;

                return direction == TradeDirection.BuyStop
                        ? NormalizePrice(entryPrice + TakeProfitPips * _pipSize)
                        : NormalizePrice(entryPrice - TakeProfitPips * _pipSize);
        }

        private bool IsWithinTradingWindow(DateTimeOffset time)
        {
                if (!UseTimeFilter)
                        return true;

                var start = new TimeSpan(StartHour, StartMinute, 0);
                var end = new TimeSpan(EndHour, EndMinute, 0);
                var current = time.LocalDateTime.TimeOfDay;

                if (start <= end)
                        return current >= start && current < end;

                return current >= start || current < end;
        }

        private bool IsSpreadAcceptable()
        {
                if (MaxSpreadPoints <= 0m)
                        return true;

                if (_bestAsk <= 0m || _bestBid <= 0m)
                        return true;

                var spread = _bestAsk - _bestBid;
                if (spread <= 0m)
                        return true;

                var priceStep = Security?.PriceStep ?? _pipSize;
                if (priceStep <= 0m)
                        return true;

                var spreadPoints = spread / priceStep;
                return spreadPoints <= MaxSpreadPoints;
        }

        private decimal NormalizePrice(decimal price)
        {
                var step = Security?.PriceStep;
                if (step == null || step.Value <= 0m)
                        return price;

                var ratio = Math.Round(price / step.Value, MidpointRounding.AwayFromZero);
                return ratio * step.Value;
        }

        private decimal CalculatePipSize()
        {
                var step = Security?.PriceStep ?? 0.0001m;
                if (step <= 0m)
                        return 0.0001m;

                var digits = CountDecimals(step);
                return digits % 2 == 1 ? step * 10m : step;
        }

        private static int CountDecimals(decimal value)
        {
                value = Math.Abs(value);
                var count = 0;

                while (value != Math.Truncate(value) && count < 10)
                {
                        value *= 10m;
                        count++;
                }

                return count;
        }
}
