import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class channel_ea_limits_strategy(Strategy):
    def __init__(self):
        super(channel_ea_limits_strategy, self).__init__()

        self._begin_hour = self.Param("BeginHour", 1) \
            .SetDisplay("Begin Hour", "Hour when session tracking starts (0-23)", "Session")
        self._end_hour = self.Param("EndHour", 10) \
            .SetDisplay("Begin Hour", "Hour when session tracking starts (0-23)", "Session")
        self._order_volume = self.Param("OrderVolume", 1) \
            .SetDisplay("Begin Hour", "Hour when session tracking starts (0-23)", "Session")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Begin Hour", "Hour when session tracking starts (0-23)", "Session")

        self._session_start = None
        self._session_end = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._bars_in_session = 0.0
        self._prev_candle_close = None
        self._orders_placed = False
        self._needs_session_reset = False
        self._trade_taken = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(channel_ea_limits_strategy, self).OnReseted()
        self._session_start = None
        self._session_end = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._bars_in_session = 0.0
        self._prev_candle_close = None
        self._orders_placed = False
        self._needs_session_reset = False
        self._trade_taken = False

    def OnStarted(self, time):
        super(channel_ea_limits_strategy, self).OnStarted(time)


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
        return channel_ea_limits_strategy()
