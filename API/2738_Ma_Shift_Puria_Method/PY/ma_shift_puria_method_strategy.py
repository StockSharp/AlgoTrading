import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, MovingAverageConvergenceDivergence
)
from StockSharp.Algo.Strategies import Strategy


class ma_shift_puria_method_strategy(Strategy):
    def __init__(self):
        super(ma_shift_puria_method_strategy, self).__init__()

        self._use_manual_volume = self.Param("UseManualVolume", True)
        self._manual_volume = self.Param("ManualVolume", 0.1)
        self._risk_percent = self.Param("RiskPercent", 9.0)
        self._stop_loss_pips = self.Param("StopLossPips", 45)
        self._take_profit_pips = self.Param("TakeProfitPips", 75)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5)
        self._max_positions = self.Param("MaxPositions", 1)
        self._shift_min_pips = self.Param("ShiftMinPips", 20.0)
        self._fast_length = self.Param("FastLength", 14)
        self._slow_length = self.Param("SlowLength", 80)
        self._macd_fast = self.Param("MacdFast", 11)
        self._macd_slow = self.Param("MacdSlow", 102)
        self._use_fractal_trailing = self.Param("UseFractalTrailing", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._fast_ema = None
        self._slow_ema = None
        self._macd = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._fast_prev3 = None
        self._slow_prev1 = None
        self._slow_prev2 = None
        self._slow_prev3 = None
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._high_window = [0.0] * 5
        self._low_window = [0.0] * 5
        self._fractal_count = 0
        self._last_upper_fractal = None
        self._last_lower_fractal = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def UseManualVolume(self):
        return self._use_manual_volume.Value

    @property
    def ManualVolume(self):
        return self._manual_volume.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @property
    def ShiftMinPips(self):
        return self._shift_min_pips.Value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @property
    def UseFractalTrailing(self):
        return self._use_fractal_trailing.Value

    def OnStarted(self, time):
        super(ma_shift_puria_method_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastLength
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowLength

        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.MacdSlow
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.MacdFast
        self._macd = MovingAverageConvergenceDivergence(slow_ma, fast_ma)

        self.Volume = self.ManualVolume

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self._macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast, slow, macd_main):
        if candle.State != CandleStates.Finished:
            return

        pip = self._get_pip_size()
        self._update_fractals(candle)

        prev_fast1 = self._fast_prev1
        prev_fast2 = self._fast_prev2
        prev_fast3 = self._fast_prev3
        prev_slow1 = self._slow_prev1
        prev_slow3 = self._slow_prev3
        prev_macd1 = self._macd_prev1
        prev_macd3 = self._macd_prev3

        self._update_history(float(fast), float(slow), float(macd_main))
        self._manage_long_position(candle, pip)
        self._manage_short_position(candle, pip)

        if (prev_fast1 is None or prev_fast2 is None or prev_fast3 is None or
                prev_slow1 is None or prev_slow3 is None or
                prev_macd1 is None or prev_macd3 is None):
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if pip <= 0:
            pip = 0.0001

        f1 = prev_fast1; f2 = prev_fast2; f3 = prev_fast3
        s1 = prev_slow1; s3 = prev_slow3
        m1 = prev_macd1; m3 = prev_macd3

        x1_long = (f1 - f2) / pip
        x2_long = (f2 - f3) / pip
        x1_short = (f2 - f1) / pip
        x2_short = (f3 - f2) / pip

        shift_req = self.ShiftMinPips

        buy_signal = (f1 > s1 and s1 > s3 and f1 > f2 and
                      m1 > 0 and m3 < 0 and
                      x1_long > shift_req and (x1_long >= x2_long or x2_long <= 0))

        sell_signal = (f1 < s1 and s1 < s3 and f1 < f2 and
                       m1 < 0 and m3 > 0 and
                       x1_short > shift_req and (x1_short >= x2_short or x2_short <= 0))

        if buy_signal:
            self._try_enter_long(candle, pip)
        elif sell_signal:
            self._try_enter_short(candle, pip)

    def _try_enter_long(self, candle, pip):
        self.BuyMarket()
        price = float(candle.ClosePrice)
        sd = self.StopLossPips * pip if self.StopLossPips > 0 else 0
        self._long_entry_price = price
        self._long_stop_price = price - sd if sd > 0 else None
        self._long_take_price = price + self.TakeProfitPips * pip if self.TakeProfitPips > 0 else None
        self._reset_short_state()

    def _try_enter_short(self, candle, pip):
        self.SellMarket()
        price = float(candle.ClosePrice)
        sd = self.StopLossPips * pip if self.StopLossPips > 0 else 0
        self._short_entry_price = price
        self._short_stop_price = price + sd if sd > 0 else None
        self._short_take_price = price - self.TakeProfitPips * pip if self.TakeProfitPips > 0 else None
        self._reset_long_state()

    def _manage_long_position(self, candle, pip):
        if self.Position <= 0:
            self._reset_long_state()
            return

        if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
            self.SellMarket(); self._reset_long_state(); return

        if self._long_take_price is not None and float(candle.HighPrice) >= self._long_take_price:
            self.SellMarket(); self._reset_long_state(); return

        if self.TrailingStopPips > 0 and self._long_entry_price is not None:
            dist = self.TrailingStopPips * pip
            step = self.TrailingStepPips * pip
            if dist > 0:
                profit = float(candle.ClosePrice) - self._long_entry_price
                if profit > (self.TrailingStopPips + self.TrailingStepPips) * pip:
                    threshold = float(candle.ClosePrice) - (dist + step)
                    if self._long_stop_price is None or self._long_stop_price < threshold:
                        self._long_stop_price = float(candle.ClosePrice) - dist

        if (self.UseFractalTrailing and self._long_entry_price is not None and
                self._long_stop_price is not None and self.TakeProfitPips > 0):
            target = self.TakeProfitPips * pip
            if target > 0:
                profit = float(candle.ClosePrice) - self._long_entry_price
                if profit >= 0.95 * target and self._last_lower_fractal is not None:
                    if self._last_lower_fractal > self._long_stop_price:
                        self._long_stop_price = self._last_lower_fractal

        if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
            self.SellMarket(); self._reset_long_state()

    def _manage_short_position(self, candle, pip):
        if self.Position >= 0:
            self._reset_short_state()
            return

        if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
            self.BuyMarket(); self._reset_short_state(); return

        if self._short_take_price is not None and float(candle.LowPrice) <= self._short_take_price:
            self.BuyMarket(); self._reset_short_state(); return

        if self.TrailingStopPips > 0 and self._short_entry_price is not None:
            dist = self.TrailingStopPips * pip
            step = self.TrailingStepPips * pip
            if dist > 0:
                profit = self._short_entry_price - float(candle.ClosePrice)
                if profit > (self.TrailingStopPips + self.TrailingStepPips) * pip:
                    threshold = float(candle.ClosePrice) + (dist + step)
                    if self._short_stop_price is None or self._short_stop_price > threshold:
                        self._short_stop_price = float(candle.ClosePrice) + dist

        if (self.UseFractalTrailing and self._short_entry_price is not None and
                self._short_stop_price is not None and self.TakeProfitPips > 0):
            target = self.TakeProfitPips * pip
            if target > 0:
                profit = self._short_entry_price - float(candle.ClosePrice)
                if profit >= 0.95 * target and self._last_upper_fractal is not None:
                    if self._last_upper_fractal < self._short_stop_price:
                        self._short_stop_price = self._last_upper_fractal

        if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
            self.BuyMarket(); self._reset_short_state()

    def _update_history(self, fast, slow, macd_main):
        self._fast_prev3 = self._fast_prev2
        self._fast_prev2 = self._fast_prev1
        self._fast_prev1 = fast
        self._slow_prev3 = self._slow_prev2
        self._slow_prev2 = self._slow_prev1
        self._slow_prev1 = slow
        self._macd_prev3 = self._macd_prev2
        self._macd_prev2 = self._macd_prev1
        self._macd_prev1 = macd_main

    def _update_fractals(self, candle):
        for i in range(len(self._high_window) - 1):
            self._high_window[i] = self._high_window[i + 1]
            self._low_window[i] = self._low_window[i + 1]
        self._high_window[-1] = float(candle.HighPrice)
        self._low_window[-1] = float(candle.LowPrice)

        if self._fractal_count < len(self._high_window):
            self._fractal_count += 1
        if self._fractal_count < len(self._high_window):
            return

        center = len(self._high_window) // 2
        p_upper = self._high_window[center]
        p_lower = self._low_window[center]

        is_upper = True
        for i in range(len(self._high_window)):
            if i == center:
                continue
            if self._high_window[i] >= p_upper:
                is_upper = False
                break
        if is_upper:
            self._last_upper_fractal = p_upper

        is_lower = True
        for i in range(len(self._low_window)):
            if i == center:
                continue
            if self._low_window[i] <= p_lower:
                is_lower = False
                break
        if is_lower:
            self._last_lower_fractal = p_lower

    def _get_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if step <= 0:
            return 0.0001
        if step < 0.01:
            return step * 10.0
        return step

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None

    def OnReseted(self):
        super(ma_shift_puria_method_strategy, self).OnReseted()
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._fast_prev3 = None
        self._slow_prev1 = None
        self._slow_prev2 = None
        self._slow_prev3 = None
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._reset_long_state()
        self._reset_short_state()
        self._high_window = [0.0] * 5
        self._low_window = [0.0] * 5
        self._fractal_count = 0
        self._last_upper_fractal = None
        self._last_lower_fractal = None

    def CreateClone(self):
        return ma_shift_puria_method_strategy()
