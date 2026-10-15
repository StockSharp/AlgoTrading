import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class percentage_crossover_channel_strategy(Strategy):
    def __init__(self):
        super(percentage_crossover_channel_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")
        self._percent = self.Param("Percent", 1.0).SetGreaterThanZero().SetDisplay("Percent", "Channel width percent", "Channel")
        self._sl_points = self.Param("StopLossPoints", 0).SetNotNegative().SetDisplay("Stop Loss (points)", "Protective stop distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 0).SetNotNegative().SetDisplay("Take Profit (points)", "Target profit distance", "Risk")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(percentage_crossover_channel_strategy, self).OnReseted()
        self._last_middle = 0
        self._has_state = False
        self._prev_upper = None
        self._prev_lower = None
        self._prev_close = None
        self._prev_high = None
        self._prev_low = None
        self._prev_prev_upper = None
        self._prev_prev_lower = None
        self._prev_prev_close = None
        self._prev_prev_high = None
        self._prev_prev_low = None
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0

    def OnStarted2(self, time):
        super(percentage_crossover_channel_strategy, self).OnStarted2(time)
        self._last_middle = 0
        self._has_state = False
        self._prev_upper = None
        self._prev_lower = None
        self._prev_close = None
        self._prev_high = None
        self._prev_low = None
        self._prev_prev_upper = None
        self._prev_prev_lower = None
        self._prev_prev_close = None
        self._prev_prev_high = None
        self._prev_prev_low = None
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def _get_step(self):
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            return float(self.Security.PriceStep)
        return 0.01

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        exit_triggered = self._check_protection(candle)
        if not exit_triggered:
            self._try_enter(candle)
        self._update_channel(candle)

    def _try_enter(self, candle):
        if self._prev_lower is None or self._prev_prev_lower is None:
            return
        if self._prev_close is None or self._prev_prev_close is None:
            return

        touch_lower = self._prev_prev_low > self._prev_prev_lower and self._prev_low <= self._prev_lower
        touch_upper = self._prev_prev_high < self._prev_prev_upper and self._prev_high >= self._prev_upper

        if touch_lower:
            self._enter_long(candle)
        elif touch_upper:
            self._enter_short(candle)

    def _enter_long(self, candle):
        volume = 1 + (Math.Abs(self.Position) if self.Position < 0 else 0)
        self.BuyMarket(volume)
        self._entry_price = float(candle.OpenPrice)
        step = self._get_step()
        self._stop_price = self._entry_price - self._sl_points.Value * step if self._sl_points.Value > 0 and step > 0 else None
        self._take_price = self._entry_price + self._tp_points.Value * step if self._tp_points.Value > 0 and step > 0 else None

    def _enter_short(self, candle):
        volume = 1 + (self.Position if self.Position > 0 else 0)
        self.SellMarket(volume)
        self._entry_price = float(candle.OpenPrice)
        step = self._get_step()
        self._stop_price = self._entry_price + self._sl_points.Value * step if self._sl_points.Value > 0 and step > 0 else None
        self._take_price = self._entry_price - self._tp_points.Value * step if self._tp_points.Value > 0 and step > 0 else None

    def _check_protection(self, candle):
        if self.Position > 0:
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True
            if self._take_price is not None and candle.HighPrice >= self._take_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True
        elif self.Position < 0:
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True
            if self._take_price is not None and candle.LowPrice <= self._take_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True
        else:
            self._reset_protection()
        return False

    def _reset_protection(self):
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0

    def _update_channel(self, candle):
        pct = self._percent.Value if self._percent.Value > 0 else 0.001
        plus_factor = 1.0 + pct / 100.0
        minus_factor = 1.0 - pct / 100.0
        price = float(candle.ClosePrice)

        if not self._has_state:
            current_middle = price
            self._has_state = True
        else:
            lower_bound = price * minus_factor
            upper_bound = price * plus_factor
            current_middle = self._last_middle
            if lower_bound > current_middle:
                current_middle = lower_bound
            elif upper_bound < current_middle:
                current_middle = upper_bound

        current_upper = current_middle * plus_factor
        current_lower = current_middle * minus_factor

        if self._prev_upper is not None:
            self._prev_prev_upper = self._prev_upper
            self._prev_prev_lower = self._prev_lower
            self._prev_prev_close = self._prev_close
            self._prev_prev_high = self._prev_high
            self._prev_prev_low = self._prev_low

        self._prev_upper = current_upper
        self._prev_lower = current_lower
        self._prev_close = float(candle.ClosePrice)
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._last_middle = current_middle

    def CreateClone(self):
        return percentage_crossover_channel_strategy()
