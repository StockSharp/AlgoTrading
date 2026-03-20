import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fast_slow_rvi_crossover_strategy(Strategy):
    def __init__(self):
        super(fast_slow_rvi_crossover_strategy, self).__init__()

        self._rvi_period = self.Param("RviPeriod", 20) \
            .SetDisplay("RVI Period", "Period for the Relative Vigor Index", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("RVI Period", "Period for the Relative Vigor Index", "Indicators")

        self._previous_average = None
        self._previous_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fast_slow_rvi_crossover_strategy, self).OnReseted()
        self._previous_average = None
        self._previous_signal = None

    def OnStarted(self, time):
        super(fast_slow_rvi_crossover_strategy, self).OnStarted(time)

        self._rvi = ExponentialMovingAverage()
        self._rvi.Length = self.rvi_period
        self._signal = ExponentialMovingAverage()
        self._signal.Length = self.rvi_period * 2

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rvi, self._signal, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return fast_slow_rvi_crossover_strategy()
