import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


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

        self._entry_price = Decimal(0)
        self._trailing_stop_price = Decimal(0)
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE
        self._price_step = Decimal(1)
        self._closes = []
        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(trailing_stop_manager_strategy, self).OnStarted2(time)

        self._entry_price = Decimal(0)
        self._trailing_stop_price = Decimal(0)
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE
        self._closes = []
        self._prev_fast = None
        self._prev_slow = None

        sec = self.Security
        ps = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(1)
        if ps <= Decimal(0):
            ps = Decimal(1)
        self._price_step = ps

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def _decimal_avg(self, start, count):
        s = Decimal(0)
        for i in range(start, start + count):
            s = Decimal.Add(s, self._closes[i])
        return Decimal.Divide(s, Decimal(count))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        self._closes.append(close)
        if len(self._closes) > SLOW_LEN + 10:
            self._closes.pop(0)

        if len(self._closes) < SLOW_LEN:
            return

        cnt = len(self._closes)
        fast = self._decimal_avg(cnt - FAST_LEN, FAST_LEN)
        slow = self._decimal_avg(cnt - SLOW_LEN, SLOW_LEN)

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        pos = self.Position
        if pos == Decimal(0):
            if self._current_direction != DIRECTION_NONE:
                self._reset_trailing()
            if prev_fast is not None and prev_slow is not None:
                if prev_fast <= prev_slow and fast > slow:
                    self.BuyMarket()
                    self._entry_price = close
                    self._trailing_active = False
                    self._trailing_stop_price = Decimal(0)
                    self._current_direction = DIRECTION_LONG
                elif prev_fast >= prev_slow and fast < slow:
                    self.SellMarket()
                    self._entry_price = close
                    self._trailing_active = False
                    self._trailing_stop_price = Decimal(0)
                    self._current_direction = DIRECTION_SHORT
            return

        if pos > Decimal(0) and self._current_direction == DIRECTION_LONG:
            self._update_long_trailing(close)
        elif pos < Decimal(0) and self._current_direction == DIRECTION_SHORT:
            self._update_short_trailing(close)

    def _update_long_trailing(self, price):
        stop_pips = Decimal(float(self._trailing_stop_pips.Value))
        stop_distance = Decimal.Multiply(stop_pips, self._price_step)
        if stop_distance <= Decimal(0):
            return

        step_pips = Decimal(float(self._trailing_step_pips.Value))
        step_distance = Decimal.Multiply(step_pips, self._price_step)

        if not self._trailing_active:
            if Decimal.Subtract(price, self._entry_price) >= stop_distance:
                self._trailing_active = True
                self._trailing_stop_price = self._entry_price
        else:
            desired_stop = Decimal.Subtract(price, stop_distance)
            if step_distance <= Decimal(0):
                if desired_stop > self._trailing_stop_price:
                    self._trailing_stop_price = desired_stop
            elif Decimal.Subtract(desired_stop, self._trailing_stop_price) >= step_distance:
                self._trailing_stop_price = desired_stop

        if self._trailing_active and price <= self._trailing_stop_price:
            if self.Position > Decimal(0):
                self.SellMarket()

    def _update_short_trailing(self, price):
        stop_pips = Decimal(float(self._trailing_stop_pips.Value))
        stop_distance = Decimal.Multiply(stop_pips, self._price_step)
        if stop_distance <= Decimal(0):
            return

        step_pips = Decimal(float(self._trailing_step_pips.Value))
        step_distance = Decimal.Multiply(step_pips, self._price_step)

        if not self._trailing_active:
            if Decimal.Subtract(self._entry_price, price) >= stop_distance:
                self._trailing_active = True
                self._trailing_stop_price = self._entry_price
        else:
            desired_stop = Decimal.Add(price, stop_distance)
            if step_distance <= Decimal(0):
                if desired_stop < self._trailing_stop_price or self._trailing_stop_price == Decimal(0):
                    self._trailing_stop_price = desired_stop
            elif Decimal.Subtract(self._trailing_stop_price, desired_stop) >= step_distance:
                self._trailing_stop_price = desired_stop

        if self._trailing_active and price >= self._trailing_stop_price:
            if self.Position < Decimal(0):
                self.BuyMarket()

    def _reset_trailing(self):
        self._entry_price = Decimal(0)
        self._trailing_stop_price = Decimal(0)
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE

    def OnReseted(self):
        super(trailing_stop_manager_strategy, self).OnReseted()
        self._entry_price = Decimal(0)
        self._trailing_stop_price = Decimal(0)
        self._trailing_active = False
        self._current_direction = DIRECTION_NONE
        self._price_step = Decimal(1)
        self._closes = []
        self._prev_fast = None
        self._prev_slow = None

    def CreateClone(self):
        return trailing_stop_manager_strategy()
