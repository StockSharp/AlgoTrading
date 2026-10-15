import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ronz_auto_sltp_strategy(Strategy):
    def __init__(self):
        super(ronz_auto_sltp_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 10)
        self._slow_period = self.Param("SlowPeriod", 30)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    def OnReseted(self):
        super(ronz_auto_sltp_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(ronz_auto_sltp_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        if self._has_prev:
            if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
                self.SellMarket()
        else:
            if fast_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif fast_val < slow_val and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._has_prev = True

    def CreateClone(self):
        return ronz_auto_sltp_strategy()
