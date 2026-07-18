import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class two_ma_other_time_frame_correct_intersection_strategy(Strategy):
    def __init__(self):
        super(two_ma_other_time_frame_correct_intersection_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_length = self.Param("FastLength", 20)
        self._slow_length = self.Param("SlowLength", 50)

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
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    def OnReseted(self):
        super(two_ma_other_time_frame_correct_intersection_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(two_ma_other_time_frame_correct_intersection_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastLength
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        if self._has_prev:
            if self._prev_fast < self._prev_slow and fast_val > slow_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_fast > self._prev_slow and fast_val < slow_val and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._has_prev = True

    def CreateClone(self):
        return two_ma_other_time_frame_correct_intersection_strategy()
