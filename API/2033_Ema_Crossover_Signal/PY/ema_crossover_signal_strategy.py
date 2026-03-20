import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_crossover_signal_strategy(Strategy):

    def __init__(self):
        super(ema_crossover_signal_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast Period", "Length of the fast EMA", "EMA")
        self._slow_period = self.Param("SlowPeriod", 13) \
            .SetDisplay("Slow Period", "Length of the slow EMA", "EMA")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._is_initialized = False
        self._was_fast_above_slow = False

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

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ema_crossover_signal_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast_ema, slow_ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)

        if not self._is_initialized:
            self._was_fast_above_slow = fast > slow
            self._is_initialized = True
            return

        is_fast_above_slow = fast > slow

        if self._was_fast_above_slow != is_fast_above_slow:
            if is_fast_above_slow:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self.BuyMarket()
            else:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()

            self._was_fast_above_slow = is_fast_above_slow

    def OnReseted(self):
        super(ema_crossover_signal_strategy, self).OnReseted()
        self._is_initialized = False
        self._was_fast_above_slow = False

    def CreateClone(self):
        return ema_crossover_signal_strategy()
