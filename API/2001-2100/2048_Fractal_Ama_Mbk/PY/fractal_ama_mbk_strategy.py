import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import FractalAdaptiveMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fractal_ama_mbk_strategy(Strategy):

    def __init__(self):
        super(fractal_ama_mbk_strategy, self).__init__()

        self._frama_period = self.Param("FramaPeriod", 18) \
            .SetDisplay("FRAMA Period", "Period for Fractal Adaptive Moving Average", "Indicator")
        self._signal_period = self.Param("SignalPeriod", 18) \
            .SetDisplay("Signal EMA Period", "Period for signal EMA", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_frama = 0.0
        self._prev_signal = 0.0
        self._is_first = True

    @property
    def FramaPeriod(self):
        return self._frama_period.Value

    @FramaPeriod.setter
    def FramaPeriod(self, value):
        self._frama_period.Value = value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(fractal_ama_mbk_strategy, self).OnStarted2(time)

        self._is_first = True

        frama = FractalAdaptiveMovingAverage()
        frama.Length = self.FramaPeriod
        signal = ExponentialMovingAverage()
        signal.Length = self.SignalPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(frama, signal, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, frama_value, signal_value):
        if candle.State != CandleStates.Finished:
            return

        frama = float(frama_value)
        signal = float(signal_value)

        if self._is_first:
            self._prev_frama = frama
            self._prev_signal = signal
            self._is_first = False
            return

        was_above = self._prev_frama > self._prev_signal
        is_above = frama > signal

        if not was_above and is_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif was_above and not is_above and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_frama = frama
        self._prev_signal = signal

    def OnReseted(self):
        super(fractal_ama_mbk_strategy, self).OnReseted()
        self._prev_frama = 0.0
        self._prev_signal = 0.0
        self._is_first = True

    def CreateClone(self):
        return fractal_ama_mbk_strategy()
