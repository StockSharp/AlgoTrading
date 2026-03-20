import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_fx_crossover_strategy(Strategy):
    def __init__(self):
        super(simple_fx_crossover_strategy, self).__init__()

        self._short_period = self.Param("ShortPeriod", 10) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._long_period = self.Param("LongPeriod", 30) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")

        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(simple_fx_crossover_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(simple_fx_crossover_strategy, self).OnStarted(time)

        self._fast = SimpleMovingAverage()
        self._fast.Length = self.short_period
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.long_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return simple_fx_crossover_strategy()
