import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class two_ma_other_time_frame_correct_intersection_strategy(Strategy):
    def __init__(self):
        super(two_ma_other_time_frame_correct_intersection_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Primary timeframe used for signal evaluation", "General")
        self._fast_length = self.Param("FastLength", 20) \
            .SetDisplay("Candle Type", "Primary timeframe used for signal evaluation", "General")
        self._slow_length = self.Param("SlowLength", 50) \
            .SetDisplay("Candle Type", "Primary timeframe used for signal evaluation", "General")

        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(two_ma_other_time_frame_correct_intersection_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(two_ma_other_time_frame_correct_intersection_strategy, self).OnStarted(time)

        self.__fast_ma = ExponentialMovingAverage()
        self.__fast_ma.Length = self.fast_length
        self.__slow_ma = ExponentialMovingAverage()
        self.__slow_ma.Length = self.slow_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ma, self.__slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return two_ma_other_time_frame_correct_intersection_strategy()
