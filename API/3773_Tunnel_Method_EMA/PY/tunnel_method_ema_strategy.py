import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class tunnel_method_ema_strategy(Strategy):
    """EMA crossover tunnel: long on fast cross above slow, short on fast cross below medium, with SL/TP/trailing."""
    def __init__(self):
        super(tunnel_method_ema_strategy, self).__init__()
        self._fast_len = self.Param("FastLength", 12).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._mid_len = self.Param("MediumLength", 144).SetGreaterThanZero().SetDisplay("Medium EMA", "Medium EMA period", "Indicators")
        self._slow_len = self.Param("SlowLength", 169).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._sl_points = self.Param("StopLossPoints", 25.0).SetNotNegative().SetDisplay("Stop Loss", "SL in points", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 230.0).SetNotNegative().SetDisplay("Take Profit", "TP in points", "Risk")
        self._trail_points = self.Param("TrailingStopPoints", 35.0).SetNotNegative().SetDisplay("Trailing Stop", "Trail distance", "Risk")
        self._trail_trigger = self.Param("TrailingTriggerPoints", 20.0).SetNotNegative().SetDisplay("Trail Trigger", "Profit to activate trail", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tunnel_method_ema_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_mid = 0
        self._prev_slow = 0
        self._has_prev = False
        self._entry_price = 0
        self._highest = 0
        self._lowest = 0
        self._long_trail = 0
        self._short_trail = 0

    def OnStarted(self, time):
        super(tunnel_method_ema_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_mid = 0
        self._prev_slow = 0
        self._has_prev = False
        self._entry_price = 0
        self._highest = 0
        self._lowest = 0
        self._long_trail = 0
        self._short_trail = 0

        slow = ExponentialMovingAverage()
        slow.Length = self._slow_len.Value
        mid = ExponentialMovingAverage()
        mid.Length = self._mid_len.Value
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_len.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(slow, mid, fast, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, slow_val, mid_val, fast_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        mid = float(mid_val)
        slow = float(slow_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if not self._has_prev:
            self._prev_fast = fast
            self._prev_mid = mid
            self._prev_slow = slow
            self._has_prev = True
            return

        sl = self._sl_points.Value
        tp = self._tp_points.Value
        trail = self._trail_points.Value
        trigger = self._trail_trigger.Value

        # Manage long
        if self.Position > 0:
            if self._entry_price == 0:
                self._entry_price = close
            self._highest = max(self._highest, high)

            if tp > 0 and high >= self._entry_price + tp:
                self.SellMarket()
                self._reset_pos()
                self._prev_fast = fast
                self._prev_mid = mid
                self._prev_slow = slow
                return
            if sl > 0 and low <= self._entry_price - sl:
                self.SellMarket()
                self._reset_pos()
                self._prev_fast = fast
                self._prev_mid = mid
                self._prev_slow = slow
                return
            if trail > 0 and trigger > 0 and self._highest - self._entry_price >= trigger:
                candidate = self._highest - trail
                if self._long_trail == 0 or candidate > self._long_trail:
                    self._long_trail = candidate
                if self._long_trail > 0 and low <= self._long_trail:
                    self.SellMarket()
                    self._reset_pos()
                    self._prev_fast = fast
                    self._prev_mid = mid
                    self._prev_slow = slow
                    return

        # Manage short
        elif self.Position < 0:
            if self._entry_price == 0:
                self._entry_price = close
            if self._lowest == 0:
                self._lowest = low
            else:
                self._lowest = min(self._lowest, low)

            if tp > 0 and low <= self._entry_price - tp:
                self.BuyMarket()
                self._reset_pos()
                self._prev_fast = fast
                self._prev_mid = mid
                self._prev_slow = slow
                return
            if sl > 0 and high >= self._entry_price + sl:
                self.BuyMarket()
                self._reset_pos()
                self._prev_fast = fast
                self._prev_mid = mid
                self._prev_slow = slow
                return
            if trail > 0 and trigger > 0 and self._entry_price - self._lowest >= trigger:
                candidate = self._lowest + trail
                if self._short_trail == 0 or candidate < self._short_trail:
                    self._short_trail = candidate
                if self._short_trail > 0 and high >= self._short_trail:
                    self.BuyMarket()
                    self._reset_pos()
                    self._prev_fast = fast
                    self._prev_mid = mid
                    self._prev_slow = slow
                    return

        # Entries
        if self.Position == 0:
            cross_up = self._prev_fast < self._prev_slow and fast > slow
            cross_down = self._prev_fast > self._prev_mid and fast < mid

            if cross_up:
                self.BuyMarket()
                self._entry_price = close
                self._highest = high
                self._long_trail = 0
            elif cross_down:
                self.SellMarket()
                self._entry_price = close
                self._lowest = low
                self._short_trail = 0

        self._prev_fast = fast
        self._prev_mid = mid
        self._prev_slow = slow

    def _reset_pos(self):
        self._entry_price = 0
        self._highest = 0
        self._lowest = 0
        self._long_trail = 0
        self._short_trail = 0

    def CreateClone(self):
        return tunnel_method_ema_strategy()
