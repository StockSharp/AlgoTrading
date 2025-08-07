import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class tendency_ema_rsi_strategy(Strategy):
    """EMA crossover with RSI filter and trend confirmation."""

    def __init__(self):
        super(tendency_ema_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")

        self._ema_a_length = self.Param("EmaALength", 9) \
            .SetDisplay("EMA A Length", "Fast EMA", "Moving Averages")

        self._ema_b_length = self.Param("EmaBLength", 21) \
            .SetDisplay("EMA B Length", "Medium EMA", "Moving Averages")

        self._ema_c_length = self.Param("EmaCLength", 50) \
            .SetDisplay("EMA C Length", "Slow EMA", "Moving Averages")

        self._show_long = self.Param("ShowLong", True) \
            .SetDisplay("Long Entries", "Enable long entries", "Strategy")

        self._show_short = self.Param("ShowShort", False) \
            .SetDisplay("Short Entries", "Enable short entries", "Strategy")

        self._close_after_x = self.Param("CloseAfterXBars", False) \
            .SetDisplay("Close After X Bars", "Close position after X bars if in profit", "Strategy")

        self._x_bars = self.Param("XBars", 5) \
            .SetDisplay("X Bars", "Number of bars to hold", "Strategy")

        self._rsi = None
        self._ema_a = None
        self._ema_b = None
        self._ema_c = None

        self._prev_ema_a = 0
        self._prev_ema_b = 0
        self._cross_over = False
        self._cross_under = False
        self._bars_in_pos = 0
        self._entry_price = 0

    # region properties
    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def ema_a_length(self):
        return self._ema_a_length.Value

    @property
    def ema_b_length(self):
        return self._ema_b_length.Value

    @property
    def ema_c_length(self):
        return self._ema_c_length.Value

    @property
    def show_long(self):
        return self._show_long.Value

    @property
    def show_short(self):
        return self._show_short.Value

    @property
    def close_after_x(self):
        return self._close_after_x.Value

    @property
    def x_bars(self):
        return self._x_bars.Value

    # endregion

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super().OnStarted(time)

        self._rsi = RelativeStrengthIndex(Length=self.rsi_length)
        self._ema_a = ExponentialMovingAverage(Length=self.ema_a_length)
        self._ema_b = ExponentialMovingAverage(Length=self.ema_b_length)
        self._ema_c = ExponentialMovingAverage(Length=self.ema_c_length)

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._rsi, self._ema_a, self._ema_b, self._ema_c, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._ema_a)
            self.DrawIndicator(area, self._ema_b)
            self.DrawIndicator(area, self._ema_c)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, ema_a_val, ema_b_val, ema_c_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._ema_a.IsFormed or not self._ema_b.IsFormed or not self._ema_c.IsFormed:
            return

        price = candle.ClosePrice
        open_price = candle.OpenPrice

        if self._prev_ema_a and self._prev_ema_b:
            self._cross_over = self._prev_ema_a <= self._prev_ema_b and ema_a_val > ema_b_val
            self._cross_under = self._prev_ema_a >= self._prev_ema_b and ema_a_val < ema_b_val

        if self.Position != 0:
            self._bars_in_pos += 1
        else:
            self._bars_in_pos = 0
            self._entry_price = 0

        self.CheckEntry(candle, rsi_value, ema_a_val, ema_b_val, ema_c_val)
        self.CheckExit(candle, rsi_value, ema_a_val, ema_b_val, ema_c_val)

        self._prev_ema_a = ema_a_val
        self._prev_ema_b = ema_b_val

    def CheckEntry(self, candle, rsi_val, ema_a_val, ema_b_val, ema_c_val):
        price = candle.ClosePrice
        open_price = candle.OpenPrice

        if (self.show_long and self._cross_over and ema_a_val > ema_c_val and price > open_price and self.Position == 0):
            self._entry_price = open_price
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))

        if (self.show_short and self._cross_under and ema_a_val < ema_c_val and price < open_price and self.Position == 0):
            self._entry_price = open_price
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

    def CheckExit(self, candle, rsi_val, ema_a_val, ema_b_val, ema_c_val):
        price = candle.ClosePrice

        if self.Position > 0 and rsi_val > 70:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, abs(self.Position)))
            return

        if self.Position < 0 and rsi_val < 30:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, abs(self.Position)))
            return

        if self.close_after_x and self._entry_price != 0 and self._bars_in_pos >= self.x_bars:
            if self.Position > 0 and price > self._entry_price:
                self.RegisterOrder(self.CreateOrder(Sides.Sell, price, abs(self.Position)))
            elif self.Position < 0 and price < self._entry_price:
                self.RegisterOrder(self.CreateOrder(Sides.Buy, price, abs(self.Position)))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return tendency_ema_rsi_strategy()
