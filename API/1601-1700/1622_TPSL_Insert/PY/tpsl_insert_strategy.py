import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class tpsl_insert_strategy(Strategy):
    def __init__(self):
        super(tpsl_insert_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "General")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_diff = 0.0

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
        super(tpsl_insert_strategy, self).OnReseted()
        self._prev_diff = 0.0

    def OnStarted2(self, time):
        super(tpsl_insert_strategy, self).OnStarted2(time)
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.fast_length
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_fast, ema_slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        diff = fast - slow
        cross_up = self._prev_diff <= 0 and diff > 0
        cross_down = self._prev_diff >= 0 and diff < 0
        self._prev_diff = diff
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return tpsl_insert_strategy()
