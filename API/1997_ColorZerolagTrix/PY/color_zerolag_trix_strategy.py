import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Trix
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_trix_strategy(Strategy):

    def __init__(self):
        super(color_zerolag_trix_strategy, self).__init__()

        self._trix_period = self.Param("TrixPeriod", 14) \
            .SetDisplay("TRIX Period", "TRIX calculation period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_trix = 0.0
        self._prev_prev_trix = 0.0
        self._count = 0

    @property
    def TrixPeriod(self):
        return self._trix_period.Value

    @TrixPeriod.setter
    def TrixPeriod(self, value):
        self._trix_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(color_zerolag_trix_strategy, self).OnStarted(time)

        trix = Trix()
        trix.Length = self.TrixPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(trix, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, trix_value):
        if candle.State != CandleStates.Finished:
            return

        trix_val = float(trix_value)
        self._count += 1

        if self._count < 3:
            self._prev_prev_trix = self._prev_trix
            self._prev_trix = trix_val
            return

        turn_up = self._prev_trix < self._prev_prev_trix and trix_val > self._prev_trix
        turn_down = self._prev_trix > self._prev_prev_trix and trix_val < self._prev_trix

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_trix = self._prev_trix
        self._prev_trix = trix_val

    def OnReseted(self):
        super(color_zerolag_trix_strategy, self).OnReseted()
        self._prev_trix = 0.0
        self._prev_prev_trix = 0.0
        self._count = 0

    def CreateClone(self):
        return color_zerolag_trix_strategy()
