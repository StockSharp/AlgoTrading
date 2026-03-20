import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class fit_ful13_time_gated_strategy(Strategy):
    def __init__(self):
        super(fit_ful13_time_gated_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 13) \
            .SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators")

        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

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

        self._highest = Highest()
        self._highest.Length = self.channel_period
        self._lowest = Lowest()
        self._lowest.Length = self.channel_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return fit_ful13_time_gated_strategy()
