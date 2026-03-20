import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class donchian_scalper_strategy(Strategy):
    def __init__(self):
        super(donchian_scalper_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 50) \
            .SetDisplay("Channel Period", "Donchian channel lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Channel Period", "Donchian channel lookback", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_scalper_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(donchian_scalper_strategy, self).OnStarted(time)

        self._donchian = DonchianChannels()
        self._donchian.Length = self.channel_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.channel_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return donchian_scalper_strategy()
