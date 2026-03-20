import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class daydream_strategy(Strategy):
    def __init__(self):
        super(daydream_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Donchian channel lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(daydream_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(daydream_strategy, self).OnStarted(time)
        self._has_prev = False
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high_val = float(highest)
        low_val = float(lowest)
        mid = (high_val + low_val) / 2.0
        if not self._has_prev:
            self._prev_close = close
            self._prev_mid = mid
            self._has_prev = True
            return
        if self._prev_close <= self._prev_mid and close > mid and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_close >= self._prev_mid and close < mid and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return daydream_strategy()
