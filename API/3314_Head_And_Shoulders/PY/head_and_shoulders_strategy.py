import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class head_and_shoulders_strategy(Strategy):
    def __init__(self):
        super(head_and_shoulders_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(head_and_shoulders_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(head_and_shoulders_strategy, self).OnStarted(time)

        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.slow_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

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
        return head_and_shoulders_strategy()
