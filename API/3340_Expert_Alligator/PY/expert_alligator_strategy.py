import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class expert_alligator_strategy(Strategy):
    def __init__(self):
        super(expert_alligator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(expert_alligator_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(expert_alligator_strategy, self).OnStarted(time)

        self._lips = SimpleMovingAverage()
        self._lips.Length = 5
        self._teeth = SimpleMovingAverage()
        self._teeth.Length = 8
        self._jaw = SimpleMovingAverage()
        self._jaw.Length = 13

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
        return expert_alligator_strategy()
