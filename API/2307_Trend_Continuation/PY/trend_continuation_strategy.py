import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trend_continuation_strategy(Strategy):
    def __init__(self):
        super(trend_continuation_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Fast EMA Length", "Period for the fast EMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast = None
        self._prev_slow = None

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_continuation_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(trend_continuation_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_slow = None
        fast = ExponentialMovingAverage()
        fast.Length = self.length
        slow = ExponentialMovingAverage()
        slow.Length = self.length * 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast)
        slow = float(slow)
        if self._prev_fast is not None and self._prev_slow is not None:
            if self._prev_fast < self._prev_slow and fast >= slow and self.Position <= 0:
                self.BuyMarket()
            if self._prev_fast > self._prev_slow and fast <= slow and self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return trend_continuation_strategy()
