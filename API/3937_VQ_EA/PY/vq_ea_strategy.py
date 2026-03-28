import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vq_ea_strategy(Strategy):
    """EMA trend filter with Momentum zero-cross for entries and cooldown."""
    def __init__(self):
        super(vq_ea_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._mom_period = self.Param("MomentumPeriod", 14).SetDisplay("Momentum", "Momentum period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vq_ea_strategy, self).OnReseted()
        self._prev_mom = 0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(vq_ea_strategy, self).OnStarted(time)
        self._prev_mom = 0
        self._has_prev = False
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        mom = Momentum()
        mom.Length = self._mom_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, mom, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val, mom_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ev = float(ema_val)
        mv = float(mom_val)
        close = float(candle.ClosePrice)

        if not self._has_prev:
            self._prev_mom = mv
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_mom = mv
            return

        if close > ev and self._prev_mom <= 0 and mv > 0 and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 2
        elif close < ev and self._prev_mom >= 0 and mv < 0 and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 2

        self._prev_mom = mv

    def CreateClone(self):
        return vq_ea_strategy()
