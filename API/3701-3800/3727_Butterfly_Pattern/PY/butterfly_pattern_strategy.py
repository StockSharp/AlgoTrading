import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class butterfly_pattern_strategy(Strategy):
    """Butterfly harmonic pattern detection with partial take-profits and break-even/trailing management."""

    def __init__(self):
        super(butterfly_pattern_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._pivot_left = self.Param("PivotLeft", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Pivot Left", "Bars to the left when validating a pivot", "Pattern")
        self._pivot_right = self.Param("PivotRight", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Pivot Right", "Bars to the right when validating a pivot", "Pattern")
        self._tolerance = self.Param("Tolerance", 0.50) \
            .SetGreaterThanZero() \
            .SetDisplay("Ratio Tolerance", "Maximum deviation allowed for Fibonacci ratios", "Pattern")
        self._use_fixed_volume = self.Param("UseFixedVolume", True) \
            .SetDisplay("Use Fixed Volume", "Use fixed trade volume instead of risk-based sizing", "Risk")
        self._fixed_volume = self.Param("FixedVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Fixed Volume", "Volume to trade when fixed sizing is active", "Risk")
        self._tp1_percent = self.Param("Tp1Percent", 50.0) \
            .SetNotNegative() \
            .SetDisplay("TP1 %", "Share of volume closed at the first take-profit", "Targets")
        self._tp2_percent = self.Param("Tp2Percent", 30.0) \
            .SetNotNegative() \
            .SetDisplay("TP2 %", "Share of volume closed at the second take-profit", "Targets")
        self._tp3_percent = self.Param("Tp3Percent", 20.0) \
            .SetNotNegative() \
            .SetDisplay("TP3 %", "Share of volume closed at the third take-profit", "Targets")
        self._use_break_even = self.Param("UseBreakEven", False) \
            .SetDisplay("Use Break-Even", "Enable break-even management", "Risk")
        self._break_even_after_tp = self.Param("BreakEvenAfterTp", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Break-Even After TP", "Activate break-even after the specified take-profit", "Risk")
        self._break_even_trigger = self.Param("BreakEvenTrigger", 30.0) \
            .SetDisplay("Break-Even Trigger", "Points required to lock break-even", "Risk")
        self._break_even_profit = self.Param("BreakEvenProfit", 5.0) \
            .SetDisplay("Break-Even Profit", "Profit offset applied to break-even", "Risk")
        self._use_trailing_stop = self.Param("UseTrailingStop", False) \
            .SetDisplay("Use Trailing", "Enable trailing stop management", "Risk")
        self._trail_after_tp = self.Param("TrailAfterTp", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Trail After TP", "Activate trailing after the specified take-profit", "Risk")
        self._trail_start = self.Param("TrailStart", 20.0) \
            .SetDisplay("Trail Start", "Points required before trailing", "Risk")
        self._trail_step = self.Param("TrailStep", 5.0) \
            .SetDisplay("Trail Step", "Trailing step in price points", "Risk")

        self._candles = []
        self._pivots = []
        self._side = None
        self._remaining_volume = 0.0
        self._lot1 = 0.0
        self._lot2 = 0.0
        self._lot3 = 0.0
        self._tp1_filled = False
        self._tp2_filled = False
        self._tp3_filled = False
        self._entry_price = None
        self._stop_price = None
        self._tp1_price = 0.0
        self._tp2_price = 0.0
        self._tp3_price = 0.0
        self._break_even_applied = False
        self._trailing_activated = False
        self._last_pattern_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def PivotLeft(self):
        return self._pivot_left.Value

    @property
    def PivotRight(self):
        return self._pivot_right.Value

    @property
    def Tolerance(self):
        return self._tolerance.Value

    @property
    def UseFixedVolume(self):
        return self._use_fixed_volume.Value

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @property
    def Tp1Percent(self):
        return self._tp1_percent.Value

    @property
    def Tp2Percent(self):
        return self._tp2_percent.Value

    @property
    def Tp3Percent(self):
        return self._tp3_percent.Value

    @property
    def UseBreakEven(self):
        return self._use_break_even.Value

    @property
    def BreakEvenAfterTp(self):
        return self._break_even_after_tp.Value

    @property
    def BreakEvenTrigger(self):
        return self._break_even_trigger.Value

    @property
    def BreakEvenProfit(self):
        return self._break_even_profit.Value

    @property
    def UseTrailingStop(self):
        return self._use_trailing_stop.Value

    @property
    def TrailAfterTp(self):
        return self._trail_after_tp.Value

    @property
    def TrailStart(self):
        return self._trail_start.Value

    @property
    def TrailStep(self):
        return self._trail_step.Value

    def OnReseted(self):
        super(butterfly_pattern_strategy, self).OnReseted()
        self._candles = []
        self._pivots = []
        self._reset_position()
        self._last_pattern_time = None

    def OnStarted2(self, time):
        super(butterfly_pattern_strategy, self).OnStarted2(time)

        self._candles = []
        self._pivots = []
        self._reset_position()
        self._last_pattern_time = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_risk_management(candle)

        self._candles.append(candle)

        pivot = self._try_extract_pivot()
        if pivot is not None:
            self._pivots.append(pivot)
            if len(self._pivots) > 5:
                self._pivots.pop(0)
            self._try_detect_pattern(candle)

    def _try_extract_pivot(self):
        left = self.PivotLeft
        right = self.PivotRight
        required = left + right + 1
        if len(self._candles) < required:
            return None

        index = len(self._candles) - 1 - right
        if index < left:
            return None

        middle = self._candles[index]
        if middle is None:
            return None

        is_high = True
        is_low = True
        from_idx = index - left
        to_idx = index + right

        for i in range(from_idx, to_idx + 1):
            if i < 0 or i >= len(self._candles) or i == index:
                continue
            c = self._candles[i]
            if c is None:
                continue
            if float(c.HighPrice) > float(middle.HighPrice):
                is_high = False
            if float(c.LowPrice) < float(middle.LowPrice):
                is_low = False

        if not is_high and not is_low:
            return None

        price = float(middle.HighPrice) if is_high else float(middle.LowPrice)
        pivot = (middle.OpenTime, price, is_high)

        if len(self._candles) > required:
            self._candles.pop(0)

        return pivot

    def _try_detect_pattern(self, candle):
        if len(self._pivots) < 5:
            return

        x = self._pivots[-5]
        a = self._pivots[-4]
        b = self._pivots[-3]
        c = self._pivots[-2]
        d = self._pivots[-1]

        if self._last_pattern_time is not None and self._last_pattern_time == d[0]:
            return

        side = self._detect_pattern_type(x, a, b, c, d)
        if side is None:
            return

        self._last_pattern_time = d[0]

        if self._side is not None and self._remaining_volume > 0:
            return

        self._execute_pattern(candle, side, a, c)

    def _detect_pattern_type(self, x, a, b, c, d):
        x_price, a_price, b_price, c_price, d_price = x[1], a[1], b[1], c[1], d[1]
        x_is_high, a_is_high, b_is_high, c_is_high, d_is_high = x[2], a[2], b[2], c[2], d[2]
        tol = float(self.Tolerance)

        diff_bear = x_price - a_price
        if x_is_high and not a_is_high and b_is_high and not c_is_high and d_is_high and diff_bear > 0:
            ideal_b = a_price + 0.786 * diff_bear
            if abs(b_price - ideal_b) <= tol * diff_bear:
                bc = b_price - c_price
                if bc >= 0.1 * diff_bear and bc <= 2 * diff_bear:
                    cd = d_price - c_price
                    if cd >= 0.5 * diff_bear and cd <= 3 * diff_bear:
                        return Sides.Sell

        diff_bull = a_price - x_price
        if not x_is_high and a_is_high and not b_is_high and c_is_high and not d_is_high and diff_bull > 0:
            ideal_b = a_price - 0.786 * diff_bull
            if abs(b_price - ideal_b) <= tol * diff_bull:
                bc = c_price - b_price
                if bc >= 0.1 * diff_bull and bc <= 2 * diff_bull:
                    cd = c_price - d_price
                    if cd >= 0.5 * diff_bull and cd <= 3 * diff_bull:
                        return Sides.Buy

        return None

    def _execute_pattern(self, candle, side, a, c):
        entry_price = float(candle.ClosePrice)
        tp3 = c[1]
        diff = (tp3 - entry_price) if side == Sides.Buy else (entry_price - tp3)
        if diff <= 0:
            return

        if side == Sides.Buy:
            tp1 = entry_price + diff / 3.0
            tp2 = entry_price + diff * 2.0 / 3.0
            stop = entry_price - (tp2 - entry_price) * 3.0
        else:
            tp1 = entry_price - diff / 3.0
            tp2 = entry_price - diff * 2.0 / 3.0
            stop = entry_price + (entry_price - tp2) * 3.0

        volume = float(self.FixedVolume) if self.UseFixedVolume else 1.0
        if volume <= 0:
            return

        percents = float(self.Tp1Percent) + float(self.Tp2Percent) + float(self.Tp3Percent)
        if percents <= 0:
            lot1 = volume / 3.0
            lot2 = volume / 3.0
            lot3 = volume - lot1 - lot2
        else:
            lot1 = volume * float(self.Tp1Percent) / percents
            lot2 = volume * float(self.Tp2Percent) / percents
            lot3 = volume - lot1 - lot2

        lot1 = max(0.0, lot1)
        lot2 = max(0.0, lot2)
        lot3 = max(0.0, lot3)
        total = lot1 + lot2 + lot3
        if total <= 0:
            return

        if side == Sides.Buy:
            self.BuyMarket(total)
        else:
            self.SellMarket(total)

        self._side = side
        self._entry_price = entry_price
        self._stop_price = stop
        self._lot1 = lot1
        self._lot2 = lot2
        self._lot3 = lot3
        self._remaining_volume = total
        self._tp1_filled = lot1 <= 0
        self._tp2_filled = lot2 <= 0
        self._tp3_filled = lot3 <= 0
        self._tp1_price = tp1
        self._tp2_price = tp2
        self._tp3_price = tp3
        self._break_even_applied = False
        self._trailing_activated = False

    def _update_risk_management(self, candle):
        if self._side is None or self._remaining_volume <= 0 or self._entry_price is None:
            return

        side = self._side
        direction = 1.0 if side == Sides.Buy else -1.0
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                step = ps

        if self._stop_price is not None:
            if side == Sides.Buy:
                hit = float(candle.LowPrice) <= self._stop_price
            else:
                hit = float(candle.HighPrice) >= self._stop_price
            if hit:
                self._exit_all()
                return

        if not self._tp1_filled and self._lot1 > 0:
            if side == Sides.Buy:
                reached = float(candle.HighPrice) >= self._tp1_price
            else:
                reached = float(candle.LowPrice) <= self._tp1_price
            if reached:
                self._exit_partial(self._lot1, 1)

        if not self._tp2_filled and self._lot2 > 0:
            if side == Sides.Buy:
                reached = float(candle.HighPrice) >= self._tp2_price
            else:
                reached = float(candle.LowPrice) <= self._tp2_price
            if reached:
                self._exit_partial(self._lot2, 2)

        if not self._tp3_filled and self._lot3 > 0:
            if side == Sides.Buy:
                reached = float(candle.HighPrice) >= self._tp3_price
            else:
                reached = float(candle.LowPrice) <= self._tp3_price
            if reached:
                self._exit_partial(self._lot3, 3)

        if self._remaining_volume <= 0:
            self._reset_position()
            return

        entry = self._entry_price
        self._apply_break_even(candle, entry, direction, step)
        self._apply_trailing(candle, entry, direction, step)

    def _exit_partial(self, volume, tp_index):
        if volume <= 0:
            return

        if self._side == Sides.Buy:
            self.SellMarket(volume)
        else:
            self.BuyMarket(volume)

        self._remaining_volume = max(0.0, self._remaining_volume - volume)

        if tp_index == 1:
            self._tp1_filled = True
        elif tp_index == 2:
            self._tp2_filled = True
        elif tp_index == 3:
            self._tp3_filled = True

    def _exit_all(self):
        if self._side is None or self._remaining_volume <= 0:
            self._reset_position()
            return

        volume = self._remaining_volume
        if self._side == Sides.Buy:
            self.SellMarket(volume)
        else:
            self.BuyMarket(volume)

        self._reset_position()

    def _apply_break_even(self, candle, entry, direction, step):
        if not self.UseBreakEven or self._break_even_applied or self._stop_price is None:
            return

        if not self._is_gate_passed(self.BreakEvenAfterTp):
            return

        if float(self.BreakEvenTrigger) <= 0:
            return

        movement = (float(candle.ClosePrice) - entry) * direction
        if movement < float(self.BreakEvenTrigger) * step:
            return

        new_stop = entry + direction * float(self.BreakEvenProfit) * step
        current_stop = self._stop_price

        if direction > 0:
            if new_stop <= current_stop:
                return
        elif new_stop >= current_stop:
            return

        self._stop_price = new_stop
        self._break_even_applied = True

    def _apply_trailing(self, candle, entry, direction, step):
        if not self.UseTrailingStop or self._stop_price is None:
            return

        if not self._is_gate_passed(self.TrailAfterTp):
            return

        if float(self.TrailStart) <= 0 or float(self.TrailStep) <= 0:
            return

        movement = (float(candle.ClosePrice) - entry) * direction
        if movement < float(self.TrailStart) * step:
            return

        new_stop = float(candle.ClosePrice) - direction * float(self.TrailStep) * step
        current_stop = self._stop_price

        if direction > 0:
            if new_stop <= current_stop:
                return
        elif new_stop >= current_stop:
            return

        self._stop_price = new_stop
        self._trailing_activated = True

    def _is_gate_passed(self, gate):
        normalized = max(1, min(2, gate))
        if normalized == 1:
            return self._tp1_filled
        if normalized == 2:
            return self._tp2_filled
        return False

    def _reset_position(self):
        self._side = None
        self._remaining_volume = 0.0
        self._lot1 = 0.0
        self._lot2 = 0.0
        self._lot3 = 0.0
        self._tp1_filled = False
        self._tp2_filled = False
        self._tp3_filled = False
        self._entry_price = None
        self._stop_price = None
        self._tp1_price = 0.0
        self._tp2_price = 0.0
        self._tp3_price = 0.0
        self._break_even_applied = False
        self._trailing_activated = False

    def CreateClone(self):
        return butterfly_pattern_strategy()
