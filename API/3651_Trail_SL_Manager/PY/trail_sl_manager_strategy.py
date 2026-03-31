import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trail_sl_manager_strategy(Strategy):
    """SMA crossover (10/30) with break-even and trailing stop management."""
    def __init__(self):
        super(trail_sl_manager_strategy, self).__init__()
        self._enable_be = self.Param("EnableBreakEven", True).SetDisplay("Break Even", "Enable break-even stop adjustment", "Risk")
        self._be_trigger = self.Param("BreakEvenTriggerPoints", 20).SetDisplay("Break Even Trigger", "Points before break-even", "Risk")
        self._be_offset = self.Param("BreakEvenOffsetPoints", 10).SetDisplay("Break Even Offset", "Extra points locked at break-even", "Risk")
        self._enable_trailing = self.Param("EnableTrailing", True).SetDisplay("Trailing", "Enable trailing stop management", "Risk")
        self._trail_after_be = self.Param("TrailAfterBreakEven", True).SetDisplay("Trail After Break Even", "Start trailing only after break-even", "Risk")
        self._trail_start = self.Param("TrailStartPoints", 40).SetDisplay("Trail Start", "Points before trailing begins", "Risk")
        self._trail_step = self.Param("TrailStepPoints", 10).SetDisplay("Trail Step", "Step for trailing recalculation", "Risk")
        self._trail_offset = self.Param("TrailOffsetPoints", 10).SetDisplay("Trail Offset", "SL increment per trailing step", "Risk")
        self._initial_stop = self.Param("InitialStopPoints", 200).SetDisplay("Initial Stop", "Initial stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle subscription", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def _reset_state(self):
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._long_be_active = False
        self._short_be_active = False

    def OnReseted(self):
        super(trail_sl_manager_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(trail_sl_manager_strategy, self).OnStarted2(time)
        sec = self.Security
        ps = 0.0
        if sec is not None and sec.PriceStep is not None:
            try:
                ps = float(sec.PriceStep)
            except:
                ps = 0.0
        self._price_step = ps if ps > 0 else 1.0
        self._last_entry_price = 0.0
        self._reset_state()

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

    def OnOwnTradeReceived(self, trade):
        super(trail_sl_manager_strategy, self).OnOwnTradeReceived(trade)
        if self.Position == 0:
            self._reset_state()
            return
        if trade.Order.Side == Sides.Buy and self.Position > 0:
            self._long_stop = self._last_entry_price - self._initial_stop.Value * self._price_step if self._initial_stop.Value > 0 else 0.0
            self._long_be_active = False
        elif trade.Order.Side == Sides.Sell and self.Position < 0:
            self._short_stop = self._last_entry_price + self._initial_stop.Value * self._price_step if self._initial_stop.Value > 0 else 0.0
            self._short_be_active = False

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)

        if fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._last_entry_price = float(candle.ClosePrice)
        elif fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._last_entry_price = float(candle.ClosePrice)

        self._manage_long(candle)
        self._manage_short(candle)

    def _manage_long(self, candle):
        if self.Position <= 0:
            self._long_stop = 0.0
            self._long_be_active = False
            return

        entry_price = self._last_entry_price
        if entry_price <= 0:
            return

        ps = self._price_step
        current_price = float(candle.ClosePrice)
        profit_points = (current_price - entry_price) / ps

        be_trigger = self._be_trigger.Value
        be_offset = self._be_offset.Value

        if self._enable_be.Value and not self._long_be_active and profit_points >= be_trigger and be_trigger > 0:
            new_stop = entry_price + be_offset * ps if be_offset > 0 else entry_price
            if new_stop < current_price:
                self._long_stop = max(self._long_stop, new_stop)
                self._long_be_active = True

        trail_offset = self._trail_offset.Value
        trail_step = self._trail_step.Value
        if not self._enable_trailing.Value or trail_offset <= 0 or trail_step <= 0:
            return

        require_be = self._trail_after_be.Value and self._enable_be.Value
        if require_be and not self._long_be_active:
            return

        if require_be:
            base_stop = self._long_stop if self._long_stop > 0 else (entry_price + be_offset * ps if be_offset > 0 else entry_price)
        else:
            if self._initial_stop.Value > 0:
                base_stop = entry_price - self._initial_stop.Value * ps
            else:
                base_stop = self._long_stop if self._long_stop > 0 else 0.0

        if base_stop <= 0:
            return

        trail_start = self._trail_start.Value
        if not require_be and profit_points < trail_start:
            return

        if require_be:
            base_distance = (current_price - base_stop) / ps
            if base_distance < trail_start:
                return

        if require_be:
            start_price = base_stop + (trail_start - trail_step) * ps
        else:
            start_price = entry_price + (trail_start - trail_step) * ps

        step_distance = trail_step * ps
        if step_distance <= 0:
            return

        open_steps = (current_price - start_price) / step_distance
        if open_steps <= 0:
            return

        step_open_price = int(math.floor(open_steps))
        if self._long_stop > base_stop:
            current_stop_steps = int(math.floor((self._long_stop - base_stop) / (trail_offset * ps)))
        else:
            current_stop_steps = 0

        if step_open_price <= current_stop_steps:
            return

        proposed_stop = base_stop + step_open_price * trail_offset * ps
        max_stop = float(candle.LowPrice) - ps
        if proposed_stop >= max_stop:
            proposed_stop = max_stop

        if proposed_stop > self._long_stop and proposed_stop < current_price:
            self._long_stop = proposed_stop

        if self._long_stop > 0 and float(candle.LowPrice) <= self._long_stop:
            self.SellMarket()

    def _manage_short(self, candle):
        if self.Position >= 0:
            self._short_stop = 0.0
            self._short_be_active = False
            return

        entry_price = self._last_entry_price
        if entry_price <= 0:
            return

        ps = self._price_step
        current_price = float(candle.ClosePrice)
        profit_points = (entry_price - current_price) / ps

        be_trigger = self._be_trigger.Value
        be_offset = self._be_offset.Value

        if self._enable_be.Value and not self._short_be_active and profit_points >= be_trigger and be_trigger > 0:
            new_stop = entry_price - be_offset * ps if be_offset > 0 else entry_price
            if new_stop > current_price:
                self._short_stop = new_stop if self._short_stop == 0 else min(self._short_stop, new_stop)
                self._short_be_active = True

        trail_offset = self._trail_offset.Value
        trail_step = self._trail_step.Value
        if not self._enable_trailing.Value or trail_offset <= 0 or trail_step <= 0:
            return

        require_be = self._trail_after_be.Value and self._enable_be.Value
        if require_be and not self._short_be_active:
            return

        if require_be:
            base_stop = self._short_stop if self._short_stop > 0 else (entry_price - be_offset * ps if be_offset > 0 else entry_price)
        else:
            if self._initial_stop.Value > 0:
                base_stop = entry_price + self._initial_stop.Value * ps
            else:
                base_stop = self._short_stop if self._short_stop > 0 else 0.0

        if base_stop <= 0:
            return

        trail_start = self._trail_start.Value
        if not require_be and profit_points < trail_start:
            return

        if require_be:
            base_distance = (base_stop - current_price) / ps
            if base_distance < trail_start:
                return

        if require_be:
            start_price = base_stop - (trail_start - trail_step) * ps
        else:
            start_price = entry_price - (trail_start - trail_step) * ps

        step_distance = trail_step * ps
        if step_distance <= 0:
            return

        open_steps = (start_price - current_price) / step_distance
        if open_steps <= 0:
            return

        step_open_price = int(math.floor(open_steps))
        if self._short_stop > 0:
            current_stop_steps = int(math.floor((base_stop - self._short_stop) / (trail_offset * ps)))
        else:
            current_stop_steps = 0

        if step_open_price <= current_stop_steps:
            return

        proposed_stop = base_stop - step_open_price * trail_offset * ps
        min_stop = float(candle.HighPrice) + ps
        if proposed_stop <= min_stop:
            proposed_stop = min_stop

        if (self._short_stop == 0 or proposed_stop < self._short_stop) and proposed_stop > current_price:
            self._short_stop = proposed_stop

        if self._short_stop > 0 and float(candle.HighPrice) >= self._short_stop:
            self.BuyMarket()

    def CreateClone(self):
        return trail_sl_manager_strategy()
