import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


# InitialDirections enum
DIRECTION_NONE = 0
DIRECTION_LONG = 1
DIRECTION_SHORT = 2

FAST_LEN = 10
SLOW_LEN = 30


class trailing_stop_manager_strategy(Strategy):
    def __init__(self):
        super(trailing_stop_manager_strategy, self).__init__()

        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._start_direction = self.Param("StartDirection", DIRECTION_NONE)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._entry_price = 0.0
        self._trailing_stop_price = 0.0
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE
        self._price_step = 1.0
        self._closes = []
        self._prev_fast = None
        self._prev_slow = None

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @TrailingStopPips.setter
    def TrailingStopPips(self, value):
        self._trailing_stop_pips.Value = value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @TrailingStepPips.setter
    def TrailingStepPips(self, value):
        self._trailing_step_pips.Value = value

    @property
    def StartDirection(self):
        return self._start_direction.Value

    @StartDirection.setter
    def StartDirection(self, value):
        self._start_direction.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(trailing_stop_manager_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._trailing_stop_price = 0.0
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE
        self._closes = []
        self._prev_fast = None
        self._prev_slow = None

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if self._price_step <= 0.0:
            self._price_step = 1.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        self._closes.append(close)
        if len(self._closes) > SLOW_LEN + 10:
            self._closes.pop(0)

        if len(self._closes) < SLOW_LEN:
            return

        fast = sum(self._closes[-FAST_LEN:]) / FAST_LEN
        slow = sum(self._closes[-SLOW_LEN:]) / SLOW_LEN

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if self.Position == 0:
            if prev_fast is not None and prev_slow is not None:
                if prev_fast <= prev_slow and fast > slow:
                    self.BuyMarket()
                    self._entry_price = close
                    self._trailing_active = False
                    self._trailing_stop_price = 0.0
                    self._current_direction = DIRECTION_LONG
                elif prev_fast >= prev_slow and fast < slow:
                    self.SellMarket()
                    self._entry_price = close
                    self._trailing_active = False
                    self._trailing_stop_price = 0.0
                    self._current_direction = DIRECTION_SHORT
            return

        price = close

        if self.Position > 0 and self._current_direction == DIRECTION_LONG:
            self._update_long_trailing(price)
        elif self.Position < 0 and self._current_direction == DIRECTION_SHORT:
            self._update_short_trailing(price)

    def _update_long_trailing(self, price):
        stop_distance = float(self.TrailingStopPips) * self._price_step
        if stop_distance <= 0.0:
            return

        step_distance = float(self.TrailingStepPips) * self._price_step

        if not self._trailing_active:
            if price - self._entry_price >= stop_distance:
                self._trailing_active = True
                self._trailing_stop_price = self._entry_price
        else:
            desired_stop = price - stop_distance
            if step_distance <= 0.0:
                if desired_stop > self._trailing_stop_price:
                    self._trailing_stop_price = desired_stop
            else:
                if desired_stop - self._trailing_stop_price >= step_distance:
                    self._trailing_stop_price = desired_stop

        if self._trailing_active and price <= self._trailing_stop_price:
            if self.Position > 0:
                self.SellMarket()
                self._reset_trailing()

    def _update_short_trailing(self, price):
        stop_distance = float(self.TrailingStopPips) * self._price_step
        if stop_distance <= 0.0:
            return

        step_distance = float(self.TrailingStepPips) * self._price_step

        if not self._trailing_active:
            if self._entry_price - price >= stop_distance:
                self._trailing_active = True
                self._trailing_stop_price = self._entry_price
        else:
            desired_stop = price + stop_distance
            if step_distance <= 0.0:
                if desired_stop < self._trailing_stop_price or self._trailing_stop_price == 0.0:
                    self._trailing_stop_price = desired_stop
            else:
                if self._trailing_stop_price - desired_stop >= step_distance:
                    self._trailing_stop_price = desired_stop

        if self._trailing_active and price >= self._trailing_stop_price:
            if self.Position < 0:
                self.BuyMarket()
                self._reset_trailing()

    def _reset_trailing(self):
        self._entry_price = 0.0
        self._trailing_stop_price = 0.0
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE

    def OnReseted(self):
        super(trailing_stop_manager_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._trailing_stop_price = 0.0
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE
        self._price_step = 1.0
        self._closes = []
        self._prev_fast = None
        self._prev_slow = None

    def CreateClone(self):
        return trailing_stop_manager_strategy()
