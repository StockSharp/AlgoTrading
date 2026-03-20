import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class fast_slow_rvi_crossover_strategy(Strategy):
    def __init__(self):
        super(fast_slow_rvi_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._rvi_period = self.Param("RviPeriod", 20)

        self._prev_avg = 0.0
        self._prev_sig = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RviPeriod(self):
        return self._rvi_period.Value

    @RviPeriod.setter
    def RviPeriod(self, value):
        self._rvi_period.Value = value

    def OnReseted(self):
        super(fast_slow_rvi_crossover_strategy, self).OnReseted()
        self._prev_avg = 0.0
        self._prev_sig = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(fast_slow_rvi_crossover_strategy, self).OnStarted(time)
        self._prev_avg = 0.0
        self._prev_sig = 0.0
        self._has_prev = False

        rvi = ExponentialMovingAverage()
        rvi.Length = self.RviPeriod
        signal = ExponentialMovingAverage()
        signal.Length = self.RviPeriod * 2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rvi, signal, self._process_candle).Start()

    def _process_candle(self, candle, avg_value, sig_value):
        if candle.State != CandleStates.Finished:
            return

        avg_val = float(avg_value)
        sig_val = float(sig_value)

        if self._has_prev:
            long_signal = self._prev_avg <= self._prev_sig and avg_val > sig_val
            short_signal = self._prev_avg >= self._prev_sig and avg_val < sig_val

            if long_signal and self.Position <= 0:
                self.BuyMarket()
            elif short_signal and self.Position >= 0:
                self.SellMarket()

        self._prev_avg = avg_val
        self._prev_sig = sig_val
        self._has_prev = True

    def CreateClone(self):
        return fast_slow_rvi_crossover_strategy()
