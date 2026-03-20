import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_cross_contest_hedged_ladder_strategy(Strategy):
    def __init__(self):
        super(ema_cross_contest_hedged_ladder_strategy, self).__init__()

        self._short_period = self.Param("ShortPeriod", 9) \
            .SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._long_period = self.Param("LongPeriod", 21) \
            .SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Short EMA", "Short EMA period", "Indicators")

        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_cross_contest_hedged_ladder_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ema_cross_contest_hedged_ladder_strategy, self).OnStarted(time)

        self._short_ema = ExponentialMovingAverage()
        self._short_ema.Length = self.short_period
        self._long_ema = ExponentialMovingAverage()
        self._long_ema.Length = self.long_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._short_ema, self._long_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ema_cross_contest_hedged_ladder_strategy()
