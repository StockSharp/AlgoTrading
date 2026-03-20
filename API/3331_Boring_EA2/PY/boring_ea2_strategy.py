import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class boring_ea2_strategy(Strategy):
    def __init__(self):
        super(boring_ea2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(boring_ea2_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(boring_ea2_strategy, self).OnStarted(time)

        self._fast = SimpleMovingAverage()
        self._fast.Length = 10
        self._med = SimpleMovingAverage()
        self._med.Length = 20
        self._slow = SimpleMovingAverage()
        self._slow.Length = 40

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
        return boring_ea2_strategy()
