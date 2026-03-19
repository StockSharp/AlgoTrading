import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class pipso_night_breakout_strategy(Strategy):
    def __init__(self):
        super(pipso_night_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")
        self._breakout_period = self.Param("BreakoutPeriod", 36).SetDisplay("Breakout Period", "Period for Highest/Lowest channel", "Indicators")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pipso_night_breakout_strategy, self).OnReseted()
        self._entry_price = 0
        self._prev_highest = 0
        self._prev_lowest = 0
        self._has_prev = False

    def OnStarted(self, time):
        super(pipso_night_breakout_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._prev_highest = 0
        self._prev_lowest = 0
        self._has_prev = False

        highest = Highest()
        highest.Length = self._breakout_period.Value
        lowest = Lowest()
        lowest.Length = self._breakout_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._breakout_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(highest, lowest, ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, highest_val, lowest_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        mid = (highest_val + lowest_val) / 2.0

        if self.Position > 0:
            if close < mid or (self._entry_price > 0 and close < self._entry_price * 0.98):
                self.SellMarket()
        elif self.Position < 0:
            if close > mid or (self._entry_price > 0 and close > self._entry_price * 1.02):
                self.BuyMarket()

        if self.Position == 0 and self._has_prev:
            if close > self._prev_highest and close > ema_val:
                self._entry_price = close
                self.BuyMarket()
            elif close < self._prev_lowest and close < ema_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_highest = highest_val
        self._prev_lowest = lowest_val
        self._has_prev = True

    def CreateClone(self):
        return pipso_night_breakout_strategy()
