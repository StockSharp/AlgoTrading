import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class status_mail_and_alert_on_order_close_strategy(Strategy):
    def __init__(self):
        super(status_mail_and_alert_on_order_close_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 9) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

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
        super(status_mail_and_alert_on_order_close_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(status_mail_and_alert_on_order_close_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_length
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_length
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
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast < slow:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return status_mail_and_alert_on_order_close_strategy()
