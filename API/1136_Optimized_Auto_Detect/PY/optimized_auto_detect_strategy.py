import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class optimized_auto_detect_strategy(Strategy):
    def __init__(self):
        super(optimized_auto_detect_strategy, self).__init__()
        self._short_ma_period = self.Param("ShortMaPeriod", 14) \
            .SetGreaterThanZero()
        self._long_ma_period = self.Param("LongMaPeriod", 40) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(optimized_auto_detect_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._initialized = False
        self._last_signal_ticks = 0

    def OnStarted2(self, time):
        super(optimized_auto_detect_strategy, self).OnStarted2(time)
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._initialized = False
        self._last_signal_ticks = 0
        self._short_sma = SimpleMovingAverage()
        self._short_sma.Length = self._short_ma_period.Value
        self._long_sma = SimpleMovingAverage()
        self._long_sma.Length = self._long_ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._short_sma, self._long_sma, self.OnProcess).Start()

    def OnProcess(self, candle, s, l):
        if candle.State != CandleStates.Finished:
            return
        if not self._short_sma.IsFormed or not self._long_sma.IsFormed:
            return
        sv = float(s)
        lv = float(l)
        if not self._initialized:
            self._prev_short = sv
            self._prev_long = lv
            self._initialized = True
            return
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks >= cooldown_ticks:
            if self._prev_short <= self._prev_long and sv > lv and self.Position <= 0:
                self.BuyMarket()
                self._last_signal_ticks = current_ticks
            elif self._prev_short >= self._prev_long and sv < lv and self.Position >= 0:
                self.SellMarket()
                self._last_signal_ticks = current_ticks
        self._prev_short = sv
        self._prev_long = lv

    def CreateClone(self):
        return optimized_auto_detect_strategy()
