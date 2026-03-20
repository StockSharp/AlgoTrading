import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bruno_trend_strategy(Strategy):
    def __init__(self):
        super(bruno_trend_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bruno_trend_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(bruno_trend_strategy, self).OnStarted(time)

        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.slow_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bruno_trend_strategy()
