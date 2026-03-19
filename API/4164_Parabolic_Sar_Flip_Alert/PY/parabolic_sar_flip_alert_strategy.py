import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class parabolic_sar_flip_alert_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_flip_alert_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_length = self.Param("EmaLength", 20).SetDisplay("EMA Length", "Trend filter", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(parabolic_sar_flip_alert_strategy, self).OnReseted()
        self._prev_close = 0
        self._entry_price = 0

    def OnStarted(self, time):
        super(parabolic_sar_flip_alert_strategy, self).OnStarted(time)
        self._prev_close = 0
        self._entry_price = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if self._prev_close == 0 or atr_val <= 0:
            self._prev_close = close
            return

        if self.Position > 0:
            if close >= self._entry_price + atr_val * 2.5 or close <= self._entry_price - atr_val * 1.5 or close < ema_val:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if close <= self._entry_price - atr_val * 2.5 or close >= self._entry_price + atr_val * 1.5 or close > ema_val:
                self.BuyMarket()
                self._entry_price = 0

        if self.Position == 0:
            if close > ema_val and self._prev_close <= ema_val:
                self._entry_price = close
                self.BuyMarket()
            elif close < ema_val and self._prev_close >= ema_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_close = close

    def CreateClone(self):
        return parabolic_sar_flip_alert_strategy()
