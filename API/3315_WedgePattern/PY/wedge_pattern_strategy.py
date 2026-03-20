import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class wedge_pattern_strategy(Strategy):
    def __init__(self):
        super(wedge_pattern_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 40) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._mom_period = self.Param("MomPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wedge_pattern_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(wedge_pattern_strategy, self).OnStarted(time)

        self._fast = WeightedMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = WeightedMovingAverage()
        self._slow.Length = self.slow_period
        self._mom = Momentum()
        self._mom.Length = self.mom_period

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
        return wedge_pattern_strategy()
