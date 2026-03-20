import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class hpcs_inter4_strategy(Strategy):
    def __init__(self):
        super(hpcs_inter4_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Time frame for calculation", "General")
        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Candle Type", "Time frame for calculation", "General")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Candle Type", "Time frame for calculation", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first_value = True

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hpcs_inter4_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first_value = True

    def OnStarted(self, time):
        super(hpcs_inter4_strategy, self).OnStarted(time)

        self._fast_sma = ExponentialMovingAverage()
        self._fast_sma.Length = self.fast_period
        self._slow_sma = ExponentialMovingAverage()
        self._slow_sma.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_sma, self._slow_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return hpcs_inter4_strategy()
