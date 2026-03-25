import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class twenty_200_expert_auto_lot_strategy(Strategy):
    """Open price difference between two bar offsets with SL/TP management."""
    def __init__(self):
        super(twenty_200_expert_auto_lot_strategy, self).__init__()
        self._tp_long = self.Param("TakeProfitLong", 39).SetDisplay("TP Long", "TP for long", "Risk")
        self._sl_long = self.Param("StopLossLong", 147).SetDisplay("SL Long", "SL for long", "Risk")
        self._tp_short = self.Param("TakeProfitShort", 32).SetDisplay("TP Short", "TP for short", "Risk")
        self._sl_short = self.Param("StopLossShort", 267).SetDisplay("SL Short", "SL for short", "Risk")
        self._t1 = self.Param("T1", 6).SetDisplay("T1", "First bar shift", "Logic")
        self._t2 = self.Param("T2", 2).SetDisplay("T2", "Second bar shift", "Logic")
        self._delta_long = self.Param("DeltaLong", 1).SetDisplay("Delta Long", "Min rise", "Logic")
        self._delta_short = self.Param("DeltaShort", 1).SetDisplay("Delta Short", "Min fall", "Logic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(twenty_200_expert_auto_lot_strategy, self).OnReseted()
        self._opens = []
        self._stop_price = 0
        self._take_price = 0
        self._is_long = False

    def OnStarted(self, time):
        super(twenty_200_expert_auto_lot_strategy, self).OnStarted(time)
        self._opens = []
        self._stop_price = 0
        self._take_price = 0
        self._is_long = False

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
        pip = 1.0

        max_shift = max(self._t1.Value, self._t2.Value)
        if len(self._opens) <= max_shift:
            return

        # SL/TP check
        if self.Position != 0:
            if self._is_long:
                if low <= self._stop_price or high >= self._take_price:
                    self.SellMarket()
                    self._stop_price = 0
                    self._take_price = 0
                    return
            else:
                if high >= self._stop_price or low <= self._take_price:
                    self.BuyMarket()
                    self._stop_price = 0
                    self._take_price = 0
                    return
            return

        open_t1 = self._opens[len(self._opens) - 1 - self._t1.Value]
        open_t2 = self._opens[len(self._opens) - 1 - self._t2.Value]

        diff_short = open_t1 - open_t2
        diff_long = open_t2 - open_t1

        if diff_short > self._delta_short.Value * pip and self.Position >= 0:
            self.SellMarket()
            self._is_long = False
            self._stop_price = float(candle.OpenPrice) + self._sl_short.Value * pip
            self._take_price = float(candle.OpenPrice) - self._tp_short.Value * pip
        elif diff_long > self._delta_long.Value * pip and self.Position <= 0:
            self.BuyMarket()
            self._is_long = True
            self._stop_price = float(candle.OpenPrice) - self._sl_long.Value * pip
            self._take_price = float(candle.OpenPrice) + self._tp_long.Value * pip

    def CreateClone(self):
        return twenty_200_expert_auto_lot_strategy()
