import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class universal_investor3920_strategy(Strategy):
    def __init__(self):
        super(universal_investor3920_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def fast_period(self): return self._fast_period.Value
    @property
    def slow_period(self): return self._slow_period.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(universal_investor3920_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(universal_investor3920_strategy, self).OnStarted2(time)
        self._has_prev = False
        self._cooldown = 0
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast)
        slow_val = float(slow)
        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return
        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 2
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 2
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return universal_investor3920_strategy()
