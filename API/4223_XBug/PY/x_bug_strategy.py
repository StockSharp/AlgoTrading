import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x_bug_strategy(Strategy):
    def __init__(self):
        super(x_bug_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast MA period", "Length of the fast moving average.", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 14) \
            .SetDisplay("Fast MA period", "Length of the fast moving average.", "Indicators")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Fast MA period", "Length of the fast moving average.", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Fast MA period", "Length of the fast moving average.", "Indicators")

        self._slow_ma = None
        self._prev_fast = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(x_bug_strategy, self).OnReseted()
        self._slow_ma = None
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(x_bug_strategy, self).OnStarted(time)

        self.__slow_ma = SimpleMovingAverage()
        self.__slow_ma.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return x_bug_strategy()
