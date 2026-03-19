import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trail_sl_manager_strategy(Strategy):
    """SMA crossover (10/30) with break-even and trailing stop management."""
    def __init__(self):
        super(trail_sl_manager_strategy, self).__init__()
        self._be_trigger = self.Param("BreakEvenTriggerPoints", 20).SetDisplay("Break Even Trigger", "Points before break-even", "Risk")
        self._be_offset = self.Param("BreakEvenOffsetPoints", 10).SetDisplay("Break Even Offset", "Extra points locked at break-even", "Risk")
        self._trail_start = self.Param("TrailStartPoints", 40).SetDisplay("Trail Start", "Points before trailing begins", "Risk")
        self._trail_step = self.Param("TrailStepPoints", 10).SetDisplay("Trail Step", "Step for trailing recalculation", "Risk")
        self._trail_offset = self.Param("TrailOffsetPoints", 10).SetDisplay("Trail Offset", "SL increment per trailing step", "Risk")
        self._initial_stop = self.Param("InitialStopPoints", 200).SetDisplay("Initial Stop", "Initial stop distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle subscription", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trail_sl_manager_strategy, self).OnReseted()
        self._entry_price = 0
        self._long_stop = 0
        self._short_stop = 0

    def OnStarted(self, time):
        super(trail_sl_manager_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._long_stop = 0
        self._short_stop = 0

        fast = SimpleMovingAverage()
        fast.Length = 10
        slow = SimpleMovingAverage()
        slow.Length = 30

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        if fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._long_stop = close - self._initial_stop.Value if self._initial_stop.Value > 0 else 0
            self._short_stop = 0
        elif fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._short_stop = close + self._initial_stop.Value if self._initial_stop.Value > 0 else 0
            self._long_stop = 0

        # Manage long
        if self.Position > 0 and self._entry_price > 0:
            profit = close - self._entry_price
            be_trigger = self._be_trigger.Value
            be_offset = self._be_offset.Value
            if be_trigger > 0 and profit >= be_trigger:
                new_stop = self._entry_price + be_offset
                if new_stop > self._long_stop:
                    self._long_stop = new_stop
            trail_start = self._trail_start.Value
            trail_step = self._trail_step.Value
            trail_offset = self._trail_offset.Value
            if trail_start > 0 and trail_step > 0 and trail_offset > 0 and profit >= trail_start:
                new_stop = close - trail_offset
                if new_stop > self._long_stop:
                    self._long_stop = new_stop
            if self._long_stop > 0 and low <= self._long_stop:
                self.SellMarket()
                self._entry_price = 0
                self._long_stop = 0

        # Manage short
        elif self.Position < 0 and self._entry_price > 0:
            profit = self._entry_price - close
            be_trigger = self._be_trigger.Value
            be_offset = self._be_offset.Value
            if be_trigger > 0 and profit >= be_trigger:
                new_stop = self._entry_price - be_offset
                if self._short_stop == 0 or new_stop < self._short_stop:
                    self._short_stop = new_stop
            trail_start = self._trail_start.Value
            trail_step = self._trail_step.Value
            trail_offset = self._trail_offset.Value
            if trail_start > 0 and trail_step > 0 and trail_offset > 0 and profit >= trail_start:
                new_stop = close + trail_offset
                if self._short_stop == 0 or new_stop < self._short_stop:
                    self._short_stop = new_stop
            if self._short_stop > 0 and high >= self._short_stop:
                self.BuyMarket()
                self._entry_price = 0
                self._short_stop = 0

    def CreateClone(self):
        return trail_sl_manager_strategy()
