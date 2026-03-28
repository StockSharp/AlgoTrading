import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rnd_trade_random_hold_strategy(Strategy):
    def __init__(self):
        super(rnd_trade_random_hold_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 14).SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 10).SetDisplay("Momentum", "Momentum period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rnd_trade_random_hold_strategy, self).OnReseted()
        self._prev_mom = 0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(rnd_trade_random_hold_strategy, self).OnStarted(time)
        self._prev_mom = 0
        self._has_prev = False
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        mom = Momentum()
        mom.Length = self._momentum_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, mom, self.OnProcess).Start()

    def OnProcess(self, candle, ema_val, mom_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_mom = mom_val
            self._has_prev = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_mom = mom_val
            return

        if close > ema_val and self._prev_mom <= 0 and mom_val > 0 and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 2
        elif close < ema_val and self._prev_mom >= 0 and mom_val < 0 and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 2

        self._prev_mom = mom_val

    def CreateClone(self):
        return rnd_trade_random_hold_strategy()
