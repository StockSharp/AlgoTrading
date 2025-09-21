using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending order strategy driven by the DeMarker oscillator.
/// Places stop or limit orders when the indicator crosses configured bands
/// and manages protective stops, take profit and trailing logic after fills.
/// </summary>
public class DeMarkerPendingStrategy : Strategy
{
        private readonly StrategyParam<decimal> _tradeVolume;
        private readonly StrategyParam<int> _deMarkerPeriod;
        private readonly StrategyParam<decimal> _deMarkerUpperLevel;
        private readonly StrategyParam<decimal> _deMarkerLowerLevel;
        private readonly StrategyParam<decimal> _stopLossPoints;
        private readonly StrategyParam<decimal> _takeProfitPoints;
        private readonly StrategyParam<decimal> _trailingActivationPoints;
        private readonly StrategyParam<decimal> _trailingStopPoints;
        private readonly StrategyParam<decimal> _trailingStepPoints;
        private readonly StrategyParam<decimal> _pendingIndentPoints;
        private readonly StrategyParam<decimal> _pendingMaxSpreadPoints;
        private readonly StrategyParam<bool> _pendingOnlyOne;
        private readonly StrategyParam<bool> _pendingClosePrevious;
        private readonly StrategyParam<TimeSpan> _pendingExpiration;
        private readonly StrategyParam<PendingEntryMode> _entryMode;
        private readonly StrategyParam<bool> _useTimeFilter;
        private readonly StrategyParam<TimeSpan> _sessionStart;
        private readonly StrategyParam<TimeSpan> _sessionEnd;
        private readonly StrategyParam<decimal> _targetProfit;
        private readonly StrategyParam<DataType> _candleType;

        private decimal _priceStep;
        private decimal? _bestBid;
        private decimal? _bestAsk;
        private Order? _pendingOrder;
        private bool _pendingIsBuy;
        private decimal? _entryPrice;
        private decimal? _stopPrice;
        private decimal? _takePrice;
        private decimal? _lastTrailingPriceLong;
        private decimal? _lastTrailingPriceShort;
        private bool _targetReached;

        /// <summary>
        /// Trade volume used for new orders.
        /// </summary>
        public decimal TradeVolume
        {
                get => _tradeVolume.Value;
                set => _tradeVolume.Value = value;
        }

        /// <summary>
        /// DeMarker period.
        /// </summary>
        public int DeMarkerPeriod
        {
                get => _deMarkerPeriod.Value;
                set => _deMarkerPeriod.Value = value;
        }

        /// <summary>
        /// Upper DeMarker threshold for short signals.
        /// </summary>
        public decimal DeMarkerUpperLevel
        {
                get => _deMarkerUpperLevel.Value;
                set => _deMarkerUpperLevel.Value = value;
        }

        /// <summary>
        /// Lower DeMarker threshold for long signals.
        /// </summary>
        public decimal DeMarkerLowerLevel
        {
                get => _deMarkerLowerLevel.Value;
                set => _deMarkerLowerLevel.Value = value;
        }

        /// <summary>
        /// Stop loss distance expressed in price steps.
        /// </summary>
        public decimal StopLossPoints
        {
                get => _stopLossPoints.Value;
                set => _stopLossPoints.Value = value;
        }

        /// <summary>
        /// Take profit distance expressed in price steps.
        /// </summary>
        public decimal TakeProfitPoints
        {
                get => _takeProfitPoints.Value;
                set => _takeProfitPoints.Value = value;
        }

        /// <summary>
        /// Activation distance for the trailing stop in price steps.
        /// </summary>
        public decimal TrailingActivationPoints
        {
                get => _trailingActivationPoints.Value;
                set => _trailingActivationPoints.Value = value;
        }

        /// <summary>
        /// Trailing stop distance expressed in price steps.
        /// </summary>
        public decimal TrailingStopPoints
        {
                get => _trailingStopPoints.Value;
                set => _trailingStopPoints.Value = value;
        }

        /// <summary>
        /// Additional progress in price steps required before moving the trailing stop.
        /// </summary>
        public decimal TrailingStepPoints
        {
                get => _trailingStepPoints.Value;
                set => _trailingStepPoints.Value = value;
        }

