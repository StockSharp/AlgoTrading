import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class bounce_number_strategy(Strategy):
    def __init__(self):
        super(bounce_number_strategy, self).__init__()

        self._max_history_candles = self.Param("MaxHistoryCandles", 10000) \
            .SetDisplay("Max History Candles", "Maximum number of candles inspected inside a single channel cycle", "General")
        self._channel_points = self.Param("ChannelPoints", 10) \
            .SetDisplay("Max History Candles", "Maximum number of candles inspected inside a single channel cycle", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Max History Candles", "Maximum number of candles inspected inside a single channel cycle", "General")

        self._bounce_distribution = new()
        self._channel_center = None
        self._bounce_count = 0.0
        self._last_touch_direction = 0.0
        self._candles_in_cycle = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bounce_number_strategy, self).OnReseted()
        self._bounce_distribution = new()
        self._channel_center = None
        self._bounce_count = 0.0
        self._last_touch_direction = 0.0
        self._candles_in_cycle = 0.0

    def OnStarted(self, time):
        super(bounce_number_strategy, self).OnStarted(time)


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
        return bounce_number_strategy()
