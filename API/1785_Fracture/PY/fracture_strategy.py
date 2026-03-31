import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fracture_strategy(Strategy):
    def __init__(self):
        super(fracture_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle", "Candle type", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast", "Fast EMA period", "EMA")
        self._mid_period = self.Param("MidPeriod", 20) \
            .SetDisplay("Mid", "Mid EMA period", "EMA")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow", "Slow EMA period", "EMA")
        self._prev_fast = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def mid_period(self):
        return self._mid_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(fracture_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(fracture_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        mid = ExponentialMovingAverage()
        mid.Length = self.mid_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        self.SubscribeCandles(self.candle_type).Bind(fast, mid, slow, self.process_candle).Start()

    def process_candle(self, candle, fast_val, mid_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_val)
        mv = float(mid_val)
        if not self._has_prev:
            self._prev_fast = fv
            self._prev_mid = mv
            self._has_prev = True
            return
        cross_up = self._prev_fast <= self._prev_mid and fv > mv
        cross_down = self._prev_fast >= self._prev_mid and fv < mv
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fv
        self._prev_mid = mv

    def CreateClone(self):
        return fracture_strategy()
