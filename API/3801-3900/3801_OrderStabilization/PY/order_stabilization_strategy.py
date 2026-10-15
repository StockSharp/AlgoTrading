import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.MatchingEngine")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes, OrderStates, ITickTradeMessage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.MatchingEngine import StopOrderCondition

# ExecutionMessage implements tick Price/Volume explicitly; access the declared interface.
_tick_price = clr.GetClrType(ITickTradeMessage).GetProperty("Price")
_tick_volume = clr.GetClrType(ITickTradeMessage).GetProperty("Volume")


class order_stabilization_strategy(Strategy):
    def __init__(self):
        super(order_stabilization_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Trade volume in lots used for both stop entries", "Trading")
        self._order_distance = self.Param("OrderDistancePoints", 20.0).SetGreaterThanZero() \
            .SetDisplay("Order Distance", "Distance between the current close price and each stop order, in points", "Trading")
        self._profit_threshold = self.Param("ProfitThreshold", -2.0) \
            .SetDisplay("Profit Threshold", "Minimum floating profit (account currency) required before an exit triggered by stabilization is allowed", "Exit")
        self._absolute_fixation = self.Param("AbsoluteFixation", 30.0).SetNotNegative() \
            .SetDisplay("Absolute Fixation", "Profit level (account currency) that forces an immediate exit", "Exit")
        self._stabilization_points = self.Param("StabilizationPoints", 25.0).SetGreaterThanZero() \
            .SetDisplay("Stabilization", "Maximum candle body size (points) that signals a flat market", "Exit")
        self._expiration_minutes = self.Param("ExpirationMinutes", 20).SetNotNegative() \
            .SetDisplay("Expiration", "Lifetime of pending stop orders in minutes, 0 disables expiration", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type used to evaluate stabilization", "General")

        self._buy_stop = None
        self._sell_stop = None
        self._created_at = None
        self._filled_position = Decimal.Zero
        self._entry_value = Decimal.Zero
        self._previous_body = Decimal.Zero
        self._has_previous_body = False

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Ticks)]

    def OnReseted(self):
        super(order_stabilization_strategy, self).OnReseted()
        self._buy_stop = None
        self._sell_stop = None
        self._created_at = None
        self._filled_position = Decimal.Zero
        self._entry_value = Decimal.Zero
        self._previous_body = Decimal.Zero
        self._has_previous_body = False

    def OnStarted2(self, time):
        super(order_stabilization_strategy, self).OnStarted2(time)
        # Stop orders are matched against trade prints, so they fire between candle closes.
        self.Subscribe(Subscription(DataType.Ticks, self.Security))
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        point = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal.One
        if point <= 0:
            point = Decimal.One

        body = Math.Abs(candle.ClosePrice - candle.OpenPrice)

        if self.Position != 0 and self._is_exit_due(candle.ClosePrice, body, point):
            self._cancel_if_active(self._buy_stop)
            self._cancel_if_active(self._sell_stop)
            self._flatten()
        elif (self.Position == 0 and not self._is_working(self._buy_stop) and not self._is_working(self._sell_stop)) or \
                self._is_expired(candle.OpenTime):
            self._arm_stops(candle.ClosePrice, candle.OpenTime, point)

        self._previous_body = body
        self._has_previous_body = True

    def OnOwnTradeReceived(self, trade):
        super(order_stabilization_strategy, self).OnOwnTradeReceived(trade)

        price = _tick_price.GetValue(trade.Trade)
        volume = _tick_volume.GetValue(trade.Trade)
        signed = volume if trade.Order.Side == Sides.Buy else -volume
        previous = self._filled_position

        self._filled_position += signed

        if self._filled_position == 0:
            self._entry_value = Decimal.Zero
        elif previous == 0 or Math.Sign(previous) == Math.Sign(signed):
            self._entry_value += price * volume
        elif Math.Sign(self._filled_position) != Math.Sign(previous):
            self._entry_value = price * Math.Abs(self._filled_position)
        else:
            self._entry_value -= self._entry_value / Math.Abs(previous) * volume

    def _is_exit_due(self, price, body, point):
        body_limit = Decimal(self._stabilization_points.Value) * point
        pnl = self._floating_pnl(price, point)
        one_small = body <= body_limit
        two_small = one_small and self._has_previous_body and self._previous_body <= body_limit
        absolute_fixation = Decimal(self._absolute_fixation.Value)

        return (one_small and pnl > Decimal(self._profit_threshold.Value)) or \
            two_small or \
            (absolute_fixation > 0 and pnl >= absolute_fixation)

    def _is_expired(self, time):
        minutes = int(self._expiration_minutes.Value)
        return minutes > 0 and \
            self._created_at is not None and \
            time - self._created_at >= TimeSpan.FromMinutes(minutes)

    # Flat: both stops are placed around the close. With a position open only the pending
    # opposite stop is moved; the side that opened the position is never placed again.
    def _arm_stops(self, center, time, point):
        distance = Decimal(self._order_distance.Value) * point

        if self.Position == 0 or (self.Position < 0 and self._is_working(self._buy_stop)):
            self._buy_stop = self._renew_stop(self._buy_stop, Sides.Buy, center + distance)

        if self.Position == 0 or (self.Position > 0 and self._is_working(self._sell_stop)):
            self._sell_stop = self._renew_stop(self._sell_stop, Sides.Sell, center - distance)

        self._created_at = time if self._is_working(self._buy_stop) or self._is_working(self._sell_stop) else None

    def _renew_stop(self, order, side, activation_price):
        if order is not None and order.State == OrderStates.Active:
            self.CancelOrder(order)
        elif self._is_working(order):
            return order

        stop = Order()
        stop.Security = self.Security
        stop.Portfolio = self.Portfolio
        stop.Side = side
        stop.Volume = Decimal(self._order_volume.Value)
        stop.Type = OrderTypes.Conditional
        condition = StopOrderCondition()
        condition.ActivationPrice = activation_price
        stop.Condition = condition

        self.RegisterOrder(stop)
        return stop

    def _is_working(self, order):
        return order is not None and order.State != OrderStates.Done and order.State != OrderStates.Failed

    def _cancel_if_active(self, order):
        if order is not None and order.State == OrderStates.Active:
            self.CancelOrder(order)

    def _floating_pnl(self, price, point):
        if self.Position == 0 or self._filled_position == 0:
            return Decimal.Zero

        entry_price = self._entry_value / Math.Abs(self._filled_position)
        direction = Decimal.One if self.Position > 0 else Decimal.MinusOne
        move = (price - entry_price) * direction
        step_price = self.Security.StepPrice if self.Security is not None and self.Security.StepPrice is not None else Decimal.Zero

        if step_price > 0:
            return move / point * step_price * Math.Abs(self.Position)

        multiplier = self.Security.Multiplier if self.Security is not None and self.Security.Multiplier is not None else Decimal.One
        return move * multiplier * Math.Abs(self.Position)

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return order_stabilization_strategy()
