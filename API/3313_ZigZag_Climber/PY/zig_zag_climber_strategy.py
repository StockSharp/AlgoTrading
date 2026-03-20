import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class zig_zag_climber_strategy(Strategy):
    def __init__(self):
        super(zig_zag_climber_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._channel_period = self.Param("ChannelPeriod", 12) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zig_zag_climber_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(zig_zag_climber_strategy, self).OnStarted(time)

        self._high = Highest()
        self._high.Length = self.channel_period
        self._low = Lowest()
        self._low.Length = self.channel_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._high, self._low, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return zig_zag_climber_strategy()
