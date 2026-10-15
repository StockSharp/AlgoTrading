import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vr_moving_distance_strategy(Strategy):
    """Opens positions when price deviates from MA by distance; scales with multiplier."""
    def __init__(self):
        super(vr_moving_distance_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 60).SetGreaterThanZero().SetDisplay("MA Length", "Moving average period", "Moving Average")
        self._distance = self.Param("DistancePips", 50).SetGreaterThanZero().SetDisplay("Distance", "Offset from MA in pips", "Trading")
        self._tp = self.Param("TakeProfitPips", 50).SetNotNegative().SetDisplay("Take Profit", "TP in pips", "Trading")
        self._vol_mult = self.Param("VolumeMultiplier", 1).SetGreaterThanZero().SetDisplay("Volume Multiplier", "Multiplier for additional entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vr_moving_distance_strategy, self).OnReseted()
        self._long_entries = 0
        self._short_entries = 0
        self._long_highest = 0
        self._short_lowest = 0
        self._long_entry_price = None
        self._short_entry_price = None

    def OnStarted2(self, time):
        super(vr_moving_distance_strategy, self).OnStarted2(time)
        self._long_entries = 0
        self._short_entries = 0
        self._long_highest = 0
        self._short_lowest = 0
        self._long_entry_price = None
        self._short_entry_price = None

        ma = ExponentialMovingAverage()
        ma.Length = self._ma_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        mv = float(ma_val)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        dist = self._distance.Value
        tp = self._tp.Value

        long_trigger = mv + dist if self._long_entries == 0 else self._long_highest + dist
        short_trigger = mv - dist if self._short_entries == 0 else self._short_lowest - dist

        # TP for single long
        if tp > 0 and self._long_entries == 1 and self.Position > 0 and self._long_entry_price is not None:
            target = self._long_entry_price + tp
            if high >= target:
                self.SellMarket()
                self._long_entries = 0
                self._long_highest = 0
                self._long_entry_price = None

        # TP for single short
        if tp > 0 and self._short_entries == 1 and self.Position < 0 and self._short_entry_price is not None:
            target = self._short_entry_price - tp
            if low <= target:
                self.BuyMarket()
                self._short_entries = 0
                self._short_lowest = 0
                self._short_entry_price = None

        # Long entry
        if high >= long_trigger:
            if self.Position < 0:
                self.BuyMarket()
                self._short_entries = 0
                self._short_lowest = 0
                self._short_entry_price = None
            self.BuyMarket()
            self._long_entries += 1
            self._long_highest = long_trigger if self._long_entries == 1 else max(self._long_highest, long_trigger)
            self._long_entry_price = long_trigger if self._long_entries == 1 else None
        elif low <= short_trigger:
            if self.Position > 0:
                self.SellMarket()
                self._long_entries = 0
                self._long_highest = 0
                self._long_entry_price = None
            self.SellMarket()
            self._short_entries += 1
            self._short_lowest = short_trigger if self._short_entries == 1 else min(self._short_lowest, short_trigger)
            self._short_entry_price = short_trigger if self._short_entries == 1 else None

    def CreateClone(self):
        return vr_moving_distance_strategy()
