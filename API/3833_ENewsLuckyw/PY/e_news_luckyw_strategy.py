import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, Momentum
from StockSharp.Algo.Strategies import Strategy


class e_news_luckyw_strategy(Strategy):
    def __init__(self):
        super(e_news_luckyw_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 15) \
            .SetDisplay("Channel Period", "Highest/Lowest period", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 10) \
            .SetDisplay("Momentum Period", "Momentum period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def momentum_period(self):
        return self._momentum_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(e_news_luckyw_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(e_news_luckyw_strategy, self).OnStarted2(time)
        self._has_prev = False
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        momentum = Momentum()
        momentum.Length = self.momentum_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, momentum, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest, momentum):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high_val = float(highest)
        low_val = float(lowest)
        mom_val = float(momentum)
        mid = (high_val + low_val) / 2.0
        if not self._has_prev:
            self._prev_close = close
            self._prev_mid = mid
            self._has_prev = True
            return
        if self._prev_close <= self._prev_mid and close > mid and mom_val > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_close >= self._prev_mid and close < mid and mom_val < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return e_news_luckyw_strategy()
