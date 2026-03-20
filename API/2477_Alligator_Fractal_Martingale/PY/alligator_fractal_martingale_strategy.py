import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class alligator_fractal_martingale_strategy(Strategy):
    def __init__(self):
        super(alligator_fractal_martingale_strategy, self).__init__()

        self._jaw_length = self.Param("JawLength", 13)
        self._jaw_shift = self.Param("JawShift", 8)
        self._teeth_length = self.Param("TeethLength", 8)
        self._teeth_shift = self.Param("TeethShift", 5)
        self._lips_length = self.Param("LipsLength", 5)
        self._lips_shift = self.Param("LipsShift", 3)
        self._entry_spread = self.Param("EntrySpread", 50.0)
        self._exit_spread = self.Param("ExitSpread", 10.0)
        self._use_alligator_entry = self.Param("UseAlligatorEntry", True)
        self._use_fractal_filter = self.Param("UseFractalFilter", True)
        self._use_alligator_exit = self.Param("UseAlligatorExit", False)
        self._allow_multiple_entries = self.Param("AllowMultipleEntries", False)
        self._enable_martingale = self.Param("EnableMartingale", False)
        self._enable_trailing = self.Param("EnableTrailing", True)
        self._manual_mode = self.Param("ManualMode", False)
        self._take_profit_distance = self.Param("TakeProfitDistance", 800.0)
        self._stop_loss_distance = self.Param("StopLossDistance", 800.0)
        self._trailing_step = self.Param("TrailingStep", 100.0)
        self._fractal_lookback = self.Param("FractalLookback", 10)
        self._fractal_buffer = self.Param("FractalBuffer", 300.0)
        self._martingale_steps = self.Param("MartingaleSteps", 3)
        self._martingale_multiplier = self.Param("MartingaleMultiplier", 1.3)
        self._martingale_step_distance = self.Param("MartingaleStepDistance", 500.0)
        self._max_volume = self.Param("MaxVolume", 10.0)
        self._base_volume = self.Param("BaseVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._jaw = None
        self._teeth = None
        self._lips = None
        self._jaw_history = []
        self._teeth_history = []
        self._lips_history = []
        self._high_history = []
        self._low_history = []
        self._up_fractals = []
        self._down_fractals = []
        self._long_martingale_levels = []
        self._short_martingale_levels = []
        self._current_buy_state = True
        self._current_sell_state = True
        self._prev_buy_state = True
        self._prev_sell_state = True
        self._active_up_fractal = None
        self._active_down_fractal = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._finished_bar_index = -1
        self._history_offset = 0
        self._max_alligator_buffer = 0

    @property
    def JawLength(self):
        return self._jaw_length.Value

    @JawLength.setter
    def JawLength(self, value):
        self._jaw_length.Value = value

    @property
    def JawShift(self):
        return self._jaw_shift.Value

    @JawShift.setter
    def JawShift(self, value):
        self._jaw_shift.Value = value

    @property
    def TeethLength(self):
        return self._teeth_length.Value

    @TeethLength.setter
    def TeethLength(self, value):
        self._teeth_length.Value = value

    @property
    def TeethShift(self):
        return self._teeth_shift.Value

    @TeethShift.setter
    def TeethShift(self, value):
        self._teeth_shift.Value = value

    @property
    def LipsLength(self):
        return self._lips_length.Value

    @LipsLength.setter
    def LipsLength(self, value):
        self._lips_length.Value = value

    @property
    def LipsShift(self):
        return self._lips_shift.Value

    @LipsShift.setter
    def LipsShift(self, value):
        self._lips_shift.Value = value

    @property
    def EntrySpread(self):
        return self._entry_spread.Value

    @EntrySpread.setter
    def EntrySpread(self, value):
        self._entry_spread.Value = value

    @property
    def ExitSpread(self):
        return self._exit_spread.Value

    @ExitSpread.setter
    def ExitSpread(self, value):
        self._exit_spread.Value = value

    @property
    def UseAlligatorEntry(self):
        return self._use_alligator_entry.Value

    @UseAlligatorEntry.setter
    def UseAlligatorEntry(self, value):
        self._use_alligator_entry.Value = value

    @property
    def UseFractalFilter(self):
        return self._use_fractal_filter.Value

    @UseFractalFilter.setter
    def UseFractalFilter(self, value):
        self._use_fractal_filter.Value = value

    @property
    def UseAlligatorExit(self):
        return self._use_alligator_exit.Value

    @UseAlligatorExit.setter
    def UseAlligatorExit(self, value):
        self._use_alligator_exit.Value = value

    @property
    def AllowMultipleEntries(self):
        return self._allow_multiple_entries.Value

    @AllowMultipleEntries.setter
    def AllowMultipleEntries(self, value):
        self._allow_multiple_entries.Value = value

    @property
    def EnableMartingale(self):
        return self._enable_martingale.Value

    @EnableMartingale.setter
    def EnableMartingale(self, value):
        self._enable_martingale.Value = value

    @property
    def EnableTrailing(self):
        return self._enable_trailing.Value

    @EnableTrailing.setter
    def EnableTrailing(self, value):
        self._enable_trailing.Value = value

    @property
    def ManualMode(self):
        return self._manual_mode.Value

    @ManualMode.setter
    def ManualMode(self, value):
        self._manual_mode.Value = value

    @property
    def TakeProfitDistance(self):
        return self._take_profit_distance.Value

    @TakeProfitDistance.setter
    def TakeProfitDistance(self, value):
        self._take_profit_distance.Value = value

    @property
    def StopLossDistance(self):
        return self._stop_loss_distance.Value

    @StopLossDistance.setter
    def StopLossDistance(self, value):
        self._stop_loss_distance.Value = value

    @property
    def TrailingStep(self):
        return self._trailing_step.Value

    @TrailingStep.setter
    def TrailingStep(self, value):
        self._trailing_step.Value = value

    @property
    def FractalLookback(self):
        return self._fractal_lookback.Value

    @FractalLookback.setter
    def FractalLookback(self, value):
        self._fractal_lookback.Value = value

    @property
    def FractalBuffer(self):
        return self._fractal_buffer.Value

    @FractalBuffer.setter
    def FractalBuffer(self, value):
        self._fractal_buffer.Value = value

    @property
    def MartingaleSteps(self):
        return self._martingale_steps.Value

    @MartingaleSteps.setter
    def MartingaleSteps(self, value):
        self._martingale_steps.Value = value

    @property
    def MartingaleMultiplier(self):
        return self._martingale_multiplier.Value

    @MartingaleMultiplier.setter
    def MartingaleMultiplier(self, value):
        self._martingale_multiplier.Value = value

    @property
    def MartingaleStepDistance(self):
        return self._martingale_step_distance.Value

    @MartingaleStepDistance.setter
    def MartingaleStepDistance(self, value):
        self._martingale_step_distance.Value = value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @MaxVolume.setter
    def MaxVolume(self, value):
        self._max_volume.Value = value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(alligator_fractal_martingale_strategy, self).OnStarted(time)

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = self.JawLength
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = self.TeethLength
        self._lips = SmoothedMovingAverage()
        self._lips.Length = self.LipsLength

        self._max_alligator_buffer = max(max(int(self.JawShift), int(self.TeethShift)), int(self.LipsShift)) + 10

        self._jaw_history = []
        self._teeth_history = []
        self._lips_history = []
        self._high_history = []
        self._low_history = []
        self._up_fractals = []
        self._down_fractals = []
        self._long_martingale_levels = []
        self._short_martingale_levels = []
        self._current_buy_state = True
        self._current_sell_state = True
        self._prev_buy_state = True
        self._prev_sell_state = True
        self._active_up_fractal = None
        self._active_down_fractal = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._finished_bar_index = -1
        self._history_offset = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        is_final = candle.State == CandleStates.Finished

        jaw_result = self._jaw.Process(median, candle.CloseTime, is_final)
        if is_final:
            self._add_indicator_value(self._jaw_history, float(jaw_result))

        teeth_result = self._teeth.Process(median, candle.CloseTime, is_final)
        if is_final:
            self._add_indicator_value(self._teeth_history, float(teeth_result))

        lips_result = self._lips.Process(median, candle.CloseTime, is_final)
        if is_final:
            self._add_indicator_value(self._lips_history, float(lips_result))

        if not is_final:
            return

        self._finished_bar_index += 1

        self._update_alligator_states()
        self._update_fractals(candle)
        self._update_trailing_and_stops(candle)
        self._process_martingale_levels(candle)

        if self.Position == 0:
            self._long_stop = None
            self._long_take = None
            self._short_stop = None
            self._short_take = None

        if not self._jaw.IsFormed or not self._teeth.IsFormed or not self._lips.IsFormed:
            return

        if not self.ManualMode:
            self._try_open_positions(candle)

        self._try_close_positions_on_alligator(candle)

    def _try_open_positions(self, candle):
        allow_long = not self.UseFractalFilter or self._active_up_fractal is not None
        allow_short = not self.UseFractalFilter or self._active_down_fractal is not None

        long_signal = not self.UseAlligatorEntry or (self._current_buy_state and not self._prev_buy_state)
        short_signal = not self.UseAlligatorEntry or (self._current_sell_state and not self._prev_sell_state)

        initial_volume = self._get_initial_volume()
        if initial_volume <= 0.0:
            return

        if long_signal and allow_long:
            can_add = self.AllowMultipleEntries or self.Position <= 0
            if can_add:
                self._open_long(float(candle.ClosePrice), initial_volume)

        if short_signal and allow_short:
            can_add = self.AllowMultipleEntries or self.Position >= 0
            if can_add:
                self._open_short(float(candle.ClosePrice), initial_volume)

    def _try_close_positions_on_alligator(self, candle):
        if not self.UseAlligatorExit:
            return

        if self._prev_buy_state and not self._current_buy_state and self.Position > 0:
            self.SellMarket()
            self._clear_long_state()

        if self._prev_sell_state and not self._current_sell_state and self.Position < 0:
            self.BuyMarket()
            self._clear_short_state()

    def _update_alligator_states(self):
        self._prev_buy_state = self._current_buy_state
        self._prev_sell_state = self._current_sell_state

        jaw = self._get_shifted_value(self._jaw_history, int(self.JawShift))
        teeth = self._get_shifted_value(self._teeth_history, int(self.TeethShift))
        lips = self._get_shifted_value(self._lips_history, int(self.LipsShift))

        if jaw is None or teeth is None or lips is None:
            return

        entry_spread = float(self.EntrySpread)
        exit_spread = float(self.ExitSpread)

        if lips > jaw + entry_spread:
            self._current_buy_state = True

        if lips + exit_spread < teeth:
            self._current_buy_state = False

        if jaw > lips + entry_spread:
            self._current_sell_state = True

        if jaw + exit_spread < teeth:
            self._current_sell_state = False

    def _update_fractals(self, candle):
        self._high_history.append(float(candle.HighPrice))
        self._low_history.append(float(candle.LowPrice))

        max_history = max(int(self.FractalLookback) + 10, 10)
        while len(self._high_history) > max_history:
            self._high_history.pop(0)
            self._low_history.pop(0)
            self._history_offset += 1

        count = len(self._high_history)
        if count >= 5:
            center = count - 3
            if center >= 2 and center + 2 < len(self._high_history) and center + 2 < len(self._low_history):
                h2 = self._high_history[center]
                h1 = self._high_history[center - 1]
                h0 = self._high_history[center - 2]
                h3 = self._high_history[center + 1]
                h4 = self._high_history[center + 2]

                if h2 > h0 and h2 > h1 and h2 > h3 and h2 > h4:
                    self._up_fractals.append((self._history_offset + center, h2))

                l2 = self._low_history[center]
                l1 = self._low_history[center - 1]
                l0 = self._low_history[center - 2]
                l3 = self._low_history[center + 1]
                l4 = self._low_history[center + 2]

                if l2 < l0 and l2 < l1 and l2 < l3 and l2 < l4:
                    self._down_fractals.append((self._history_offset + center, l2))

        lookback = int(self.FractalLookback)

        i = len(self._up_fractals) - 1
        while i >= 0:
            if self._finished_bar_index - self._up_fractals[i][0] > lookback:
                self._up_fractals.pop(i)
            i -= 1

        i = len(self._down_fractals) - 1
        while i >= 0:
            if self._finished_bar_index - self._down_fractals[i][0] > lookback:
                self._down_fractals.pop(i)
            i -= 1

        close = float(candle.ClosePrice)
        fractal_buf = float(self.FractalBuffer)

        self._active_up_fractal = None
        for idx in range(len(self._up_fractals)):
            value = self._up_fractals[idx][1]
            if value >= close + fractal_buf:
                if self._active_up_fractal is None or value > self._active_up_fractal:
                    self._active_up_fractal = value

        self._active_down_fractal = None
        for idx in range(len(self._down_fractals)):
            value = self._down_fractals[idx][1]
            if value <= close - fractal_buf:
                if self._active_down_fractal is None or value < self._active_down_fractal:
                    self._active_down_fractal = value

    def _update_trailing_and_stops(self, candle):
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        sl_dist = float(self.StopLossDistance)
        trail_step = float(self.TrailingStep)

        if self.Position > 0:
            if sl_dist > 0.0:
                desired = close - sl_dist
                if self._long_stop is None:
                    self._long_stop = desired
                elif self.EnableTrailing and desired > self._long_stop + trail_step:
                    self._long_stop = desired

            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket()
                self._clear_long_state()
                return

            if self._long_take is not None and high >= self._long_take:
                self.SellMarket()
                self._clear_long_state()

        elif self.Position < 0:
            if sl_dist > 0.0:
                desired = close + sl_dist
                if self._short_stop is None:
                    self._short_stop = desired
                elif self.EnableTrailing and desired < self._short_stop - trail_step:
                    self._short_stop = desired

            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket()
                self._clear_short_state()
                return

            if self._short_take is not None and low <= self._short_take:
                self.BuyMarket()
                self._clear_short_state()

    def _process_martingale_levels(self, candle):
        if not self.EnableMartingale:
            return

        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        sl_dist = float(self.StopLossDistance)

        if self.Position >= 0:
            i = 0
            while i < len(self._long_martingale_levels):
                level = self._long_martingale_levels[i]
                if level["executed"]:
                    i += 1
                    continue
                if low <= level["price"]:
                    vol = level["volume"]
                    if vol <= 0.0:
                        level["executed"] = True
                        i += 1
                        continue
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
                    level["executed"] = True
                    if sl_dist > 0.0:
                        desired = close - sl_dist
                        if self._long_stop is not None and self._long_stop < desired:
                            pass
                        else:
                            self._long_stop = desired
                i += 1
            self._long_martingale_levels = [l for l in self._long_martingale_levels if not l["executed"]]

        if self.Position <= 0:
            i = 0
            while i < len(self._short_martingale_levels):
                level = self._short_martingale_levels[i]
                if level["executed"]:
                    i += 1
                    continue
                if high >= level["price"]:
                    vol = level["volume"]
                    if vol <= 0.0:
                        level["executed"] = True
                        i += 1
                        continue
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()
                    level["executed"] = True
                    if sl_dist > 0.0:
                        desired = close + sl_dist
                        if self._short_stop is not None and self._short_stop > desired:
                            pass
                        else:
                            self._short_stop = desired
                i += 1
            self._short_martingale_levels = [l for l in self._short_martingale_levels if not l["executed"]]

    def _open_long(self, entry_price, volume):
        if volume <= 0.0:
            return

        if self.Position < 0:
            self.BuyMarket()

        self.BuyMarket()

        sl_dist = float(self.StopLossDistance)
        tp_dist = float(self.TakeProfitDistance)
        self._long_stop = entry_price - sl_dist if sl_dist > 0.0 else None
        self._long_take = entry_price + tp_dist if tp_dist > 0.0 else None
        self._short_stop = None
        self._short_take = None

        if self.EnableMartingale:
            self._build_martingale_levels(True, entry_price, volume)
        else:
            self._long_martingale_levels = []

        self._short_martingale_levels = []

    def _open_short(self, entry_price, volume):
        if volume <= 0.0:
            return

        if self.Position > 0:
            self.SellMarket()

        self.SellMarket()

        sl_dist = float(self.StopLossDistance)
        tp_dist = float(self.TakeProfitDistance)
        self._short_stop = entry_price + sl_dist if sl_dist > 0.0 else None
        self._short_take = entry_price - tp_dist if tp_dist > 0.0 else None
        self._long_stop = None
        self._long_take = None

        if self.EnableMartingale:
            self._build_martingale_levels(False, entry_price, volume)
        else:
            self._short_martingale_levels = []

        self._long_martingale_levels = []

    def _clear_long_state(self):
        self._long_stop = None
        self._long_take = None
        self._long_martingale_levels = []

    def _clear_short_state(self):
        self._short_stop = None
        self._short_take = None
        self._short_martingale_levels = []

    def _build_martingale_levels(self, is_long, entry_price, base_volume):
        target_list = []
        volume = base_volume
        max_vol = float(self.MaxVolume)
        step_dist = float(self.MartingaleStepDistance)
        mult = float(self.MartingaleMultiplier)
        steps = int(self.MartingaleSteps)

        for i in range(1, steps + 1):
            volume *= mult
            if max_vol > 0.0:
                volume = min(volume, max_vol)

            if volume <= 0.0:
                break

            distance = step_dist * i
            if distance <= 0.0:
                break

            price = entry_price - distance if is_long else entry_price + distance

            target_list.append({
                "price": price,
                "volume": volume,
                "executed": False
            })

        if is_long:
            self._long_martingale_levels = target_list
        else:
            self._short_martingale_levels = target_list

    def _get_initial_volume(self):
        volume = float(self.BaseVolume)
        max_vol = float(self.MaxVolume)
        if max_vol > 0.0 and volume > max_vol:
            volume = max_vol
        return volume

    def _add_indicator_value(self, lst, value):
        lst.append(value)
        if len(lst) > self._max_alligator_buffer:
            lst.pop(0)

    def _get_shifted_value(self, lst, shift):
        if shift < 0:
            return None
        index = len(lst) - 1 - shift
        if index < 0 or index >= len(lst):
            return None
        return lst[index]

    def OnReseted(self):
        super(alligator_fractal_martingale_strategy, self).OnReseted()
        self._jaw = None
        self._teeth = None
        self._lips = None
        self._jaw_history = []
        self._teeth_history = []
        self._lips_history = []
        self._high_history = []
        self._low_history = []
        self._up_fractals = []
        self._down_fractals = []
        self._long_martingale_levels = []
        self._short_martingale_levels = []
        self._current_buy_state = True
        self._current_sell_state = True
        self._prev_buy_state = True
        self._prev_sell_state = True
        self._active_up_fractal = None
        self._active_down_fractal = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._finished_bar_index = -1
        self._history_offset = 0
        self._max_alligator_buffer = 0

    def CreateClone(self):
        return alligator_fractal_martingale_strategy()
