import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rrs_randomness_strategy(Strategy):
    def __init__(self):
        super(rrs_randomness_strategy, self).__init__()
        self._tp_points = self.Param("TakeProfitPoints", 2000.0).SetNotNegative().SetDisplay("Take Profit", "TP in price steps", "Protection")
        self._sl_points = self.Param("StopLossPoints", 3000.0).SetNotNegative().SetDisplay("Stop Loss", "SL in price steps", "Protection")
        self._trailing_start = self.Param("TrailingStartPoints", 1500.0).SetNotNegative().SetDisplay("Trailing Start", "Profit to enable trailing", "Protection")
        self._trailing_gap = self.Param("TrailingGapPoints", 1000.0).SetNotNegative().SetDisplay("Trailing Gap", "Trailing offset", "Protection")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rrs_randomness_strategy, self).OnReseted()
        self._trailing_stop = None
        self._open_long_next = True
        self._entry_price = 0

    def OnStarted(self, time):
        super(rrs_randomness_strategy, self).OnStarted(time)
        self._trailing_stop = None
        self._open_long_next = True
        self._entry_price = 0
        self._step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._step = float(self.Security.PriceStep)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._apply_protection(close)
        self._apply_trailing(close)

        if self.Position != 0:
            return

        if self._open_long_next:
            self.BuyMarket()
            self._entry_price = close
        else:
            self.SellMarket()
            self._entry_price = close

        self._open_long_next = not self._open_long_next

    def _apply_protection(self, price):
        if self.Position == 0 or self._entry_price <= 0:
            return
        step = self._step

        if self.Position > 0:
            if self._sl_points.Value > 0:
                sl = self._entry_price - self._sl_points.Value * step
                if price <= sl:
                    self.SellMarket()
                    self._trailing_stop = None
                    return
            if self._tp_points.Value > 0:
                tp = self._entry_price + self._tp_points.Value * step
                if price >= tp:
                    self.SellMarket()
                    self._trailing_stop = None
        elif self.Position < 0:
            if self._sl_points.Value > 0:
                sl = self._entry_price + self._sl_points.Value * step
                if price >= sl:
                    self.BuyMarket()
                    self._trailing_stop = None
                    return
            if self._tp_points.Value > 0:
                tp = self._entry_price - self._tp_points.Value * step
                if price <= tp:
                    self.BuyMarket()
                    self._trailing_stop = None

    def _apply_trailing(self, price):
        if self.Position == 0 or self._trailing_gap.Value <= 0 or self._trailing_start.Value <= 0:
            return
        step = self._step
        gap = self._trailing_gap.Value * step
        trigger = (self._trailing_start.Value + self._trailing_gap.Value) * step

        if self.Position > 0:
            profit = price - self._entry_price
            if profit > trigger:
                candidate = price - gap
                if self._trailing_stop is None or candidate > self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price <= self._trailing_stop:
                self.SellMarket()
                self._trailing_stop = None
        elif self.Position < 0:
            profit = self._entry_price - price
            if profit > trigger:
                candidate = price + gap
                if self._trailing_stop is None or candidate < self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price >= self._trailing_stop:
                self.BuyMarket()
                self._trailing_stop = None

    def CreateClone(self):
        return rrs_randomness_strategy()
