import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class simple_pivot_flip_strategy(Strategy):
    def __init__(self):
        super(simple_pivot_flip_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1) \
            .SetDisplay("Order Volume", "Market order volume used for entries.", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1) \
            .SetDisplay("Order Volume", "Market order volume used for entries.", "General")

        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous_candle = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(simple_pivot_flip_strategy, self).OnReseted()
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous_candle = False

    def OnStarted(self, time):
        super(simple_pivot_flip_strategy, self).OnStarted(time)


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
        return simple_pivot_flip_strategy()
