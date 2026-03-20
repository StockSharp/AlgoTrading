import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class mm_fibonacci_strategy(Strategy):
    def __init__(self):
        super(mm_fibonacci_strategy, self).__init__()
        self._frame = self.Param("Frame", 64) \
            .SetGreaterThanZero()
        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetGreaterThanZero()
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._since_high = 0
        self._since_low = 0
        self._prev_fib_dir = 0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mm_fibonacci_strategy, self).OnReseted()
        self._since_high = 0
        self._since_low = 0
        self._prev_fib_dir = 0
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(mm_fibonacci_strategy, self).OnStarted(time)
        self._since_high = 0
        self._since_low = 0
        self._prev_fib_dir = 0
        self._bars_from_signal = self._signal_cooldown_bars.Value
        length = int(round(self._frame.Value * float(self._multiplier.Value)))
        self._highest = Highest()
        self._highest.Length = length
        self._lowest = Lowest()
        self._lowest.Length = length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self.OnProcess).Start()

    def OnProcess(self, candle, n_high, n_low):
        if candle.State != CandleStates.Finished:
            return
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return
        nh = float(n_high)
        nl = float(n_low)
        rng = nh - nl
        if rng <= 0.0:
            return
        if nh <= 250000.0 and nh > 25000.0:
            fractal = 100000.0
        elif nh <= 25000.0 and nh > 2500.0:
            fractal = 10000.0
        elif nh <= 2500.0 and nh > 250.0:
            fractal = 1000.0
        elif nh <= 250.0 and nh > 25.0:
            fractal = 100.0
        elif nh <= 25.0 and nh > 6.25:
            fractal = 12.5
        elif nh <= 6.25 and nh > 3.125:
            fractal = 6.25
        elif nh <= 3.125 and nh > 1.5625:
            fractal = 3.125
        elif nh <= 1.5625 and nh > 0.390625:
            fractal = 1.5625
        else:
            fractal = 0.1953125
        s = math.floor(math.log(fractal / rng, 2))
        octave = fractal * (0.5 ** s)
        minimum = math.floor(nl / octave) * octave
        maximum = minimum + octave if (minimum + octave) > nh else minimum + 2.0 * octave
        t1 = 0.0
        t2 = 0.0
        t3 = 0.0
        t4 = 0.0
        t5 = 0.0
        diff = maximum - minimum
        if nl >= (3.0 * diff / 16.0 + minimum) and nh <= (9.0 * diff / 16.0 + minimum):
            t2 = minimum + diff / 2.0
        if nl >= (minimum - diff / 8.0) and nh <= (5.0 * diff / 8.0 + minimum) and t2 == 0.0:
            t1 = minimum + diff / 2.0
        if nl >= (minimum + 7.0 * diff / 16.0) and nh <= (13.0 * diff / 16.0 + minimum):
            t4 = minimum + 3.0 * diff / 4.0
        if nl >= (minimum + 3.0 * diff / 8.0) and nh <= (9.0 * diff / 8.0 + minimum) and t4 == 0.0:
            t5 = maximum
        if nl >= (minimum + diff / 8.0) and nh <= (7.0 * diff / 8.0 + minimum) and t1 == 0.0 and t2 == 0.0 and t4 == 0.0 and t5 == 0.0:
            t3 = minimum + 3.0 * diff / 4.0
        t6 = maximum if (t1 + t2 + t3 + t4 + t5) == 0.0 else 0.0
        top = t1 + t2 + t3 + t4 + t5 + t6
        b1 = 0.0
        b2 = 0.0
        b3 = 0.0
        b4 = 0.0
        b5 = 0.0
        if t1 > 0.0:
            b1 = minimum
        if t2 > 0.0:
            b2 = minimum + diff / 4.0
        if t3 > 0.0:
            b3 = minimum + diff / 4.0
        if t4 > 0.0:
            b4 = minimum + diff / 2.0
        if t5 > 0.0:
            b5 = minimum + diff / 2.0
        b6 = minimum if (top > 0.0 and (b1 + b2 + b3 + b4 + b5) == 0.0) else 0.0
        bottom = b1 + b2 + b3 + b4 + b5 + b6
        fib_range = top - bottom
        if fib_range <= 0.0:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._since_high = 0 if high >= top else self._since_high + 1
        self._since_low = 0 if low <= bottom else self._since_low + 1
        fib_dir_up = self._since_high > self._since_low
        fib_dir_dn = self._since_high < self._since_low
        fib_dir = 1 if fib_dir_up else (-1 if fib_dir_dn else 0)
        self._bars_from_signal += 1
        if fib_dir != 0 and fib_dir != self._prev_fib_dir and self._bars_from_signal >= self._signal_cooldown_bars.Value:
            if fib_dir > 0 and self.Position <= 0:
                self.BuyMarket()
                self._bars_from_signal = 0
            elif fib_dir < 0 and self.Position >= 0:
                self.SellMarket()
                self._bars_from_signal = 0
        if fib_dir != 0:
            self._prev_fib_dir = fib_dir

    def CreateClone(self):
        return mm_fibonacci_strategy()
