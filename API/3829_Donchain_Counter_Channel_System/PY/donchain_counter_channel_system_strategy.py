import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class donchain_counter_channel_system_strategy(Strategy):
    def __init__(self):
        super(donchain_counter_channel_system_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Donchian channel lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Channel Period", "Donchian channel lookback", "Indicators")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_prev_high = 0.0
        self._prev_prev_low = 0.0
        self._bar_count = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchain_counter_channel_system_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_prev_high = 0.0
        self._prev_prev_low = 0.0
        self._bar_count = 0.0

    def OnStarted(self, time):
        super(donchain_counter_channel_system_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self.channel_period
        self._lowest = Lowest()
        self._lowest.Length = self.channel_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return donchain_counter_channel_system_strategy()
