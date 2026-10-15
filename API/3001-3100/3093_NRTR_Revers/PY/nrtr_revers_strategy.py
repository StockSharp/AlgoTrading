import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class nrtr_revers_strategy(Strategy):
    def __init__(self):
        super(nrtr_revers_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR averaging period", "Indicator")
        self._volatility_multiplier = self.Param("VolatilityMultiplier", 5.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier for bands", "Indicator")
        self._ema_period = self.Param("EmaPeriod", 100).SetGreaterThanZero().SetDisplay("EMA Period", "EMA trend center period", "Indicator")

    @property
    def CandleType(self):
        return tf(5)

    def OnReseted(self):
        super(nrtr_revers_strategy, self).OnReseted()
        self._entry_price = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(nrtr_revers_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._cooldown = 0

        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_period.Value
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_period.Value

        sub = self.SubscribeCandles(tf(5))
        sub.Bind(self._atr, self._ema, self.OnProcess).Start()

    def OnProcess(self, candle, atr_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._atr.IsFormed or not self._ema.IsFormed:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = candle.ClosePrice
        band_offset = atr_val * self._volatility_multiplier.Value
        upper_band = ema_val + band_offset
        lower_band = ema_val - band_offset

        if close > upper_band and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 50
        elif close < lower_band and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 50
        elif self.Position > 0 and close < ema_val:
            self.SellMarket()
            self._entry_price = 0
            self._cooldown = 50
        elif self.Position < 0 and close > ema_val:
            self.BuyMarket()
            self._entry_price = 0
            self._cooldown = 50

    def CreateClone(self):
        return nrtr_revers_strategy()
