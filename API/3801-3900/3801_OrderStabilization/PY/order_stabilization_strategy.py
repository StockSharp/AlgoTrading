import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class order_stabilization_strategy(Strategy):
    def __init__(self):
        super(order_stabilization_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero()
        self._order_distance = self.Param("OrderDistancePoints", 20.0).SetGreaterThanZero()
        self._profit_threshold = self.Param("ProfitThreshold", -2.0)
        self._absolute_fixation = self.Param("AbsoluteFixation", 30.0).SetNotNegative()
        self._stabilization_points = self.Param("StabilizationPoints", 25.0).SetGreaterThanZero()
        self._expiration_minutes = self.Param("ExpirationMinutes", 20).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._buy_stop = None
        self._sell_stop = None
        self._created_at = None
        self._entry_price = 0.0
        self._previous_body = 0.0
        self._has_previous_body = False

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(order_stabilization_strategy, self).OnReseted()
        self._clear_pending()
        self._entry_price = 0.0
        self._previous_body = 0.0
        self._has_previous_body = False

    def OnStarted2(self, time):
        super(order_stabilization_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        point = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if point <= 0:
            point = 1.0

        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        body_limit = float(self._stabilization_points.Value) * point

        if self.Position != 0:
            pnl = self._floating_pnl(float(candle.ClosePrice), point)
            one_small = body <= body_limit
            two_small = one_small and self._has_previous_body and self._previous_body <= body_limit

            if ((one_small and pnl > float(self._profit_threshold.Value)) or two_small or
                    (float(self._absolute_fixation.Value) > 0 and pnl >= float(self._absolute_fixation.Value))):
                self._flatten()
                self._clear_pending()
                self._previous_body = body
                self._has_previous_body = True
                return

        if (self._created_at is not None and int(self._expiration_minutes.Value) > 0 and
                (candle.OpenTime - self._created_at).TotalMinutes >= int(self._expiration_minutes.Value)):
            self._create_pending(float(candle.ClosePrice), candle.OpenTime, point)

        if self._buy_stop is None and self._sell_stop is None:
            self._create_pending(float(candle.ClosePrice), candle.OpenTime, point)

        hit_buy = self._buy_stop is not None and float(candle.HighPrice) >= self._buy_stop
        hit_sell = self._sell_stop is not None and float(candle.LowPrice) <= self._sell_stop

        if hit_buy or hit_sell:
            if hit_buy and hit_sell:
                side = Sides.Buy if float(candle.ClosePrice) >= float(candle.OpenPrice) else Sides.Sell
            else:
                side = Sides.Buy if hit_buy else Sides.Sell

            trigger = self._buy_stop if side == Sides.Buy else self._sell_stop
            volume = float(self._order_volume.Value)

            if side == Sides.Buy:
                self.BuyMarket(volume)
                self._buy_stop = None
            else:
                self.SellMarket(volume)
                self._sell_stop = None

            self._entry_price = trigger

        self._previous_body = body
        self._has_previous_body = True

    def _create_pending(self, center, time, point):
        distance = float(self._order_distance.Value) * point
        self._buy_stop = center + distance
        self._sell_stop = center - distance
        self._created_at = time

    def _floating_pnl(self, price, point):
        if self.Position == 0 or self._entry_price == 0:
            return 0.0
        direction = 1.0 if self.Position > 0 else -1.0
        move = (price - self._entry_price) * direction
        step_price = float(self.Security.StepPrice) if self.Security is not None and self.Security.StepPrice is not None else 0.0
        multiplier = float(self.Security.Multiplier) if self.Security is not None and self.Security.Multiplier is not None else 1.0
        return move / point * step_price * abs(float(self.Position)) if step_price > 0 else move * multiplier * abs(float(self.Position))

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
        self._entry_price = 0.0

    def _clear_pending(self):
        self._buy_stop = None
        self._sell_stop = None
        self._created_at = None

    def CreateClone(self):
        return order_stabilization_strategy()
