import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class doji_arrows_strategy(Strategy):
    def __init__(self):
        super(doji_arrows_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 30.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 90.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 15.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 5.0)
        self._doji_body_points = self.Param("DojiBodyPoints", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._has_previous_candle = False
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0

        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(doji_arrows_strategy, self).OnStarted(time)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_active_position(candle)

        if not self._has_previous_candle:
            self._cache_previous_candle(candle)
            return

        step = self._get_price_step()
        tolerance = step if self._doji_body_points.Value <= 0 else self._doji_body_points.Value * step
        body_size = abs(self._prev_open - self._prev_close)
        is_doji = body_size <= tolerance

        breakout_up = is_doji and float(candle.ClosePrice) > self._prev_high
        breakout_down = is_doji and float(candle.ClosePrice) < self._prev_low

        if breakout_up and self.Position == 0:
            self.BuyMarket()
            self._initialize_protection(float(candle.ClosePrice), True, step)
        elif breakout_down and self.Position == 0:
            self.SellMarket()
            self._initialize_protection(float(candle.ClosePrice), False, step)

        self._cache_previous_candle(candle)

    def _manage_active_position(self, candle):
        if self.Position == 0:
            return

        step = self._get_price_step()
        trailing_distance = self._trailing_stop_points.Value * step if self._trailing_stop_points.Value > 0 else 0.0
        trailing_step = self._trailing_step_points.Value * step if self._trailing_step_points.Value > 0 else 0.0

        if self.Position > 0:
            if trailing_distance > 0 and self._entry_price is not None:
                gain = float(candle.ClosePrice) - self._entry_price
                if gain > trailing_distance + trailing_step:
                    new_stop = float(candle.ClosePrice) - trailing_distance
                    if self._stop_price is None or new_stop > self._stop_price:
                        self._stop_price = new_stop

            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(abs(self.Position))
                self._reset_protection()
                return

            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket(abs(self.Position))
                self._reset_protection()
                return

        elif self.Position < 0:
            if trailing_distance > 0 and self._entry_price is not None:
                gain = self._entry_price - float(candle.ClosePrice)
                if gain > trailing_distance + trailing_step:
                    new_stop = float(candle.ClosePrice) + trailing_distance
                    if self._stop_price is None or new_stop < self._stop_price:
                        self._stop_price = new_stop

            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_protection()
                return

            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket(abs(self.Position))
                self._reset_protection()
                return

    def _initialize_protection(self, price, is_long, step):
        self._entry_price = price

        if self._stop_loss_points.Value > 0:
            offset = self._stop_loss_points.Value * step
            self._stop_price = price - offset if is_long else price + offset
        else:
            self._stop_price = None

        if self._take_profit_points.Value > 0:
            offset = self._take_profit_points.Value * step
            self._take_price = price + offset if is_long else price - offset
        else:
            self._take_price = None

    def _reset_protection(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def _cache_previous_candle(self, candle):
        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._has_previous_candle = True

    def _get_price_step(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        return step if step > 0 else 1.0

    def OnReseted(self):
        super(doji_arrows_strategy, self).OnReseted()
        self._has_previous_candle = False
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._reset_protection()

    def CreateClone(self):
        return doji_arrows_strategy()
