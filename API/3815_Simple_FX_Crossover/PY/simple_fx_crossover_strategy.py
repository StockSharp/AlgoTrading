import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_fx_crossover_strategy(Strategy):
    def __init__(self):
        super(simple_fx_crossover_strategy, self).__init__()
        self._short_period = self.Param("ShortPeriod", 10) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._long_period = self.Param("LongPeriod", 30) \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    @property
    def short_period(self):
        return self._short_period.Value

    @property
    def long_period(self):
        return self._long_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(simple_fx_crossover_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(simple_fx_crossover_strategy, self).OnStarted2(time)
        self._has_prev = False
        fast = SimpleMovingAverage()
        fast.Length = self.short_period
        slow = SimpleMovingAverage()
        slow.Length = self.long_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast)
        slow_val = float(slow)
        if not self._has_prev:
            self._prev_short = fast_val
            self._prev_long = slow_val
            self._has_prev = True
            return
        cross_up = self._prev_short <= self._prev_long and fast_val > slow_val
        cross_down = self._prev_short >= self._prev_long and fast_val < slow_val
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_short = fast_val
        self._prev_long = slow_val

    def CreateClone(self):
        return simple_fx_crossover_strategy()
