import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class fast2_crossover_strategy(Strategy):
    """
    Fast2 histogram moving average crossover.
    Uses weighted candle body differences with WMA smoothing.
    """

    def __init__(self):
        super(fast2_crossover_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast length", "Fast length", "General")
        self._slow_length = self.Param("SlowLength", 13) \
            .SetDisplay("Slow length", "Slow length", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev_average = False
        self._prev_diff1 = 0.0
        self._prev_diff2 = 0.0
        self._has_prev_diff1 = False
        self._has_prev_diff2 = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fast2_crossover_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev_average = False
        self._prev_diff1 = 0.0
        self._prev_diff2 = 0.0
        self._has_prev_diff1 = False
        self._has_prev_diff2 = False

    def OnStarted(self, time):
        super(fast2_crossover_strategy, self).OnStarted(time)

        self._fast = WeightedMovingAverage()
        self._fast.Length = self._fast_length.Value
        self._slow = WeightedMovingAverage()
        self._slow.Length = self._slow_length.Value

        self.Indicators.Add(self._fast)
        self.Indicators.Add(self._slow)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        diff = close - open_p
        hist = diff
        if self._has_prev_diff1:
            hist += self._prev_diff1 / math.sqrt(2)
        if self._has_prev_diff2:
            hist += self._prev_diff2 / math.sqrt(3)

        fast_result = self._fast.Process(hist, candle.OpenTime, True)
        slow_result = self._slow.Process(hist, candle.OpenTime, True)

        self._prev_diff2 = self._prev_diff1
        self._prev_diff1 = diff
        self._has_prev_diff2 = self._has_prev_diff1
        self._has_prev_diff1 = True

        if fast_result.IsEmpty or slow_result.IsEmpty or not self._fast.IsFormed or not self._slow.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        f = float(fast_result.ToDecimal())
        s = float(slow_result.ToDecimal())

        if self._has_prev_average:
            if self._prev_fast > self._prev_slow and f < s and self.Position <= 0:
                self.BuyMarket()
            if self._prev_fast < self._prev_slow and f > s and self.Position >= 0:
                self.SellMarket()

        self._prev_fast = f
        self._prev_slow = s
        self._has_prev_average = True

    def CreateClone(self):
        return fast2_crossover_strategy()
