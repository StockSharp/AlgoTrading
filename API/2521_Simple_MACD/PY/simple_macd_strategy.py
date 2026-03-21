import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class simple_macd_strategy(Strategy):
    def __init__(self):
        super(simple_macd_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("MACD Fast Period", "Fast EMA length", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26).SetDisplay("MACD Slow Period", "Slow EMA length", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("MACD Signal Period", "Signal EMA length", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(simple_macd_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_prev_macd = None
        self._prev_slope = None

    def OnStarted(self, time):
        super(simple_macd_strategy, self).OnStarted(time)
        self._prev_macd = None
        self._prev_prev_macd = None
        self._prev_slope = None

        self._macd = MovingAverageConvergenceDivergence()
        self._macd.ShortMa.Length = self._fast_period.Value
        self._macd.LongMa.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._macd, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._macd.IsFormed or not macd_val.IsFinal:
            return

        macd_line = macd_val.ToDecimal()

        if self._prev_macd is None:
            self._prev_macd = macd_line
            return

        if self._prev_prev_macd is None:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd_line
            return

        prev = float(self._prev_macd)
        prev_prev = float(self._prev_prev_macd)

        if prev > prev_prev:
            current_slope = 1
        elif prev < prev_prev:
            current_slope = -1
        else:
            current_slope = 0

        if current_slope == 0:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd_line
            return

        if self._prev_slope == current_slope:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd_line
            return

        if current_slope > 0:
            self.BuyMarket()
        else:
            self.SellMarket()

        self._prev_slope = current_slope
        self._prev_prev_macd = self._prev_macd
        self._prev_macd = macd_line

    def CreateClone(self):
        return simple_macd_strategy()
