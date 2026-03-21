import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class parabolic_sar_cross_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_cross_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(parabolic_sar_cross_strategy, self).OnReseted()
        self._prev_sar = None
        self._prev_close = None

    def OnStarted(self, time):
        super(parabolic_sar_cross_strategy, self).OnStarted(time)
        self._prev_sar = None
        self._prev_close = None

        sar = ParabolicSar()
        sar.Acceleration = 0.02
        sar.AccelerationStep = 0.02
        sar.AccelerationMax = 0.2

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sar, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sar_val):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        if self._prev_sar is not None and self._prev_close is not None:
            cross_up = self._prev_close <= self._prev_sar and close > sar_val
            cross_down = self._prev_close >= self._prev_sar and close < sar_val

            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()

        self._prev_sar = sar_val
        self._prev_close = close

    def CreateClone(self):
        return parabolic_sar_cross_strategy()
