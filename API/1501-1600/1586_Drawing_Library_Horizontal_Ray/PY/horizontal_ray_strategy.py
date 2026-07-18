import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class horizontal_ray_strategy(Strategy):
    def __init__(self):
        super(horizontal_ray_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast Length", "Fast SMA length", "General")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow Length", "Slow SMA length", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(horizontal_ray_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted2(self, time):
        super(horizontal_ray_strategy, self).OnStarted2(time)
        fast = SimpleMovingAverage()
        fast.Length = self.fast_length
        slow = SimpleMovingAverage()
        slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return
        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return horizontal_ray_strategy()
