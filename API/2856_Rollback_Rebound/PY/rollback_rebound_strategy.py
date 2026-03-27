import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rollback_rebound_strategy(Strategy):
    def __init__(self):
        super(rollback_rebound_strategy, self).__init__()
        self._sl_pips = self.Param("StopLossPips", 30.0).SetNotNegative().SetDisplay("Stop Loss (pips)", "SL distance", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 90.0).SetNotNegative().SetDisplay("Take Profit (pips)", "TP distance", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 20.0).SetNotNegative().SetDisplay("Trailing Stop (pips)", "Trailing offset", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 15.0).SetNotNegative().SetDisplay("Trailing Step (pips)", "Trailing step", "Risk")
        self._rollback_pips = self.Param("RollbackRatePips", 40.0).SetNotNegative().SetDisplay("Rollback Threshold (pips)", "Pullback threshold", "Signal")
        self._reverse_signal = self.Param("ReverseSignal", False).SetDisplay("Reverse Signal", "Invert entry logic", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rollback_rebound_strategy, self).OnReseted()
        self._pip_size = 0
        self._long_entry = 0
        self._long_stop = 0
        self._long_tp = 0
        self._short_entry = 0
        self._short_stop = 0
        self._short_tp = 0

    def OnStarted(self, time):
        super(rollback_rebound_strategy, self).OnStarted(time)
        self._pip_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._pip_size = float(self.Security.PriceStep)
            if self.Security.Decimals == 3 or self.Security.Decimals == 5:
                self._pip_size *= 10.0

        self._sl_offset = self._sl_pips.Value * self._pip_size
        self._tp_offset = self._tp_pips.Value * self._pip_size
        self._trail_offset = self._trailing_stop_pips.Value * self._pip_size
        self._trail_step_offset = self._trailing_step_pips.Value * self._pip_size
        self._rollback_offset = self._rollback_pips.Value * self._pip_size

        self._long_entry = 0
        self._long_stop = 0
        self._long_tp = 0
        self._short_entry = 0
        self._short_stop = 0
        self._short_tp = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_position(candle)

        o = candle.OpenPrice
        c = candle.ClosePrice
        h = candle.HighPrice
        l = candle.LowPrice

        long_cond = o > c and h - c > self._rollback_offset
        short_cond = c > o and c - l > self._rollback_offset

        if self._reverse_signal.Value:
            long_cond, short_cond = short_cond, long_cond

        if long_cond and self.Position <= 0:
            vol = self.Volume + abs(self.Position)
            if vol <= 0:
                return
            self.BuyMarket(vol)
            self._short_entry = 0
            self._short_stop = 0
            self._short_tp = 0
            self._long_entry = float(c)
            self._long_stop = self._long_entry - self._sl_offset if self._sl_pips.Value > 0 else 0
            self._long_tp = self._long_entry + self._tp_offset if self._tp_pips.Value > 0 else 0
        elif short_cond and self.Position >= 0:
            vol = self.Volume + abs(self.Position)
            if vol <= 0:
                return
            self.SellMarket(vol)
            self._long_entry = 0
            self._long_stop = 0
            self._long_tp = 0
            self._short_entry = float(c)
            self._short_stop = self._short_entry + self._sl_offset if self._sl_pips.Value > 0 else 0
            self._short_tp = self._short_entry - self._tp_offset if self._tp_pips.Value > 0 else 0

    def _manage_position(self, candle):
        if self.Position > 0:
            extreme = float(candle.HighPrice)
            if self._long_entry == 0:
                self._long_entry = float(candle.ClosePrice)
            if self._trail_offset > 0 and self._long_entry > 0:
                if extreme - self._long_entry > self._trail_offset + self._trail_step_offset:
                    threshold = extreme - (self._trail_offset + self._trail_step_offset)
                    if self._long_stop == 0 or self._long_stop < threshold:
                        self._long_stop = extreme - self._trail_offset
            if self._long_tp > 0 and candle.HighPrice >= self._long_tp:
                self.SellMarket(abs(self.Position))
                self._long_entry = 0
                self._long_stop = 0
                self._long_tp = 0
                return
            if self._long_stop > 0 and candle.LowPrice <= self._long_stop:
                self.SellMarket(abs(self.Position))
                self._long_entry = 0
                self._long_stop = 0
                self._long_tp = 0
                return
        elif self.Position < 0:
            extreme = float(candle.LowPrice)
            if self._short_entry == 0:
                self._short_entry = float(candle.ClosePrice)
            if self._trail_offset > 0 and self._short_entry > 0:
                if self._short_entry - extreme > self._trail_offset + self._trail_step_offset:
                    threshold = extreme + (self._trail_offset + self._trail_step_offset)
                    if self._short_stop == 0 or self._short_stop > threshold:
                        self._short_stop = extreme + self._trail_offset
            if self._short_tp > 0 and candle.LowPrice <= self._short_tp:
                self.BuyMarket(abs(self.Position))
                self._short_entry = 0
                self._short_stop = 0
                self._short_tp = 0
                return
            if self._short_stop > 0 and candle.HighPrice >= self._short_stop:
                self.BuyMarket(abs(self.Position))
                self._short_entry = 0
                self._short_stop = 0
                self._short_tp = 0
                return
        else:
            self._long_entry = 0
            self._long_stop = 0
            self._long_tp = 0
            self._short_entry = 0
            self._short_stop = 0
            self._short_tp = 0

    def CreateClone(self):
        return rollback_rebound_strategy()
