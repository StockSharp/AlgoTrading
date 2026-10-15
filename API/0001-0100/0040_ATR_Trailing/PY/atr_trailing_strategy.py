import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, OrderStates, OrderTypes, Sides, Level1Fields, ITickTradeMessage
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

# ExecutionMessage implements tick Price/Volume explicitly; access the declared interface.
_tick_price = clr.GetClrType(ITickTradeMessage).GetProperty("Price")
_tick_volume = clr.GetClrType(ITickTradeMessage).GetProperty("Volume")

class atr_trailing_strategy(Strategy):
    """
    Enters on price/SMA crossings; a crossing against the open position closes it and opens the crossing side.
    A fill-anchored finished-close/current-ATR ratchet exits at market on fresh executable quotes.
    """

    def __init__(self):
        super(atr_trailing_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop; zero disables it", "Risk")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average calculation for entry", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._previous_close = None
        self._previous_mean = Decimal.Zero
        self._trailing_stop_level = None
        self._entry_distance = Decimal.Zero
        self._entry_volume = Decimal.Zero
        self._entry_value = Decimal.Zero
        self._entry_side = Sides.Buy
        self._pending_order = None
        self.OrderRegistering += self._track_pending
        self.Trades.TradeAdded += self._observe_actual_fill

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _has_pending_order(self):
        return self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)

    def OnReseted(self):
        super(atr_trailing_strategy, self).OnReseted()
        self._previous_close = None
        self._previous_mean = Decimal.Zero
        self._trailing_stop_level = None
        self._entry_distance = Decimal.Zero
        self._entry_volume = Decimal.Zero
        self._entry_value = Decimal.Zero
        self._pending_order = None

    def OnStarted2(self, time):
        super(atr_trailing_strategy, self).OnStarted2(time)
        self._previous_close = None
        self._trailing_stop_level = None
        self._entry_volume = Decimal.Zero
        self._entry_value = Decimal.Zero
        self._pending_order = None
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_quote).Start()
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, sma, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _ratchet(self, candidate):
        if self._trailing_stop_level is None:
            self._trailing_stop_level = candidate
        elif self._entry_side == Sides.Buy:
            self._trailing_stop_level = Math.Max(self._trailing_stop_level, candidate)
        else:
            self._trailing_stop_level = Math.Min(self._trailing_stop_level, candidate)

    def _observe_actual_fill(self, trade):
        if self.Position == 0:
            self._trailing_stop_level = None
            self._entry_volume = Decimal.Zero
            self._entry_value = Decimal.Zero
        elif trade.Order.Side == self._entry_side and self._atr_multiplier.Value > 0:
            volume = _tick_volume.GetValue(trade.Trade)
            price = _tick_price.GetValue(trade.Trade)
            self._entry_volume += volume
            self._entry_value += price * volume
            actual_entry = self._entry_value / self._entry_volume
            self._ratchet(actual_entry - self._entry_distance if self._entry_side == Sides.Buy else actual_entry + self._entry_distance)

    def _process_quote(self, quote):
        if self._atr_multiplier.Value == 0 or self.Position == 0 or self._trailing_stop_level is None or self._has_pending_order() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Only a fresh executable-side price activates; do not reuse stale opposite-side quotes.
        field = Level1Fields.BestBidPrice if self.Position > 0 else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(field):
            return
        price = Decimal(quote.Changes[field])
        if price <= 0:
            return
        if (self.Position > 0 and price <= self._trailing_stop_level) or (self.Position < 0 and price >= self._trailing_stop_level):
            self._close_position("ATR trailing stop")

    def _close_position(self, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = Sides.Sell if self.Position > 0 else Sides.Buy
        order.Volume = Math.Abs(self.Position)
        order.Comment = comment
        self.RegisterOrder(order)

    def _process_candle(self, candle, atr_value, sma_value):
        if candle.State != CandleStates.Finished or not atr_value.Indicator.IsFormed or not sma_value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        atr = atr_value.GetValue[Decimal](None)
        mean = sma_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        downward_cross = self._previous_close is not None and self._previous_close >= self._previous_mean and close < mean
        upward_cross = self._previous_close is not None and self._previous_close <= self._previous_mean and close > mean
        self._previous_close = close
        self._previous_mean = mean
        multiplier = Decimal(self._atr_multiplier.Value)
        # While a reversal is unfilled the old side is still held; its close must not seed the new side's stop.
        if self.Position != 0 and (self.Position > 0) == (self._entry_side == Sides.Buy) and multiplier > 0:
            self._ratchet(close - atr * multiplier if self.Position > 0 else close + atr * multiplier)
        if self._has_pending_order():
            return
        if upward_cross or downward_cross:
            side = Sides.Buy if upward_cross else Sides.Sell
            # A crossing against the open position closes it and opens the crossing side in one order.
            if self.Position == 0 or (self.Position > 0) != (side == Sides.Buy):
                self._entry_side = side
                self._entry_distance = atr * multiplier
                self._entry_volume = Decimal.Zero
                self._entry_value = Decimal.Zero
                self._trailing_stop_level = None
                order = Order()
                order.Security = self.Security
                order.Portfolio = self.Portfolio
                order.Type = OrderTypes.Market
                order.Side = side
                order.Volume = self.Volume + Math.Abs(self.Position)
                order.Comment = "ATR trailing entry"
                self.RegisterOrder(order)
        # Tightened levels apply to future quote updates, never retrospectively to this bar's wick.

    def CreateClone(self):
        return atr_trailing_strategy()
