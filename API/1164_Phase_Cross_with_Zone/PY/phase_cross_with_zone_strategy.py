import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class phase_cross_with_zone_strategy(Strategy):
    def __init__(self):
        super(phase_cross_with_zone_strategy, self).__init__()
        self._length = self.Param("Length", 20).SetGreaterThanZero().SetDisplay("Length", "Smoothing length", "General")
        self._offset = self.Param("Offset", 0.5).SetDisplay("Offset", "Phase offset", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(phase_cross_with_zone_strategy, self).OnReseted()
        self._prev_lead = 0
        self._prev_lag = 0
        self._prev_init = False

    def OnStarted(self, time):
        super(phase_cross_with_zone_strategy, self).OnStarted(time)
        self._prev_lead = 0
        self._prev_lag = 0
        self._prev_init = False

        sma = SimpleMovingAverage()
        sma.Length = self._length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._length.Value * 2

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        lead = sma_val + self._offset.Value
        lag = ema_val - self._offset.Value

        if self._prev_init:
            crossed_up = self._prev_lead <= self._prev_lag and lead > lag
            crossed_down = self._prev_lead >= self._prev_lag and lead < lag

            if crossed_up and self.Position <= 0:
                self.BuyMarket()
            elif crossed_down and self.Position >= 0:
                self.SellMarket()

        self._prev_lead = lead
        self._prev_lag = lag
        self._prev_init = True

    def CreateClone(self):
        return phase_cross_with_zone_strategy()
