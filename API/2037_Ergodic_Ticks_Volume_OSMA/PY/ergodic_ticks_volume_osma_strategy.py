import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, MovingAverageConvergenceDivergenceSignal, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ergodic_ticks_volume_osma_strategy(Strategy):

    def __init__(self):
        super(ergodic_ticks_volume_osma_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal EMA", "Signal EMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Timeframe", "Timeframe", "General")

        self._prev_hist = 0.0
        self._prev_prev_hist = 0.0
        self._candle_count = 0

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @SignalLength.setter
    def SignalLength(self, value):
        self._signal_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ergodic_ticks_volume_osma_strategy, self).OnStarted(time)

        inner_macd = MovingAverageConvergenceDivergence()
        inner_macd.ShortMa.Length = self.FastLength
        inner_macd.LongMa.Length = self.SlowLength

        signal_ema = ExponentialMovingAverage()
        signal_ema.Length = self.SignalLength

        macd = MovingAverageConvergenceDivergenceSignal(inner_macd, signal_ema)

        self.SubscribeCandles(self.CandleType) \
            .BindEx(macd, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = value.Macd
        signal_val = value.Signal

        if macd_val is None or signal_val is None:
            return

        hist = float(macd_val) - float(signal_val)

        self._candle_count += 1
        if self._candle_count <= 2:
            self._prev_prev_hist = self._prev_hist
            self._prev_hist = hist
            return

        rising = self._prev_hist >= self._prev_prev_hist and hist >= self._prev_hist
        falling = self._prev_hist <= self._prev_prev_hist and hist <= self._prev_hist

        if rising and self.Position <= 0:
            self.BuyMarket()
        elif falling and self.Position >= 0:
            self.SellMarket()

        self._prev_prev_hist = self._prev_hist
        self._prev_hist = hist

    def OnReseted(self):
        super(ergodic_ticks_volume_osma_strategy, self).OnReseted()
        self._prev_hist = 0.0
        self._prev_prev_hist = 0.0
        self._candle_count = 0

    def CreateClone(self):
        return ergodic_ticks_volume_osma_strategy()
