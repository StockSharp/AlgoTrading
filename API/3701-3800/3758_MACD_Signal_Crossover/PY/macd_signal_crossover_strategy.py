import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_signal_crossover_strategy(Strategy):
    """Simple MACD/signal line crossover strategy. Buys when MACD crosses above signal,
    sells when MACD crosses below signal."""

    def __init__(self):
        super(macd_signal_crossover_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 23) \
            .SetDisplay("Fast Period", "Fast EMA period for MACD", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 40) \
            .SetDisplay("Slow Period", "Slow EMA period for MACD", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 8) \
            .SetDisplay("Signal Period", "Signal line period for MACD", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")

        self._prev_macd_above_signal = False
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

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    def OnReseted(self):
        super(macd_signal_crossover_strategy, self).OnReseted()
        self._prev_macd_above_signal = False
        self._has_prev = False

    def OnStarted2(self, time):
        super(macd_signal_crossover_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFinal:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal

        if macd_line is None or signal_line is None:
            return

        is_macd_above_signal = float(macd_line) > float(signal_line)

        if not self._has_prev:
            self._has_prev = True
            self._prev_macd_above_signal = is_macd_above_signal
            return

        crossed_above = is_macd_above_signal and not self._prev_macd_above_signal
        crossed_below = not is_macd_above_signal and self._prev_macd_above_signal

        if crossed_above:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif crossed_below:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_macd_above_signal = is_macd_above_signal

    def CreateClone(self):
        return macd_signal_crossover_strategy()
