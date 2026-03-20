import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class obv_traffic_lights_strategy(Strategy):
    def __init__(self):
        super(obv_traffic_lights_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetGreaterThanZero()
        self._slow_length = self.Param("SlowLength", 14) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_close = 0.0
        self._obv = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._count = 0
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(obv_traffic_lights_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._obv = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._count = 0
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(obv_traffic_lights_strategy, self).OnStarted(time)
        self._prev_close = 0.0
        self._obv = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._count = 0
        self._last_signal_ticks = 0
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.OnProcess).Start()

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        vol = float(candle.TotalVolume)
        self._count += 1
        if self._prev_close != 0.0:
            if close > self._prev_close:
                self._obv += vol
            elif close < self._prev_close:
                self._obv -= vol
        self._prev_close = close
        fl = self._fast_length.Value
        sl = self._slow_length.Value
        fast_mult = 2.0 / (fl + 1)
        slow_mult = 2.0 / (sl + 1)
        if self._count <= 2:
            self._prev_fast = self._obv
            self._prev_slow = self._obv
            return
        fast_value = self._obv * fast_mult + self._prev_fast * (1 - fast_mult)
        slow_value = self._obv * slow_mult + self._prev_slow * (1 - slow_mult)
        self._prev_fast = fast_value
        self._prev_slow = slow_value
        if self._count < sl + 5:
            return
        go_long = self._obv > slow_value and fast_value > slow_value
        go_short = self._obv < slow_value and fast_value < slow_value
        cooldown_ticks = TimeSpan.FromMinutes(600).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks < cooldown_ticks:
            return
        if go_long and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_ticks = current_ticks
        elif go_short and self.Position >= 0:
            self.SellMarket()
            self._last_signal_ticks = current_ticks

    def CreateClone(self):
        return obv_traffic_lights_strategy()
