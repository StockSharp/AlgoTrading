import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, AverageTrueRange, AverageDirectionalIndex
)
from StockSharp.Algo.Strategies import Strategy


class tipu_ea_strategy(Strategy):
    """Trend following strategy inspired by the Tipu Expert Advisor."""

    def __init__(self):
        super(tipu_ea_strategy, self).__init__()

        self._allow_hedging = self.Param("AllowHedging", False) \
            .SetDisplay("Allow Hedging", "Allow adding trades without closing opposite direction", "Risk")
        self._close_on_reverse = self.Param("CloseOnReverseSignal", True) \
            .SetDisplay("Close On Reverse", "Close the active position when the opposite signal appears", "Risk")
        self._enable_tp = self.Param("EnableTakeProfit", True) \
            .SetDisplay("Enable Take Profit", "Enable fixed take profit target", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 50000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._max_risk_pips = self.Param("MaxRiskPips", 100000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Risk (pips)", "Maximum stop distance allowed in pips", "Risk")
        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Base order volume", "General")
        self._enable_risk_free = self.Param("EnableRiskFreePyramiding", True) \
            .SetDisplay("Enable Risk Free", "Allow risk-free pyramiding of winners", "Risk")
        self._risk_free_step = self.Param("RiskFreeStepPips", 30000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk Free Step (pips)", "Profit distance required before locking and adding", "Risk")
        self._pyramid_inc = self.Param("PyramidIncrementVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Pyramid Increment", "Additional volume added on each pyramid step", "Risk")
        self._pyramid_max = self.Param("PyramidMaxVolume", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Pyramid Max Volume", "Maximum accumulated position volume", "Risk")
        self._enable_trailing = self.Param("EnableTrailingStop", True) \
            .SetDisplay("Enable Trailing", "Enable trailing stop once trade is in profit", "Risk")
        self._trailing_start = self.Param("TrailingStartPips", 30000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Start (pips)", "Profit in pips required before trailing", "Risk")
        self._trailing_cushion = self.Param("TrailingCushionPips", 15000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Cushion (pips)", "Distance between price and trailing stop", "Risk")
        self._higher_fast_len = self.Param("HigherFastLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Fast EMA", "Fast EMA length on higher timeframe", "Signals")
        self._higher_slow_len = self.Param("HigherSlowLength", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Slow EMA", "Slow EMA length on higher timeframe", "Signals")
        self._lower_fast_len = self.Param("LowerFastLength", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Fast EMA", "Fast EMA length on signal timeframe", "Signals")
        self._lower_slow_len = self.Param("LowerSlowLength", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Slow EMA", "Slow EMA length on signal timeframe", "Signals")
        self._adx_length = self.Param("AdxLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Length", "ADX period for range detection", "Signals")
        self._adx_threshold = self.Param("AdxThreshold", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "Below this ADX value the market is treated as ranging", "Signals")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period for initial stop calculation", "Risk")
        self._atr_mult = self.Param("AtrMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier applied to ATR for the initial stop", "Risk")
        self._signal_window = self.Param("HigherSignalWindowMinutes", 14400) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Signal Window", "Minutes within which the higher timeframe signal must be recent", "Signals")
        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Higher Timeframe", "Higher timeframe candles used for context", "General")
        self._lower_candle_type = self.Param("LowerCandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Signal Timeframe", "Primary timeframe used for entries", "General")

    @property
    def CandleType(self):
        return self._lower_candle_type.Value

    def OnReseted(self):
        super(tipu_ea_strategy, self).OnReseted()
        self._reset_all()

    def _reset_all(self):
        self._higher_initialized = False
        self._lower_initialized = False
        self._higher_prev_fast = 0.0
        self._higher_prev_slow = 0.0
        self._lower_prev_fast = 0.0
        self._lower_prev_slow = 0.0
        self._higher_trend = 0
        self._last_higher_dir = 0
        self._last_higher_time = None
        self._is_higher_range = False
        self._last_atr = 0.0
        self._avg_entry = 0.0
        self._current_stop = 0.0
        self._current_target = 0.0
        self._risk_free_activated = False
        self._pos_volume = 0.0
        self._next_long_pyramid = 0.0
        self._next_short_pyramid = 0.0

    def OnStarted2(self, time):
        super(tipu_ea_strategy, self).OnStarted2(time)
        self._reset_all()

        h_fast = ExponentialMovingAverage()
        h_fast.Length = self._higher_fast_len.Value
        h_slow = ExponentialMovingAverage()
        h_slow.Length = self._higher_slow_len.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value

        self._h_fast = h_fast
        self._h_slow = h_slow

        l_fast = ExponentialMovingAverage()
        l_fast.Length = self._lower_fast_len.Value
        l_slow = ExponentialMovingAverage()
        l_slow.Length = self._lower_slow_len.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        self._l_fast = l_fast
        self._l_slow = l_slow
        self._l_atr = atr

        h_sub = self.SubscribeCandles(self._higher_candle_type.Value)
        h_sub.BindEx(h_fast, h_slow, adx, self._on_higher).Start()

        l_sub = self.SubscribeCandles(self._lower_candle_type.Value)
        l_sub.Bind(l_fast, l_slow, atr, self._on_lower).Start()

    def _to_price(self, pips):
        if pips <= 0:
            return 0.0
        sec = self.Security
        step = 0.0001
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            step = float(sec.PriceStep)
        return float(pips) * step

    def _get_candle_close_time(self, candle, candle_type):
        if candle.CloseTime is not None and candle.CloseTime != candle.CloseTime.__class__():
            return candle.CloseTime
        arg = candle_type.Arg
        if isinstance(arg, TimeSpan):
            return candle.OpenTime + arg
        return candle.OpenTime + TimeSpan.FromMinutes(1)

    def _on_higher(self, candle, fast_val, slow_val, adx_val):
        if candle.State != CandleStates.Finished:
            return

        if not fast_val.IsFinal or not slow_val.IsFinal or not adx_val.IsFinal:
            return
        if fast_val.IsEmpty or slow_val.IsEmpty or adx_val.IsEmpty:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        # ADX returns AverageDirectionalIndexValue; get MovingAverage for strength
        adx_ma = adx_val.MovingAverage
        if adx_ma is None:
            return
        adx_strength = float(adx_ma)

        if not self._higher_initialized:
            if not self._h_fast.IsFormed or not self._h_slow.IsFormed:
                return
            self._higher_prev_fast = fast
            self._higher_prev_slow = slow
            self._higher_initialized = True
            if fast > slow:
                self._higher_trend = 1
            elif fast < slow:
                self._higher_trend = -1
            else:
                self._higher_trend = 0
            self._is_higher_range = adx_strength < float(self._adx_threshold.Value)
            return

        cross_up = fast > slow and self._higher_prev_fast <= self._higher_prev_slow
        cross_down = fast < slow and self._higher_prev_fast >= self._higher_prev_slow

        close_time = self._get_candle_close_time(candle, self._higher_candle_type.Value)

        if cross_up:
            self._higher_trend = 1
            self._last_higher_dir = 1
            self._last_higher_time = close_time
        elif cross_down:
            self._higher_trend = -1
            self._last_higher_dir = -1
            self._last_higher_time = close_time
        elif fast > slow:
            self._higher_trend = 1
        elif fast < slow:
            self._higher_trend = -1

        self._is_higher_range = adx_strength < float(self._adx_threshold.Value)
        self._higher_prev_fast = fast
        self._higher_prev_slow = slow

    def _on_lower(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        atr = float(atr_val)
        self._last_atr = atr

        if not self._lower_initialized:
            if not self._l_fast.IsFormed or not self._l_slow.IsFormed or not self._l_atr.IsFormed:
                return
            self._lower_prev_fast = fast
            self._lower_prev_slow = slow
            self._lower_initialized = True
            return

        cross_up = fast > slow and self._lower_prev_fast <= self._lower_prev_slow
        cross_down = fast < slow and self._lower_prev_fast >= self._lower_prev_slow

        self._lower_prev_fast = fast
        self._lower_prev_slow = slow

        close_time = self._get_candle_close_time(candle, self._lower_candle_type.Value)

        if cross_up:
            self._handle_long(candle, close_time)
        if cross_down:
            self._handle_short(candle, close_time)

        self._manage_position(candle, cross_up, cross_down)

    def _is_higher_signal_valid(self, time, direction):
        if self._higher_trend != direction:
            return False
        if self._last_higher_dir != direction:
            return False
        if self._last_higher_time is None:
            return False
        window = TimeSpan.FromMinutes(self._signal_window.Value)
        if window <= TimeSpan.Zero:
            return True
        return (time - self._last_higher_time) <= window

    def _handle_long(self, candle, close_time):
        if self._is_higher_range:
            return
        if not self._is_higher_signal_valid(close_time, 1):
            return

        if self.Position < 0:
            if not self._allow_hedging.Value:
                if self._close_on_reverse.Value:
                    self.BuyMarket()
                    self._reset_position()
                else:
                    return
            elif self._close_on_reverse.Value:
                self.BuyMarket()
                self._reset_position()

        if self.Position > 0:
            return

        entry_price = float(candle.ClosePrice)
        atr_dist = self._last_atr * float(self._atr_mult.Value)
        if atr_dist <= 0:
            return

        max_risk = self._to_price(float(self._max_risk_pips.Value))
        if max_risk > 0 and atr_dist > max_risk:
            atr_dist = max_risk

        stop_price = entry_price - atr_dist
        if stop_price <= 0:
            return

        volume = float(self._trade_volume.Value)
        if volume <= 0:
            return

        self.BuyMarket()

        prev_vol = abs(self._pos_volume)
        new_vol = prev_vol + volume
        if prev_vol == 0:
            self._avg_entry = entry_price
        else:
            self._avg_entry = (prev_vol * self._avg_entry + entry_price * volume) / new_vol
        self._pos_volume = new_vol
        self._current_stop = stop_price
        if self._enable_tp.Value:
            self._current_target = entry_price + self._to_price(float(self._tp_pips.Value))
        else:
            self._current_target = 0.0
        self._risk_free_activated = False
        self._next_long_pyramid = self._avg_entry + self._to_price(float(self._risk_free_step.Value))

    def _handle_short(self, candle, close_time):
        if self._is_higher_range:
            return
        if not self._is_higher_signal_valid(close_time, -1):
            return

        if self.Position > 0:
            if not self._allow_hedging.Value:
                if self._close_on_reverse.Value:
                    self.SellMarket()
                    self._reset_position()
                else:
                    return
            elif self._close_on_reverse.Value:
                self.SellMarket()
                self._reset_position()

        if self.Position < 0:
            return

        entry_price = float(candle.ClosePrice)
        atr_dist = self._last_atr * float(self._atr_mult.Value)
        if atr_dist <= 0:
            return

        max_risk = self._to_price(float(self._max_risk_pips.Value))
        if max_risk > 0 and atr_dist > max_risk:
            atr_dist = max_risk

        stop_price = entry_price + atr_dist
        volume = float(self._trade_volume.Value)
        if volume <= 0:
            return

        self.SellMarket()

        prev_vol = abs(self._pos_volume)
        new_vol = prev_vol + volume
        if prev_vol == 0:
            self._avg_entry = entry_price
        else:
            self._avg_entry = (prev_vol * self._avg_entry + entry_price * volume) / new_vol
        self._pos_volume = -new_vol
        self._current_stop = stop_price
        if self._enable_tp.Value:
            self._current_target = entry_price - self._to_price(float(self._tp_pips.Value))
        else:
            self._current_target = 0.0
        self._risk_free_activated = False
        self._next_short_pyramid = self._avg_entry - self._to_price(float(self._risk_free_step.Value))

    def _manage_position(self, candle, cross_up, cross_down):
        price = float(candle.ClosePrice)

        if self.Position > 0:
            if self._close_on_reverse.Value and cross_down:
                self._exit_long()
                return
            if self._current_stop > 0 and price <= self._current_stop:
                self._exit_long()
                return
            if self._current_target > 0 and price >= self._current_target:
                self._exit_long()
                return
            self._update_trailing_long(price)
            self._update_risk_free_long(price)
        elif self.Position < 0:
            if self._close_on_reverse.Value and cross_up:
                self._exit_short()
                return
            if self._current_stop > 0 and price >= self._current_stop:
                self._exit_short()
                return
            if self._current_target > 0 and price <= self._current_target:
                self._exit_short()
                return
            self._update_trailing_short(price)
            self._update_risk_free_short(price)

    def _update_trailing_long(self, price):
        if not self._enable_trailing.Value:
            return
        start = self._to_price(float(self._trailing_start.Value))
        if start <= 0:
            return
        if price - self._avg_entry < start:
            return
        cushion = self._to_price(float(self._trailing_cushion.Value))
        if cushion <= 0:
            return
        new_stop = price - cushion
        if new_stop > self._current_stop:
            self._current_stop = new_stop

    def _update_trailing_short(self, price):
        if not self._enable_trailing.Value:
            return
        start = self._to_price(float(self._trailing_start.Value))
        if start <= 0:
            return
        if self._avg_entry - price < start:
            return
        cushion = self._to_price(float(self._trailing_cushion.Value))
        if cushion <= 0:
            return
        new_stop = price + cushion
        if self._current_stop == 0 or new_stop < self._current_stop:
            self._current_stop = new_stop

    def _update_risk_free_long(self, price):
        if not self._enable_risk_free.Value:
            return
        step = self._to_price(float(self._risk_free_step.Value))
        if step <= 0:
            return
        if not self._risk_free_activated:
            if price - self._avg_entry >= step:
                self._current_stop = max(self._current_stop, self._avg_entry)
                self._risk_free_activated = True
            else:
                return
        if self._next_long_pyramid <= 0:
            self._next_long_pyramid = self._avg_entry + step
        if price < self._next_long_pyramid:
            return

        cur_vol = abs(self._pos_volume)
        max_vol = float(self._pyramid_max.Value)
        if max_vol <= 0:
            return
        if cur_vol >= max_vol:
            self._current_stop = max(self._current_stop, price - step)
            return

        inc = min(float(self._pyramid_inc.Value), max_vol - cur_vol)
        if inc <= 0:
            return

        self.BuyMarket()
        new_vol = cur_vol + inc
        self._avg_entry = (cur_vol * self._avg_entry + price * inc) / new_vol
        self._pos_volume = new_vol
        self._current_stop = max(self._current_stop, price - step)
        self._next_long_pyramid = price + step

    def _update_risk_free_short(self, price):
        if not self._enable_risk_free.Value:
            return
        step = self._to_price(float(self._risk_free_step.Value))
        if step <= 0:
            return
        if not self._risk_free_activated:
            if self._avg_entry - price >= step:
                if self._current_stop == 0:
                    self._current_stop = self._avg_entry
                else:
                    self._current_stop = min(self._current_stop, self._avg_entry)
                self._risk_free_activated = True
            else:
                return

        if self._next_short_pyramid >= self._avg_entry or self._next_short_pyramid == 0:
            self._next_short_pyramid = self._avg_entry - step
        if price > self._next_short_pyramid:
            return

        cur_vol = abs(self._pos_volume)
        max_vol = float(self._pyramid_max.Value)
        if max_vol <= 0:
            return
        if cur_vol >= max_vol:
            if self._current_stop == 0:
                self._current_stop = price + step
            else:
                self._current_stop = min(self._current_stop, price + step)
            return

        inc = min(float(self._pyramid_inc.Value), max_vol - cur_vol)
        if inc <= 0:
            return

        self.SellMarket()
        new_vol = cur_vol + inc
        self._avg_entry = (cur_vol * self._avg_entry + price * inc) / new_vol
        self._pos_volume = -new_vol
        if self._current_stop == 0:
            self._current_stop = price + step
        else:
            self._current_stop = min(self._current_stop, price + step)
        self._next_short_pyramid = price - step

    def _exit_long(self):
        if self.Position <= 0:
            return
        self.SellMarket()
        self._reset_position()

    def _exit_short(self):
        if self.Position >= 0:
            return
        self.BuyMarket()
        self._reset_position()

    def _reset_position(self):
        self._avg_entry = 0.0
        self._current_stop = 0.0
        self._current_target = 0.0
        self._risk_free_activated = False
        self._pos_volume = 0.0
        self._next_long_pyramid = 0.0
        self._next_short_pyramid = 0.0

    def CreateClone(self):
        return tipu_ea_strategy()
