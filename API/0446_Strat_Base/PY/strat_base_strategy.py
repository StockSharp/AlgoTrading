import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Messages import DataType, CandleStates, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class strat_base_strategy(Strategy):
    """Template strategy showcasing parameter handling and protection setup."""

    def __init__(self):
        super(strat_base_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ema_length = self.Param("EmaLength", 10) \
            .SetDisplay("EMA Length", "EMA period", "Moving Averages") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 50, 5)

        self._show_long = self.Param("ShowLong", True) \
            .SetDisplay("Long Entries", "Enable long entries", "Strategy")

        self._show_short = self.Param("ShowShort", False) \
            .SetDisplay("Short Entries", "Enable short entries", "Strategy")

        self._use_tp = self.Param("UseTP", False) \
            .SetDisplay("Enable Take Profit", "Use take profit", "Take Profit")

        self._tp_percent = self.Param("TpPercent", 1.2) \
            .SetDisplay("TP Percent", "Take profit percentage", "Take Profit") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 3.0, 0.3)

        self._use_sl = self.Param("UseSL", False) \
            .SetDisplay("Enable Stop Loss", "Use stop loss", "Stop Loss")

        self._sl_percent = self.Param("SlPercent", 1.8) \
            .SetDisplay("SL Percent", "Stop loss percentage", "Stop Loss") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 5.0, 0.5)

        self._ema = None

    # region properties
    @property
    def candle_type(self):
        return self._candle_type.Value

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
    def use_tp(self):
        return self._use_tp.Value

    @property
    def tp_percent(self):
        return self._tp_percent.Value

    @property
    def use_sl(self):
        return self._use_sl.Value

    @property
    def sl_percent(self):
        return self._sl_percent.Value

    # endregion

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super().OnStarted(time)

        self._ema = ExponentialMovingAverage(Length=self.ema_length)

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

        tp = Unit(self.tp_percent / 100.0, UnitTypes.Percent) if self.use_tp else Unit()
        sl = Unit(self.sl_percent / 100.0, UnitTypes.Percent) if self.use_sl else Unit()
        self.StartProtection(tp, sl)

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            return

        self.CheckEntry(candle, ema_value)
        self.CheckExit(candle, ema_value)

    def CheckEntry(self, candle, ema_value):
        price = candle.ClosePrice

        if self.show_long and self.Position == 0:
            pass  # long entry placeholder

        if self.show_short and self.Position == 0:
            pass  # short entry placeholder

    def CheckExit(self, candle, ema_value):
        if self.Position > 0:
            pass  # long exit placeholder

        if self.Position < 0:
            pass  # short exit placeholder

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return strat_base_strategy()
