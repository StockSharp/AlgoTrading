import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trend_following_mm3_high_low_strategy(Strategy):
    def __init__(self):
        super(trend_following_mm3_high_low_strategy, self).__init__()
        self._slow_length = self.Param("SlowLength", TimeSpan.FromMinutes(5)) \
            .SetDisplay("Slow Length", "Slow EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type", "General")

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(trend_following_mm3_high_low_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = 14
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def CreateClone(self):
        return trend_following_mm3_high_low_strategy()
