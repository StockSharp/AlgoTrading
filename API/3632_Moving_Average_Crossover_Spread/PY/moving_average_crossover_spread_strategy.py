import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class moving_average_crossover_spread_strategy(Strategy):
    def __init__(self):
        super(moving_average_crossover_spread_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 20)
        self._slow_period = self.Param("SlowPeriod", 50)

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    def OnReseted(self):
        super(moving_average_crossover_spread_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(moving_average_crossover_spread_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_slow = None

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastPeriod
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return moving_average_crossover_spread_strategy()
