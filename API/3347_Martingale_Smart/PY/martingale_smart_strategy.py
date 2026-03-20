import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class martingale_smart_strategy(Strategy):
    def __init__(self):
        super(martingale_smart_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingale_smart_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(martingale_smart_strategy, self).OnStarted(time)

        self._fast = SimpleMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return martingale_smart_strategy()
