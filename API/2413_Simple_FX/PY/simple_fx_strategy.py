import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class simple_fx_strategy(Strategy):
    def __init__(self):
        super(simple_fx_strategy, self).__init__()
        self._long_ma = self.Param("LongMaPeriod", 200).SetGreaterThanZero().SetDisplay("Long MA Period", "Period of long EMA", "Parameters")
        self._short_ma = self.Param("ShortMaPeriod", 50).SetGreaterThanZero().SetDisplay("Short MA Period", "Period of short EMA", "Parameters")
        self._sl = self.Param("StopLoss", 30).SetDisplay("Stop Loss", "SL in price steps", "Risk")
        self._tp = self.Param("TakeProfit", 50).SetDisplay("Take Profit", "TP in price steps", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(simple_fx_strategy, self).OnReseted()
        self._entry_price = 0
        self._last_trend = 0

    def OnStarted(self, time):
        super(simple_fx_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._last_trend = 0
        self._step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._step = float(self.Security.PriceStep)

        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_ma.Value
        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_ma.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(long_ema, short_ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, long_ema)
            self.DrawIndicator(area, short_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, long_val, short_val):
        if candle.State != CandleStates.Finished:
            return

        trend = 0
        if short_val > long_val:
            trend = 1
        elif short_val < long_val:
            trend = -1

        if trend == 0:
            return

        close = float(candle.ClosePrice)

        if trend != self._last_trend:
            if trend == 1:
                self.BuyMarket()
                self._entry_price = close
            else:
                self.SellMarket()
                self._entry_price = close
            self._last_trend = trend
            return

        if self.Position == 0 or self._entry_price == 0:
            return

        step = self._step
        sl_delta = self._sl.Value * step
        tp_delta = self._tp.Value * step

        if self.Position > 0:
            if self._sl.Value > 0 and close <= self._entry_price - sl_delta:
                self.SellMarket()
                self._last_trend = 0
                self._entry_price = 0
                return
            if self._tp.Value > 0 and close >= self._entry_price + tp_delta:
                self.SellMarket()
                self._last_trend = 0
                self._entry_price = 0
        elif self.Position < 0:
            if self._sl.Value > 0 and close >= self._entry_price + sl_delta:
                self.BuyMarket()
                self._last_trend = 0
                self._entry_price = 0
                return
            if self._tp.Value > 0 and close <= self._entry_price - tp_delta:
                self.BuyMarket()
                self._last_trend = 0
                self._entry_price = 0

    def CreateClone(self):
        return simple_fx_strategy()
