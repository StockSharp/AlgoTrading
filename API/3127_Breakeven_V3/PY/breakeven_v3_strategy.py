import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class breakeven_v3_strategy(Strategy):
    def __init__(self):
        super(breakeven_v3_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._activation_points = self.Param("ActivationPoints", 200) \
            .SetDisplay("Activation", "Distance price must move before break-even activates", "Risk")
        self._delta_points = self.Param("DeltaPoints", 100) \
            .SetDisplay("Delta", "Offset from entry for break-even stop", "Risk")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._break_even_price = 0.0
        self._break_even_activated = False
        self._cooldown = 0

    @property
    def fast_period(self):
        return self._fast_period.Value
    @property
    def slow_period(self):
        return self._slow_period.Value
    @property
    def activation_points(self):
        return self._activation_points.Value
    @property
    def delta_points(self):
        return self._delta_points.Value

    def OnReseted(self):
        super(breakeven_v3_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._break_even_price = 0.0
        self._break_even_activated = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(breakeven_v3_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(fast, slow, self.OnProcess).Start()

    def OnProcess(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast_value)
        slow_val = float(slow_value)
        if fast_val == 0 or slow_val == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 1.0

        if self.Position != 0 and self._entry_price > 0:
            activation_distance = float(self.activation_points) * step
            delta_offset = float(self.delta_points) * step
            if self.Position > 0:
                if not self._break_even_activated and activation_distance > 0 and close >= self._entry_price + activation_distance:
                    self._break_even_activated = True
                    self._break_even_price = self._entry_price + delta_offset
                if self._break_even_activated and close <= self._break_even_price:
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._break_even_price = 0.0
                    self._break_even_activated = False
                    self._cooldown = 100
                    self._prev_fast = fast_val
                    self._prev_slow = slow_val
                    return
            elif self.Position < 0:
                if not self._break_even_activated and activation_distance > 0 and close <= self._entry_price - activation_distance:
                    self._break_even_activated = True
                    self._break_even_price = self._entry_price - delta_offset
                if self._break_even_activated and close >= self._break_even_price:
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._break_even_price = 0.0
                    self._break_even_activated = False
                    self._cooldown = 100
                    self._prev_fast = fast_val
                    self._prev_slow = slow_val
                    return

        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._break_even_activated = False
            self._cooldown = 100
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._break_even_activated = False
            self._cooldown = 100

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return breakeven_v3_strategy()
