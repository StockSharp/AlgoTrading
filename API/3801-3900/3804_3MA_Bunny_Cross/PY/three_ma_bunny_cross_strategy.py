import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_ma_bunny_cross_strategy(Strategy):
    """3MA Bunny Cross strategy using weighted moving average crossover.
    Goes long on fast WMA crossing above slow WMA, short on opposite."""

    def __init__(self):
        super(three_ma_bunny_cross_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast WMA", "Fast weighted MA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow WMA", "Slow weighted MA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(three_ma_bunny_cross_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(three_ma_bunny_cross_strategy, self).OnStarted2(time)

        self._has_prev = False

        fast = WeightedMovingAverage()
        fast.Length = self.FastPeriod
        slow = WeightedMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast)
        slow_val = float(slow)

        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return three_ma_bunny_cross_strategy()
