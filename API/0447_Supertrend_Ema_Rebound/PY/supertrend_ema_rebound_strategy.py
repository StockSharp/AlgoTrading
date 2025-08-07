import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Messages import DataType, CandleStates, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class supertrend_ema_rebound_strategy(Strategy):
    """Trades SuperTrend direction changes and EMA rebounds."""

    def __init__(self):
        super(supertrend_ema_rebound_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 15, 2)

        self._atr_factor = self.Param("AtrFactor", 3.0) \
            .SetDisplay("ATR Factor", "ATR factor for Supertrend", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Moving Averages")

        self._show_long = self.Param("ShowLong", True) \
            .SetDisplay("Long Entries", "Enable long entries", "Strategy")

        self._show_short = self.Param("ShowShort", False) \
            .SetDisplay("Short Entries", "Enable short entries", "Strategy")

        self._tp_type = self.Param("TpType", "%") \
            .SetDisplay("TP Type", "Take profit type", "Strategy")

        self._tp_percent = self.Param("TpPercent", 1.5) \
            .SetDisplay("TP Percent", "Take profit percentage", "Strategy")

        self._supertrend = None
        self._ema = None
        self._prev_dir = 0
        self._prev_close = 0
        self._last_entry = 0

    # region properties
    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_factor(self):
        return self._atr_factor.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def show_long(self):
        return self._show_long.Value

    @property
    def show_short(self):
        return self._show_short.Value

    @property
    def tp_type(self):
        return self._tp_type.Value

    @property
    def tp_percent(self):
        return self._tp_percent.Value

    # endregion

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super().OnStarted(time)

        self._supertrend = SuperTrend(Length=self.atr_period, Multiplier=self.atr_factor)
        self._ema = ExponentialMovingAverage(Length=self.ema_length)

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._supertrend, self._ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._supertrend)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

        if self.tp_type == "%":
            tp = Unit(self.tp_percent / 100.0, UnitTypes.Percent)
            self.StartProtection(tp, Unit())

    def ProcessCandle(self, candle, super_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._supertrend.IsFormed or not self._ema.IsFormed:
            return

        price = candle.ClosePrice
        open_price = candle.OpenPrice

        direction = -1 if price > super_val else 1
        dir_changed = self._prev_dir != 0 and (direction * self._prev_dir < 0)

        self.CheckEntry(candle, ema_val, direction, dir_changed)
        self.CheckExit(direction, dir_changed)

        self._prev_dir = direction
        self._prev_close = price

        if self.Position != 0 and self._last_entry == 0:
            self._last_entry = open_price
        elif self.Position == 0:
            self._last_entry = 0

    def CheckEntry(self, candle, ema_value, direction, dir_changed):
        price = candle.ClosePrice
        open_price = candle.OpenPrice

        if self.show_long and self.Position == 0:
            entry1 = dir_changed and direction < 0
            entry2 = direction < 0 and self._prev_close < ema_value and price > ema_value and price < self._last_entry
            if entry1 or entry2:
                self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))

        if self.show_short and self.Position == 0:
            entry1 = dir_changed and direction > 0
            entry2 = direction > 0 and self._prev_close > ema_value and price < ema_value and price > self._last_entry
            if entry1 or entry2:
                self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

    def CheckExit(self, direction, dir_changed):
        if self.Position > 0 and dir_changed and direction > 0:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, self._prev_close, abs(self.Position)))

        if self.Position < 0 and dir_changed and direction < 0:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, self._prev_close, abs(self.Position)))

        if self.tp_type == "%" and self.Position != 0:
            pass  # handled by protection

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return supertrend_ema_rebound_strategy()
