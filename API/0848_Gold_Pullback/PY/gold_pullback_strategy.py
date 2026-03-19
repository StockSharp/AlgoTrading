import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class gold_pullback_strategy(Strategy):
    """
    EMA crossover strategy.
    Buys when fast EMA crosses above slow EMA, sells when it crosses below.
    """

    def __init__(self):
        super(gold_pullback_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 120) \
            .SetDisplay("Fast Period", "Fast EMA period", "General")
        self._slow_period = self.Param("SlowPeriod", 450) \
            .SetDisplay("Slow Period", "Slow EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gold_pullback_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(gold_pullback_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast = float(fast_val)
        slow = float(slow_val)

        if self._prev_fast != 0.0 and self._prev_slow != 0.0:
            if self._prev_fast <= self._prev_slow and fast > slow:
                if self.Position <= 0:
                    self.BuyMarket()
            elif self._prev_fast >= self._prev_slow and fast < slow:
                if self.Position >= 0:
                    self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return gold_pullback_strategy()
