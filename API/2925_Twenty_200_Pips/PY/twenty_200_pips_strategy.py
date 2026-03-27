import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class twenty_200_pips_strategy(Strategy):
    """Daily breakout: open price difference at two offsets with StartProtection SL/TP."""
    def __init__(self):
        super(twenty_200_pips_strategy, self).__init__()
        self._tp = self.Param("TakeProfit", 200).SetGreaterThanZero().SetDisplay("Take Profit", "TP in points", "Risk")
        self._sl = self.Param("StopLoss", 2000).SetGreaterThanZero().SetDisplay("Stop Loss", "SL in points", "Risk")
        self._first_offset = self.Param("FirstOffset", 7).SetGreaterThanZero().SetDisplay("First Offset", "Older bar index", "Signal")
        self._second_offset = self.Param("SecondOffset", 2).SetGreaterThanZero().SetDisplay("Second Offset", "Newer bar index", "Signal")
        self._delta = self.Param("DeltaPoints", 1).SetGreaterThanZero().SetDisplay("Delta", "Min difference", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(twenty_200_pips_strategy, self).OnReseted()
        self._opens = []

    def OnStarted(self, time):
        super(twenty_200_pips_strategy, self).OnStarted(time)
        self._opens = []

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        sec = self.Security
        point_value = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and sec.PriceStep > 0 else 1.0
        sl = self._sl.Value
        tp = self._tp.Value
        from StockSharp.Messages import Unit, UnitTypes
        tp_unit = Unit(tp * point_value, UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(sl * point_value, UnitTypes.Absolute) if sl > 0 else None
        self.StartProtection(tp_unit, sl_unit)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._opens.append(float(candle.OpenPrice))
        max_offset = max(self._first_offset.Value, self._second_offset.Value)
        if len(self._opens) <= max_offset:
            return

        # Trim buffer
        if len(self._opens) > max_offset + 100:
            self._opens = self._opens[-(max_offset + 50):]

        if self.Position != 0:
            return

        open_first = self._opens[len(self._opens) - 1 - self._first_offset.Value]
        open_second = self._opens[len(self._opens) - 1 - self._second_offset.Value]
        threshold = self._delta.Value

        if open_first > open_second + threshold:
            self.SellMarket()
        elif open_first + threshold < open_second:
            self.BuyMarket()

    def CreateClone(self):
        return twenty_200_pips_strategy()
