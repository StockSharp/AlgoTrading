import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class resonance_hunter_strategy(Strategy):
    def __init__(self):
        super(resonance_hunter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_k_period = self.Param("FastKPeriod", 8) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._slow_k_period = self.Param("SlowKPeriod", 21) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._bar_count = 0.0
        self._prev_fast_k = None
        self._prev_slow_k = None
        self._prev_fast_d = None
        self._prev_slow_d = None
        self._fast_k_count = 0.0
        self._slow_k_count = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(resonance_hunter_strategy, self).OnReseted()
        self._bar_count = 0.0
        self._prev_fast_k = None
        self._prev_slow_k = None
        self._prev_fast_d = None
        self._prev_slow_d = None
        self._fast_k_count = 0.0
        self._slow_k_count = 0.0

    def OnStarted(self, time):
        super(resonance_hunter_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return resonance_hunter_strategy()
