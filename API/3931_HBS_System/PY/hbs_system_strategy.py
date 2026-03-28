import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hbs_system_strategy(Strategy):
    """
    HBS System: EMA crossover with cooldown.
    Buys on fast EMA crossing above slow EMA, sells on reverse.
    """

    def __init__(self):
        super(hbs_system_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 13) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hbs_system_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(hbs_system_strategy, self).OnStarted(time)

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast = float(fast_val)
        slow = float(slow_val)

        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 2
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 2

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return hbs_system_strategy()
