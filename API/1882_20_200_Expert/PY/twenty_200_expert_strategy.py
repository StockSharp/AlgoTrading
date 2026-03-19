import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from collections import deque
from datatype_extensions import *
from indicator_extensions import *

class twenty_200_expert_strategy(Strategy):
    """Compares open prices at two offsets; trade on difference threshold with SL/TP."""
    def __init__(self):
        super(twenty_200_expert_strategy, self).__init__()
        self._shift1 = self.Param("Shift1", 6).SetGreaterThanZero().SetDisplay("Shift 1", "First bar shift", "Signals")
        self._shift2 = self.Param("Shift2", 2).SetGreaterThanZero().SetDisplay("Shift 2", "Second bar shift", "Signals")
        self._delta_long = self.Param("DeltaLong", 20).SetGreaterThanZero().SetDisplay("Delta Long", "Long threshold", "Signals")
        self._delta_short = self.Param("DeltaShort", 40).SetGreaterThanZero().SetDisplay("Delta Short", "Short threshold", "Signals")
        self._tp_long = self.Param("TakeProfitLong", 390).SetDisplay("TP Long", "TP for long", "Risk")
        self._sl_long = self.Param("StopLossLong", 1470).SetDisplay("SL Long", "SL for long", "Risk")
        self._tp_short = self.Param("TakeProfitShort", 320).SetDisplay("TP Short", "TP for short", "Risk")
        self._sl_short = self.Param("StopLossShort", 2670).SetDisplay("SL Short", "SL for short", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(twenty_200_expert_strategy, self).OnReseted()
        self._opens = deque(maxlen=20)
        self._entry_price = 0
        self._is_long = True

    def OnStarted(self, time):
        super(twenty_200_expert_strategy, self).OnStarted(time)
        self._opens = deque(maxlen=20)
        self._entry_price = 0
        self._is_long = True

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._opens.append(float(candle.OpenPrice))
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        step = 1.0

        max_shift = max(self._shift1.Value, self._shift2.Value)
        if len(self._opens) <= max_shift:
            return

        # Position management
        if self.Position > 0:
            tp = self._entry_price + self._tp_long.Value * step
            sl = self._entry_price - self._sl_long.Value * step
            if high >= tp or low <= sl:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            tp = self._entry_price - self._tp_short.Value * step
            sl = self._entry_price + self._sl_short.Value * step
            if low <= tp or high >= sl:
                self.BuyMarket()
                self._entry_price = 0

        if self.Position != 0:
            return

        arr = list(self._opens)
        open_s1 = arr[len(arr) - 1 - self._shift1.Value]
        open_s2 = arr[len(arr) - 1 - self._shift2.Value]

        diff_long = open_s2 - open_s1
        diff_short = open_s1 - open_s2
        th_long = self._delta_long.Value * step
        th_short = self._delta_short.Value * step

        if diff_long > th_long and diff_short <= th_short:
            self.BuyMarket()
            self._entry_price = close
            self._is_long = True
        elif diff_short > th_short and diff_long <= th_long:
            self.SellMarket()
            self._entry_price = close
            self._is_long = False

    def CreateClone(self):
        return twenty_200_expert_strategy()
