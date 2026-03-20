import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class smart_forex_system_strategy(Strategy):
    def __init__(self):
        super(smart_forex_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._mid_period = self.Param("MidPeriod", 25) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._was_bullish_alignment = False
        self._has_prev_alignment = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smart_forex_system_strategy, self).OnReseted()
        self._was_bullish_alignment = False
        self._has_prev_alignment = False

    def OnStarted(self, time):
        super(smart_forex_system_strategy, self).OnStarted(time)

        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.fast_period
        self._mid = ExponentialMovingAverage()
        self._mid.Length = self.mid_period
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._mid, self._slow, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return smart_forex_system_strategy()
