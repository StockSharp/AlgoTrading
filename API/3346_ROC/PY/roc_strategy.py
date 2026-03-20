import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class roc_strategy(Strategy):
    def __init__(self):
        super(roc_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._roc_period = self.Param("RocPeriod", 12) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 5) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(roc_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(roc_strategy, self).OnStarted(time)

        self._roc = RateOfChange()
        self._roc.Length = self.roc_period
        self._fast_ma = WeightedMovingAverage()
        self._fast_ma.Length = self.fast_ma_period
        self._slow_ma = WeightedMovingAverage()
        self._slow_ma.Length = self.slow_ma_period

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
        return roc_strategy()
