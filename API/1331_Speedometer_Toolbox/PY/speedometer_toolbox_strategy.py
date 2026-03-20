import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class speedometer_toolbox_strategy(Strategy):
    def __init__(self):
        super(speedometer_toolbox_strategy, self).__init__()
        self._slow_length = self.Param("SlowLength", 40) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_f = 0.0
        self._prev_s = 0.0
        self._init = False
        self._last_signal = DateTimeOffset.MinValue
        self._cooldown = TimeSpan.FromMinutes(360)

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(speedometer_toolbox_strategy, self).OnReseted()
        self._prev_f = 0.0
        self._prev_s = 0.0
        self._init = False
        self._last_signal = DateTimeOffset.MinValue

    def OnStarted(self, time):
        super(speedometer_toolbox_strategy, self).OnStarted(time)
        self._fast = ExponentialMovingAverage()
        self._fast.Length = 14
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self.on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast)
            self.DrawIndicator(area, self._slow)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, f, s):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast.IsFormed or not self._slow.IsFormed:
            return
        f = float(f)
        s = float(s)
        if not self._init:
            self._prev_f = f
            self._prev_s = s
            self._init = True
            return
        if candle.OpenTime - self._last_signal >= self._cooldown:
            if self._prev_f <= self._prev_s and f > s and self.Position <= 0:
                self.BuyMarket()
                self._last_signal = candle.OpenTime
            elif self._prev_f >= self._prev_s and f < s and self.Position >= 0:
                self.SellMarket()
                self._last_signal = candle.OpenTime
        self._prev_f = f
        self._prev_s = s

    def CreateClone(self):
        return speedometer_toolbox_strategy()
