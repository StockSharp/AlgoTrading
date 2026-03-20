import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class price_channel_signal_v2_strategy(Strategy):
    def __init__(self):
        super(price_channel_signal_v2_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Donchian lookback used for Price Channel", "Price Channel")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Channel Period", "Donchian lookback used for Price Channel", "Price Channel")

        self._high_history = new()
        self._low_history = new()
        self._previous_trend = 0.0
        self._previous_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(price_channel_signal_v2_strategy, self).OnReseted()
        self._high_history = new()
        self._low_history = new()
        self._previous_trend = 0.0
        self._previous_close = None

    def OnStarted(self, time):
        super(price_channel_signal_v2_strategy, self).OnStarted(time)


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
        return price_channel_signal_v2_strategy()
