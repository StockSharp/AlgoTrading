import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class surefire_thing_strategy(Strategy):
    def __init__(self):
        super(surefire_thing_strategy, self).__init__()

        self._range_multiplier = self.Param("RangeMultiplier", 0.5) \
            .SetDisplay("Range Mult", "Multiplier for range-based levels", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Range Mult", "Multiplier for range-based levels", "General")

        self._current_day = None
        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_ready = False
        self._prev_day_close = 0.0
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0
        self._has_prev_day = False
        self._traded_today = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(surefire_thing_strategy, self).OnReseted()
        self._current_day = None
        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_ready = False
        self._prev_day_close = 0.0
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0
        self._has_prev_day = False
        self._traded_today = False

    def OnStarted(self, time):
        super(surefire_thing_strategy, self).OnStarted(time)


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
        return surefire_thing_strategy()
