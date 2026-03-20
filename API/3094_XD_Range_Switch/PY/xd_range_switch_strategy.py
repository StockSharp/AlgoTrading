import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class xd_range_switch_strategy(Strategy):
    def __init__(self):
        super(xd_range_switch_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 200) \
            .SetDisplay("Channel Period", "Lookback for highest/lowest channel", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Channel Period", "Lookback for highest/lowest channel", "Indicator")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Channel Period", "Lookback for highest/lowest channel", "Indicator")

        self._highest = None
        self._lowest = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnReseted(self):
        super(xd_range_switch_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(xd_range_switch_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = self.channel_period
        self.__lowest = Lowest()
        self.__lowest.Length = self.channel_period

        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return xd_range_switch_strategy()
