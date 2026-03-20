import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x_trail2_strategy(Strategy):
    def __init__(self):
        super(x_trail2_strategy, self).__init__()
        self._ma1_length = self.Param("Ma1Length", 10) \
            .SetDisplay("MA1 Length", "Length of the fast MA", "Moving Averages")
        self._ma2_length = self.Param("Ma2Length", 30) \
            .SetDisplay("MA2 Length", "Length of the slow MA", "Moving Averages")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def ma1_length(self):
        return self._ma1_length.Value

    @property
    def ma2_length(self):
        return self._ma2_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(x_trail2_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(x_trail2_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.ma1_length
        slow = ExponentialMovingAverage()
        slow.Length = self.ma2_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if self._has_prev:
            if fast > slow and self._prev_fast <= self._prev_slow and self.Position <= 0:
                self.BuyMarket()
            elif fast < slow and self._prev_fast >= self._prev_slow and self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow
        self._has_prev = True

    def CreateClone(self):
        return x_trail2_strategy()
