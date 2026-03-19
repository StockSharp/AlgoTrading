import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class aurora_divergence_strategy(Strategy):
    """
    Aurora Divergence strategy using dual EMA crossover for trend timing.
    Enters long on golden cross, short on death cross.
    """

    def __init__(self):
        super(aurora_divergence_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 450) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def FastEmaPeriod(self): return self._fast_ema_period.Value
    @FastEmaPeriod.setter
    def FastEmaPeriod(self, v): self._fast_ema_period.Value = v
    @property
    def SlowEmaPeriod(self): return self._slow_ema_period.Value
    @SlowEmaPeriod.setter
    def SlowEmaPeriod(self, v): self._slow_ema_period.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(aurora_divergence_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(aurora_divergence_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_fast == 0.0 or self._prev_slow == 0.0:
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            return

        if self._prev_fast <= self._prev_slow and fast_value > slow_value and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_value < slow_value and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return aurora_divergence_strategy()
