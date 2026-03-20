import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lucky_shift_limit_strategy(Strategy):
    def __init__(self):
        super(lucky_shift_limit_strategy, self).__init__()

        self._shift_points = self.Param("ShiftPoints", 3) \
            .SetDisplay("Shift points", "Minimum pip delta between consecutive quotes", "Trading")
        self._limit_points = self.Param("LimitPoints", 18) \
            .SetDisplay("Shift points", "Minimum pip delta between consecutive quotes", "Trading")

        self._previous_ask = None
        self._previous_bid = None
        self._current_ask = None
        self._current_bid = None
        self._shift_offset = 0.0
        self._limit_offset = 0.0
        self._entry_price = 0.0

    def OnReseted(self):
        super(lucky_shift_limit_strategy, self).OnReseted()
        self._previous_ask = None
        self._previous_bid = None
        self._current_ask = None
        self._current_bid = None
        self._shift_offset = 0.0
        self._limit_offset = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(lucky_shift_limit_strategy, self).OnStarted(time)


    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return lucky_shift_limit_strategy()