        /// <summary>
        /// Pending order indent in price steps.
        /// </summary>
        public decimal PendingIndentPoints
        {
                get => _pendingIndentPoints.Value;
                set => _pendingIndentPoints.Value = value;
        }

        /// <summary>
        /// Maximum allowed spread for placing pending orders expressed in price steps.
        /// </summary>
        public decimal PendingMaxSpreadPoints
        {
                get => _pendingMaxSpreadPoints.Value;
                set => _pendingMaxSpreadPoints.Value = value;
        }

        /// <summary>
        /// Allow only a single pending order at a time.
        /// </summary>
        public bool PendingOnlyOne
        {
                get => _pendingOnlyOne.Value;
                set => _pendingOnlyOne.Value = value;
        }

        /// <summary>
        /// Cancel previous pending orders before placing a new one.
        /// </summary>
        public bool PendingClosePrevious
        {
                get => _pendingClosePrevious.Value;
                set => _pendingClosePrevious.Value = value;
        }

        /// <summary>
        /// Pending order expiration interval. Zero disables expiration.
        /// </summary>
        public TimeSpan PendingExpiration
        {
                get => _pendingExpiration.Value;
                set => _pendingExpiration.Value = value;
        }

        /// <summary>
        /// Selects between stop or limit entries.
        /// </summary>
        public PendingEntryMode EntryMode
        {
                get => _entryMode.Value;
                set => _entryMode.Value = value;
        }

        /// <summary>
        /// Enables trading session filtering.
        /// </summary>
        public bool UseTimeFilter
        {
                get => _useTimeFilter.Value;
                set => _useTimeFilter.Value = value;
        }

        /// <summary>
        /// Session start time (inclusive).
        /// </summary>
        public TimeSpan SessionStart
        {
                get => _sessionStart.Value;
                set => _sessionStart.Value = value;
        }

        /// <summary>
        /// Session end time (exclusive).
        /// </summary>
        public TimeSpan SessionEnd
        {
                get => _sessionEnd.Value;
                set => _sessionEnd.Value = value;
        }

        /// <summary>
        /// Target profit in account currency. Zero disables the target.
        /// </summary>
        public decimal TargetProfit
        {
                get => _targetProfit.Value;
                set => _targetProfit.Value = value;
        }

