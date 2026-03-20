import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x_alert3_strategy(Strategy):
    def __init__(self):
        super(x_alert3_strategy, self).__init__()
        self._ma1_period = self.Param("Ma1Period", 10) \
            .SetDisplay("MA1 Period", "Fast moving average period", "Indicators")
        self._ma2_period = self.Param("Ma2Period", 30) \
            .SetDisplay("MA2 Period", "Slow moving average period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def ma1_period(self):
        return self._ma1_period.Value

    @property
    def ma2_period(self):
        return self._ma2_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(x_alert3_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(x_alert3_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.ma1_period
        slow = ExponentialMovingAverage()
        slow.Length = self.ma2_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        if self._prev_fast <= self._prev_slow and fast > slow:
            if self.Position < 0) BuyMarket(:
                if self.Position <= 0) BuyMarket(:
            elif self._prev_fast >= self._prev_slow and fast < slow:
            if self.Position > 0) SellMarket(:
                if self.Position >= 0) SellMarket(:
            self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return x_alert3_strategy()
