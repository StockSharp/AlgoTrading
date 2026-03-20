import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class at_random_strategy(Strategy):
    def __init__(self):
        super(at_random_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe that triggers random decisions", "Data")
        self._random_seed = self.Param("RandomSeed", 42) \
            .SetDisplay("Candle Type", "Timeframe that triggers random decisions", "Data")

        self._random = None
        self._bar_count = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(at_random_strategy, self).OnReseted()
        self._random = None
        self._bar_count = 0.0

    def OnStarted(self, time):
        super(at_random_strategy, self).OnStarted(time)


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
        return at_random_strategy()