        /// <summary>
        /// Candle data type used for indicator calculations.
        /// </summary>
        public DataType CandleType
        {
                get => _candleType.Value;
                set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes strategy parameters with sensible defaults.
        /// </summary>
        public DeMarkerPendingStrategy()
        {
                _tradeVolume = Param(nameof(TradeVolume), 0.1m)
                        .SetGreaterThanZero()
                        .SetDisplay("Trade Volume", "Volume used for each pending order", "Risk");

                _deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
                        .SetGreaterThanZero()
                        .SetDisplay("DeMarker Period", "Averaging length for the DeMarker oscillator", "Indicator");

                _deMarkerUpperLevel = Param(nameof(DeMarkerUpperLevel), 0.7m)
                        .SetDisplay("Upper Level", "Short signals when DeMarker rises above this level", "Indicator");

                _deMarkerLowerLevel = Param(nameof(DeMarkerLowerLevel), 0.3m)
                        .SetDisplay("Lower Level", "Long signals when DeMarker falls below this level", "Indicator");

                _stopLossPoints = Param(nameof(StopLossPoints), 150m)
                        .SetNotNegative()
                        .SetDisplay("Stop Loss (steps)", "Protective stop distance in price steps", "Risk");

                _takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
                        .SetNotNegative()
                        .SetDisplay("Take Profit (steps)", "Target distance in price steps", "Risk");

                _trailingActivationPoints = Param(nameof(TrailingActivationPoints), 70m)
                        .SetNotNegative()
                        .SetDisplay("Trailing Activation", "Profit distance before trailing activates (steps)", "Risk");

                _trailingStopPoints = Param(nameof(TrailingStopPoints), 250m)
                        .SetNotNegative()
                        .SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Risk");

                _trailingStepPoints = Param(nameof(TrailingStepPoints), 50m)
                        .SetNotNegative()
                        .SetDisplay("Trailing Step", "Extra price steps before trailing moves again", "Risk");

                _pendingIndentPoints = Param(nameof(PendingIndentPoints), 5m)
                        .SetNotNegative()
                        .SetDisplay("Pending Indent", "Distance from reference price for pending orders (steps)", "Execution");

                _pendingMaxSpreadPoints = Param(nameof(PendingMaxSpreadPoints), 12m)
                        .SetNotNegative()
                        .SetDisplay("Max Spread", "Maximum allowed spread for placing orders (steps)", "Execution");

                _pendingOnlyOne = Param(nameof(PendingOnlyOne), false)
                        .SetDisplay("Only One Pending", "Allow only a single pending order", "Execution");

                _pendingClosePrevious = Param(nameof(PendingClosePrevious), false)
                        .SetDisplay("Close Previous", "Cancel older pending orders before placing a new one", "Execution");

                _pendingExpiration = Param(nameof(PendingExpiration), TimeSpan.FromMinutes(10))
                        .SetDisplay("Expiration", "Lifetime of pending orders (zero disables)", "Execution");

                _entryMode = Param(nameof(EntryMode), PendingEntryMode.Stop)
                        .SetDisplay("Entry Mode", "Choose stop or limit pending orders", "Execution");

                _useTimeFilter = Param(nameof(UseTimeFilter), false)
                        .SetDisplay("Use Time Filter", "Enable session time filter", "Session");

                _sessionStart = Param(nameof(SessionStart), new TimeSpan(10, 0, 0))
                        .SetDisplay("Session Start", "Inclusive trading session start", "Session");

                _sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 0, 0))
                        .SetDisplay("Session End", "Exclusive trading session end", "Session");

                _targetProfit = Param(nameof(TargetProfit), 0m)
                        .SetNotNegative()
                        .SetDisplay("Target Profit", "Close everything after reaching this profit", "Risk");

                _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                        .SetDisplay("Candle Type", "Timeframe used for DeMarker calculations", "General");
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

                _priceStep = 0m;
                _bestBid = null;
                _bestAsk = null;
                _pendingOrder = null;
                _pendingIsBuy = false;
                _entryPrice = null;
                _stopPrice = null;
                _takePrice = null;
                _lastTrailingPriceLong = null;
                _lastTrailingPriceShort = null;
                _targetReached = false;
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                _priceStep = Security?.PriceStep ?? 0m;
                if (_priceStep <= 0m)
                        _priceStep = 0.0001m;

                var deMarker = new DeMarker { Length = DeMarkerPeriod };

                var candleSubscription = SubscribeCandles(CandleType);
                candleSubscription
                        .Bind(deMarker, ProcessCandle)
                        .Start();

                SubscribeLevel1()
                        .Bind(ProcessLevel1)
                        .Start();

