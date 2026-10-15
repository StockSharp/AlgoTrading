import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Trix, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_trix_osma_strategy(Strategy):

    def __init__(self):
        super(color_zerolag_trix_osma_strategy, self).__init__()

        self._trix_period = self.Param("TrixPeriod", 14) \
            .SetDisplay("TRIX Period", "TRIX calculation period", "Indicator")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line EMA period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_osma = 0.0
        self._prev_prev_osma = 0.0
        self._count = 0

    @property
    def TrixPeriod(self):
        return self._trix_period.Value

    @TrixPeriod.setter
    def TrixPeriod(self, value):
        self._trix_period.Value = value

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
        super(color_zerolag_trix_osma_strategy, self).OnStarted2(time)

        trix = Trix()
        trix.Length = self.TrixPeriod
        signal = ExponentialMovingAverage()
        signal.Length = self.SignalPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(trix, signal, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, trix_value, signal_value):
        if candle.State != CandleStates.Finished:
            return

        osma = float(trix_value) - float(signal_value)
        self._count += 1

        if self._count < 3:
            self._prev_prev_osma = self._prev_osma
            self._prev_osma = osma
            return

        turn_up = self._prev_osma < self._prev_prev_osma and osma > self._prev_osma
        turn_down = self._prev_osma > self._prev_prev_osma and osma < self._prev_osma

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_osma = self._prev_osma
        self._prev_osma = osma

    def OnReseted(self):
        super(color_zerolag_trix_osma_strategy, self).OnReseted()
        self._prev_osma = 0.0
        self._prev_prev_osma = 0.0
        self._count = 0

    def CreateClone(self):
        return color_zerolag_trix_osma_strategy()
