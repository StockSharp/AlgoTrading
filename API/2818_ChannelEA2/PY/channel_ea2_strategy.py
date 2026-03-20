import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class channel_ea2_strategy(Strategy):
    def __init__(self):
        super(channel_ea2_strategy, self).__init__()

        self._begin_hour = self.Param("BeginHour", 1) \
            .SetDisplay("Begin Hour", "Hour when the session resets", "Trading")
        self._end_hour = self.Param("EndHour", 10) \
            .SetDisplay("Begin Hour", "Hour when the session resets", "Trading")
        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Begin Hour", "Hour when the session resets", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Begin Hour", "Hour when the session resets", "Trading")
        self._stop_buffer_multiplier = self.Param("StopBufferMultiplier", 2) \
            .SetDisplay("Begin Hour", "Hour when the session resets", "Trading")

        self._session_high = None
        self._session_low = None
        self._channel_ready = False
        self._entry_price = None
        self._stop_loss_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(channel_ea2_strategy, self).OnReseted()
        self._session_high = None
        self._session_low = None
        self._channel_ready = False
        self._entry_price = None
        self._stop_loss_price = None

    def OnStarted(self, time):
        super(channel_ea2_strategy, self).OnStarted(time)


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
        return channel_ea2_strategy()