                StartProtection();
        }

        private void ProcessLevel1(Level1ChangeMessage message)
        {
                if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
                {
                        var bid = (decimal)bidValue;
                        if (bid > 0m)
                                _bestBid = bid;
                }

                if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
                {
                        var ask = (decimal)askValue;
                        if (ask > 0m)
                                _bestAsk = ask;
                }

                if (TargetProfit > 0m && !_targetReached && PnL >= TargetProfit)
                {
                        LogInfo("Target profit reached. Closing all positions and cancelling pending orders.");
                        CloseAllPositions();
                        CancelPendingOrder();
                        _targetReached = true;
                        return;
                }

                ManageOpenPosition();
        }

        private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                if (!IsFormedAndOnlineAndAllowTrading())
                        return;

                if (!IsWithinSession(candle.OpenTime))
                {
                        CancelPendingOrder();
                        return;
                }

                if (TargetProfit > 0m && !_targetReached && PnL >= TargetProfit)
                {
                        LogInfo("Target profit reached on candle. Closing all positions and cancelling pending orders.");
                        CloseAllPositions();
                        CancelPendingOrder();
                        _targetReached = true;
                        return;
                }

                var ask = _bestAsk ?? candle.ClosePrice;
                var bid = _bestBid ?? candle.ClosePrice;

                if (ask <= 0m || bid <= 0m)
                        return;

                var spread = Math.Max(ask - bid, 0m);
                var maxSpread = PendingMaxSpreadPoints * _priceStep;
                if (maxSpread > 0m && spread > maxSpread)
                {
                        LogInfo($"Skip signal because spread {spread:F5} exceeds limit {maxSpread:F5}.");
                        return;
                }

                if (PendingOnlyOne && HasActivePendingOrder())
                        return;

                if (deMarkerValue <= DeMarkerLowerLevel)
                        PlacePendingOrder(true, ask, candle.CloseTime);
                else if (deMarkerValue >= DeMarkerUpperLevel)
                        PlacePendingOrder(false, bid, candle.CloseTime);
        }

        private void PlacePendingOrder(bool isBuy, decimal referencePrice, DateTimeOffset candleTime)
        {
                if (TradeVolume <= 0m)
                        return;

                if (PendingClosePrevious)
                        CancelPendingOrder();

                if (PendingOnlyOne && HasActivePendingOrder())
                        return;

                var indent = PendingIndentPoints * _priceStep;
                if (indent < 0m)
                        indent = 0m;

                decimal price;

                if (EntryMode == PendingEntryMode.Stop)
                        price = isBuy ? referencePrice + indent : referencePrice - indent;
                else
                        price = isBuy ? referencePrice - indent : referencePrice + indent;

                price = AlignPrice(price);

                if (price <= 0m)
                        return;

                var expiration = PendingExpiration > TimeSpan.Zero
                        ? (DateTimeOffset?) (candleTime + PendingExpiration)
                        : null;

                Order? order = null;

                if (EntryMode == PendingEntryMode.Stop)
                        order = isBuy ? BuyStop(TradeVolume, price, expiration: expiration)
                                      : SellStop(TradeVolume, price, expiration: expiration);
                else
                        order = isBuy ? BuyLimit(TradeVolume, price, expiration: expiration)
                                       : SellLimit(TradeVolume, price, expiration: expiration);

                if (order != null)
                {
                        _pendingOrder = order;
                        _pendingIsBuy = isBuy;
                }
        }

        private decimal AlignPrice(decimal price)
        {
                if (_priceStep <= 0m)
                        return price;

                var steps = Math.Round(price / _priceStep);
                return steps * _priceStep;
        }

        private bool HasActivePendingOrder()
        {
                return _pendingOrder is { State: OrderStates.Active or OrderStates.Pending };
        }

        private void CancelPendingOrder()
        {
                if (_pendingOrder == null)
                        return;

                if (_pendingOrder.State == OrderStates.Active)
                        CancelOrder(_pendingOrder);

                _pendingOrder = null;
        }

        private bool IsWithinSession(DateTimeOffset time)
        {
                if (!UseTimeFilter)
                        return true;

                var start = SessionStart;
                var end = SessionEnd;
                var current = time.TimeOfDay;

                if (start == end)
                        return true;

                if (start < end)
                        return current >= start && current < end;

                return current >= start || current < end;
        }

        /// <inheritdoc />
        protected override void OnOrderChanged(Order order)
        {
                base.OnOrderChanged(order);

                if (_pendingOrder != null && order == _pendingOrder)
                {
                        if (order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
                                _pendingOrder = null;
                }
        }

        /// <inheritdoc />
        protected override void OnOwnTradeReceived(MyTrade trade)
        {
                base.OnOwnTradeReceived(trade);

                if (trade.Trade == null || trade.Order == null)
                        return;

                if (_pendingOrder != null && trade.Order == _pendingOrder)
                {
                        InitializeProtection(_pendingIsBuy, trade.Trade.Price, trade.Trade.ServerTime);
                        _pendingOrder = null;
                }
        }

        private void InitializeProtection(bool isLong, decimal entryPrice, DateTimeOffset tradeTime)
        {
                _entryPrice = entryPrice;
                _lastTrailingPriceLong = null;
                _lastTrailingPriceShort = null;

                var stopDistance = StopLossPoints * _priceStep;
                var takeDistance = TakeProfitPoints * _priceStep;

                _stopPrice = stopDistance > 0m
                        ? entryPrice + (isLong ? -stopDistance : stopDistance)
                        : null;

                _takePrice = takeDistance > 0m
                        ? entryPrice + (isLong ? takeDistance : -takeDistance)
                        : null;

                if (isLong)
                        LogInfo($"Entered long position at {entryPrice:F5} on {tradeTime:u}.");
                else
                        LogInfo($"Entered short position at {entryPrice:F5} on {tradeTime:u}.");
        }

        private void ManageOpenPosition()
        {
                if (Position == 0)
                        return;

                if (_bestBid == null && _bestAsk == null)
                        return;

                var step = _priceStep <= 0m ? 0.0001m : _priceStep;

                if (Position > 0)
                {
                        if (_bestBid is not decimal bid)
                                return;

                        if (_stopPrice is decimal stop && bid <= stop)
                        {
                                SellMarket(Position);
                                return;
                        }

                        if (_takePrice is decimal take && bid >= take)
                        {
                                SellMarket(Position);
                                return;
                        }

                        ApplyTrailingForLong(bid, step);
                }
                else if (Position < 0)
                {
                        if (_bestAsk is not decimal ask)
                                return;

                        var volume = Math.Abs(Position);

                        if (_stopPrice is decimal stop && ask >= stop)
                        {
                                BuyMarket(volume);
                                return;
                        }

                        if (_takePrice is decimal take && ask <= take)
                        {
                                BuyMarket(volume);
                                return;
                        }

                        ApplyTrailingForShort(ask, step);
                }
        }

        private void ApplyTrailingForLong(decimal currentBid, decimal step)
        {
                if (_entryPrice is not decimal entry)
                        return;

                if (TrailingStopPoints <= 0m)
                        return;

                var activationDistance = TrailingActivationPoints * step;
                if (activationDistance > 0m && currentBid - entry < activationDistance)
                        return;

                var trailingDistance = TrailingStopPoints * step;
                if (trailingDistance <= 0m)
                        return;

                if (TrailingStepPoints > 0m)
                {
                        var stepDistance = TrailingStepPoints * step;
                        if (_lastTrailingPriceLong is decimal lastPrice && currentBid - lastPrice < stepDistance)
                                return;

                        _lastTrailingPriceLong = currentBid;
                }

                var desiredStop = currentBid - trailingDistance;
                if (_stopPrice is null || desiredStop > _stopPrice.Value)
                        _stopPrice = desiredStop;
        }

        private void ApplyTrailingForShort(decimal currentAsk, decimal step)
        {
                if (_entryPrice is not decimal entry)
                        return;

                if (TrailingStopPoints <= 0m)
                        return;

                var activationDistance = TrailingActivationPoints * step;
                if (activationDistance > 0m && entry - currentAsk < activationDistance)
                        return;

                var trailingDistance = TrailingStopPoints * step;
                if (trailingDistance <= 0m)
                        return;

                if (TrailingStepPoints > 0m)
                {
                        var stepDistance = TrailingStepPoints * step;
                        if (_lastTrailingPriceShort is decimal lastPrice && lastPrice - currentAsk < stepDistance)
                                return;

                        _lastTrailingPriceShort = currentAsk;
                }

                var desiredStop = currentAsk + trailingDistance;
                if (_stopPrice is null || desiredStop < _stopPrice.Value)
                        _stopPrice = desiredStop;
        }

        /// <inheritdoc />
        protected override void OnPositionChanged(decimal delta)
        {
                base.OnPositionChanged(delta);

                if (Position == 0)
                {
                        _entryPrice = null;
                        _stopPrice = null;
                        _takePrice = null;
                        _lastTrailingPriceLong = null;
                        _lastTrailingPriceShort = null;
                }
        }
}

/// <summary>
/// Entry mode for pending orders.
/// </summary>
public enum PendingEntryMode
{
        /// <summary>
        /// Use stop orders to enter in the direction of the breakout.
        /// </summary>
        Stop,

        /// <summary>
        /// Use limit orders to enter on pullbacks.
        /// </summary>
        Limit
}
