import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class polynomial_regression_bands_channel_strategy(Strategy):
    def __init__(self):
        super(polynomial_regression_bands_channel_strategy, self).__init__()
        self._slow_length = self.Param("SlowLength", 40) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(polynomial_regression_bands_channel_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(polynomial_regression_bands_channel_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._last_signal_ticks = 0
        self._fast = ExponentialMovingAverage()
        self._fast.Length = 14
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self.OnProcess).Start()

    def OnProcess(self, candle, f, s):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast.IsFormed or not self._slow.IsFormed:
            return
        fv = float(f)
        sv = float(s)
        if not self._initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._initialized = True
            return
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks >= cooldown_ticks:
            if self._prev_fast <= self._prev_slow and fv > sv and self.Position <= 0:
                self.BuyMarket()
                self._last_signal_ticks = current_ticks
            elif self._prev_fast >= self._prev_slow and fv < sv and self.Position >= 0:
                self.SellMarket()
                self._last_signal_ticks = current_ticks
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return polynomial_regression_bands_channel_strategy()
