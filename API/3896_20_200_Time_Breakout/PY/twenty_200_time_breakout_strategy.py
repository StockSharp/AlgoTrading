import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class twenty_200_time_breakout_strategy(Strategy):
    def __init__(self):
        super(twenty_200_time_breakout_strategy, self).__init__()
        self._short_period = self.Param("ShortPeriod", 20) \
            .SetDisplay("Short SMA", "Short SMA period", "Indicators")
        self._long_period = self.Param("LongPeriod", 200) \
            .SetDisplay("Long SMA", "Long SMA period", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 100) \
            .SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def short_period(self):
        return self._short_period.Value

    @property
    def long_period(self):
        return self._long_period.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(twenty_200_time_breakout_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(twenty_200_time_breakout_strategy, self).OnStarted(time)
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

        short_sma = SimpleMovingAverage()
        short_sma.Length = self.short_period
        long_sma = SimpleMovingAverage()
        long_sma.Length = self.long_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_sma, long_sma, self.process_candle).Start()

    def process_candle(self, candle, short_sma, long_sma):
        if candle.State != CandleStates.Finished:
            return

        short_val = float(short_sma)
        long_val = float(long_sma)

        if not self._has_prev:
            self._prev_short = short_val
            self._prev_long = long_val
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_short = short_val
            self._prev_long = long_val
            return

        if self._prev_short <= self._prev_long and short_val > long_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif self._prev_short >= self._prev_long and short_val < long_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles

        self._prev_short = short_val
        self._prev_long = long_val

    def CreateClone(self):
        return twenty_200_time_breakout_strategy()
