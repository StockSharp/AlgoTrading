import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class template_e_aby_market_strategy(Strategy):
    def __init__(self):
        super(template_e_aby_market_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._fast_period = self.Param("FastPeriod", 12)
        self._slow_period = self.Param("SlowPeriod", 26)
        self._signal_period = self.Param("SignalPeriod", 9)

        self._prev_macd = 0.0
        self._prev_signal = 0.0
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
    def SignalPeriod(self):
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    def OnReseted(self):
        super(template_e_aby_market_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(template_e_aby_market_strategy, self).OnStarted(time)
        self._has_prev = False

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
        signal = macd_value.Signal
        if macd_line is None or signal is None:
            return

        macd_line = float(macd_line)
        signal = float(signal)

        if self._has_prev:
            prev_hist = self._prev_macd - self._prev_signal
            curr_hist = macd_line - signal

            if prev_hist <= 0 and curr_hist > 0 and self.Position <= 0:
                self.BuyMarket()
            elif prev_hist >= 0 and curr_hist < 0 and self.Position >= 0:
                self.SellMarket()

        self._prev_macd = macd_line
        self._prev_signal = signal
        self._has_prev = True

    def CreateClone(self):
        return template_e_aby_market_strategy()
