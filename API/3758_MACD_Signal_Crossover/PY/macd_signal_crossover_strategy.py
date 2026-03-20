import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_signal_crossover_strategy(Strategy):
    def __init__(self):
        super(macd_signal_crossover_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 23)
        self._slow_period = self.Param("SlowPeriod", 40)
        self._signal_period = self.Param("SignalPeriod", 8)
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1)

        self._prev_macd_above_signal = False
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_signal_crossover_strategy, self).OnReseted()
        self._prev_macd_above_signal = False
        self._has_prev = False

    def OnStarted(self, time):
        super(macd_signal_crossover_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_signal_crossover_strategy()
