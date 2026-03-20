import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fibo_avg001a_strategy(Strategy):
    def __init__(self):
        super(fibo_avg001a_strategy, self).__init__()
        self._fibo_num_period = self.Param("FiboNumPeriod", 11) \
            .SetDisplay("Fibo Period", "Additional length for slow MA", "Indicators")
        self._ma_period = self.Param("MaPeriod", 21) \
            .SetDisplay("MA Period", "Base moving average period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fibo_num_period(self):
        return self._fibo_num_period.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibo_avg001a_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(fibo_avg001a_strategy, self).OnStarted(time)
        fast_ma = SmoothedMovingAverage()
        fast_ma.Length = self.ma_period
        slow_ma = SmoothedMovingAverage()
        slow_ma.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.on_process).Start()
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
        # Fast crosses above slow -> buy
        if self._prev_fast <= self._prev_slow and fast > slow:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Fast crosses below slow -> sell
        elif self._prev_fast >= self._prev_slow and fast < slow:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return fibo_avg001a_strategy()
