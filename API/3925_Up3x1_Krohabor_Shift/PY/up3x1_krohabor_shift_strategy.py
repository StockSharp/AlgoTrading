import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class up3x1_krohabor_shift_strategy(Strategy):
    def __init__(self):
        super(up3x1_krohabor_shift_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Channel lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 14) \
            .SetDisplay("Channel Period", "Channel lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Channel Period", "Channel lookback", "Indicators")

        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(up3x1_krohabor_shift_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(up3x1_krohabor_shift_strategy, self).OnStarted(time)

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
        return up3x1_krohabor_shift_strategy()
