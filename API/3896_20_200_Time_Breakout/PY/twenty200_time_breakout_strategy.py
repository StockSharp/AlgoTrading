import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class twenty200_time_breakout_strategy(Strategy):
    def __init__(self):
        super(twenty200_time_breakout_strategy, self).__init__()

        self._short_period = self.Param("ShortPeriod", 20) \
            .SetDisplay("Short SMA", "Short SMA period", "Indicators")
        self._long_period = self.Param("LongPeriod", 200) \
            .SetDisplay("Short SMA", "Short SMA period", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 100) \
            .SetDisplay("Short SMA", "Short SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Short SMA", "Short SMA period", "Indicators")

        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(twenty200_time_breakout_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    def OnStarted(self, time):
        super(twenty200_time_breakout_strategy, self).OnStarted(time)

        self._short_sma = SimpleMovingAverage()
        self._short_sma.Length = self.short_period
        self._long_sma = SimpleMovingAverage()
        self._long_sma.Length = self.long_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._short_sma, self._long_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return twenty200_time_breakout_strategy()
