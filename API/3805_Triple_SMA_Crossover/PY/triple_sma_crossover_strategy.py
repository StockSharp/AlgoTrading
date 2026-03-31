import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class triple_sma_crossover_strategy(Strategy):
    """Triple SMA crossover strategy.
    Goes long when fast > medium > slow, short when fast < medium < slow.
    Exits when fast crosses medium in opposite direction."""

    def __init__(self):
        super(triple_sma_crossover_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._medium_period = self.Param("MediumPeriod", 10) \
            .SetDisplay("Medium SMA", "Medium SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_fast = 0.0
        self._prev_med = 0.0
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
    def MediumPeriod(self):
        return self._medium_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(triple_sma_crossover_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_med = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(triple_sma_crossover_strategy, self).OnStarted2(time)

        self._has_prev = False

        fast = SimpleMovingAverage()
        fast.Length = self.FastPeriod
        medium = SimpleMovingAverage()
        medium.Length = self.MediumPeriod
        slow = SimpleMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, medium, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast, med, slow):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast)
        med_val = float(med)
        slow_val = float(slow)

        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_med = med_val
            self._has_prev = True
            return

        # Bullish alignment: fast > medium > slow
        if fast_val > med_val and med_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Bearish alignment: fast < medium < slow
        elif fast_val < med_val and med_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long when fast crosses below medium
        elif self.Position > 0 and self._prev_fast >= self._prev_med and fast_val < med_val:
            self.SellMarket()
        # Exit short when fast crosses above medium
        elif self.Position < 0 and self._prev_fast <= self._prev_med and fast_val > med_val:
            self.BuyMarket()

        self._prev_fast = fast_val
        self._prev_med = med_val

    def CreateClone(self):
        return triple_sma_crossover_strategy()
