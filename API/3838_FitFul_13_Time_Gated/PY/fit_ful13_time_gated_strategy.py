import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fit_ful13_time_gated_strategy(Strategy):
    def __init__(self):
        super(fit_ful13_time_gated_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 13) \
            .SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fit_ful13_time_gated_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(fit_ful13_time_gated_strategy, self).OnStarted(time)
        self._has_prev = False
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, ema, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest, ema):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high_val = float(highest)
        low_val = float(lowest)
        ema_val = float(ema)
        mid = (high_val + low_val) / 2.0
        if not self._has_prev:
            self._prev_close = close
            self._prev_mid = mid
            self._has_prev = True
            return
        if self._prev_close <= self._prev_mid and close > mid and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_close >= self._prev_mid and close < mid and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return fit_ful13_time_gated_strategy()
