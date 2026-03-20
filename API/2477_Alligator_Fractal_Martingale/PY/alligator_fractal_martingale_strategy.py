import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, DecimalIndicatorValue
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
        self._volume_param = self.Param("BaseVolume", 1.0)
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
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BaseVolume(self):
        return self._volume_param.Value

    @property
    def JawLength(self):
        return self._jaw_length.Value

    @property
    def JawShift(self):
        return self._jaw_shift.Value

    @property
    def TeethLength(self):
        return self._teeth_length.Value

    @property
    def TeethShift(self):
        return self._teeth_shift.Value

    @property
    def LipsLength(self):
        return self._lips_length.Value

    @property
    def LipsShift(self):
        return self._lips_shift.Value

    @property
    def EntrySpread(self):
        return self._entry_spread.Value

    @property
    def ExitSpread(self):
        return self._exit_spread.Value

    @property
    def UseAlligatorEntry(self):
        return self._use_alligator_entry.Value

    @property
    def UseFractalFilter(self):
        return self._use_fractal_filter.Value

    @property
    def UseAlligatorExit(self):
        return self._use_alligator_exit.Value

    @property
    def AllowMultipleEntries(self):
        return self._allow_multiple_entries.Value

    @property
    def EnableMartingale(self):
        return self._enable_martingale.Value

    @property
    def EnableTrailing(self):
        return self._enable_trailing.Value

    @property
    def ManualMode(self):
        return self._manual_mode.Value

    @property
    def TakeProfitDistance(self):
        return self._take_profit_distance.Value

    @property
    def StopLossDistance(self):
        return self._stop_loss_distance.Value

    @property
    def TrailingStep(self):
        return self._trailing_step.Value

    @property
    def FractalLookback(self):
        return self._fractal_lookback.Value

    @property
    def FractalBuffer(self):
        return self._fractal_buffer.Value

    @property
    def MartingaleSteps(self):
        return self._martingale_steps.Value

    @property
    def MartingaleMultiplier(self):
        return self._martingale_multiplier.Value

    @property
    def MartingaleStepDistance(self):
        return self._martingale_step_distance.Value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    def OnStarted(self, time):
        super(alligator_fractal_martingale_strategy, self).OnStarted(time)

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = self.JawLength
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = self.TeethLength
        self._lips = SmoothedMovingAverage()
        self._lips.Length = self.LipsLength

        self._max_alligator_buffer = max(self.JawShift, self.TeethShift, self.LipsShift) + 10

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._jaw)
            self.DrawIndicator(area, self._teeth)
            self.DrawIndicator(area, self._lips)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        is_final = candle.State == CandleStates.Finished

        jaw_iv = DecimalIndicatorValue(self._jaw, median, candle.ServerTime)
        jaw_iv.IsFinal = is_final
        jaw_result = self._jaw.Process(jaw_iv)
        if is_final:
            self._add_indicator_value(self._jaw_history, float(jaw_result.ToDecimal()))

        teeth_iv = DecimalIndicatorValue(self._teeth, median, candle.ServerTime)
        teeth_iv.IsFinal = is_final
        teeth_result = self._teeth.Process(teeth_iv)
        if is_final:
            self._add_indicator_value(self._teeth_history, float(teeth_result.ToDecimal()))

        lips_iv = DecimalIndicatorValue(self._lips, median, candle.ServerTime)
        lips_iv.IsFinal = is_final
        lips_result = self._lips.Process(lips_iv)
        if is_final:
            self._add_indicator_value(self._lips_history, float(lips_result.ToDecimal()))

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
        if initial_volume <= 0:
            return

        if long_signal and allow_long:
            if self.AllowMultipleEntries or self.Position <= 0:
                self._open_long(float(candle.ClosePrice), initial_volume)

        if short_signal and allow_short:
            if self.AllowMultipleEntries or self.Position >= 0:
                self._open_short(float(candle.ClosePrice), initial_volume)

    def _try_close_positions_on_alligator(self, candle):
        if not self.UseAlligatorExit:
            return
        if self._prev_buy_state and not self._current_buy_state and self.Position > 0:
            self.SellMarket(self.Position)
            self._clear_long_state()
        if self._prev_sell_state and not self._current_sell_state and self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._clear_short_state()

    def _update_alligator_states(self):
        self._prev_buy_state = self._current_buy_state
        self._prev_sell_state = self._current_sell_state

        jaw = self._get_shifted_value(self._jaw_history, self.JawShift)
        teeth = self._get_shifted_value(self._teeth_history, self.TeethShift)
        lips = self._get_shifted_value(self._lips_history, self.LipsShift)

        if jaw is None or teeth is None or lips is None:
            return

        if lips > jaw + self.EntrySpread:
            self._current_buy_state = True
        if lips + self.ExitSpread < teeth:
            self._current_buy_state = False
        if jaw > lips + self.EntrySpread:
            self._current_sell_state = True
        if jaw + self.ExitSpread < teeth:
            self._current_sell_state = False

    def _update_fractals(self, candle):
        self._high_history.append(float(candle.HighPrice))
        self._low_history.append(float(candle.LowPrice))

        max_history = max(self.FractalLookback + 10, 10)
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

        lookback = self.FractalLookback
        self._up_fractals = [(idx, val) for idx, val in self._up_fractals if self._finished_bar_index - idx <= lookback]
        self._down_fractals = [(idx, val) for idx, val in self._down_fractals if self._finished_bar_index - idx <= lookback]

        close_price = float(candle.ClosePrice)
        self._active_up_fractal = None
        for idx, value in self._up_fractals:
            if value >= close_price + self.FractalBuffer:
                if self._active_up_fractal is None or value > self._active_up_fractal:
                    self._active_up_fractal = value

        self._active_down_fractal = None
        for idx, value in self._down_fractals:
            if value <= close_price - self.FractalBuffer:
                if self._active_down_fractal is None or value < self._active_down_fractal:
                    self._active_down_fractal = value

    def _update_trailing_and_stops(self, candle):
        if self.Position > 0:
            if self.StopLossDistance > 0:
                desired = float(candle.ClosePrice) - self.StopLossDistance
                if self._long_stop is None:
                    self._long_stop = desired
                elif self.EnableTrailing and desired > self._long_stop + self.TrailingStep:
                    self._long_stop = desired

            if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket(self.Position)
                self._clear_long_state()
                return

            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket(self.Position)
                self._clear_long_state()

        elif self.Position < 0:
            short_volume = abs(self.Position)
            if self.StopLossDistance > 0:
                desired = float(candle.ClosePrice) + self.StopLossDistance
                if self._short_stop is None:
                    self._short_stop = desired
                elif self.EnableTrailing and desired < self._short_stop - self.TrailingStep:
                    self._short_stop = desired

            if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket(short_volume)
                self._clear_short_state()
                return

            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket(short_volume)
                self._clear_short_state()

    def _process_martingale_levels(self, candle):
        if not self.EnableMartingale:
            return

        if self.Position >= 0:
            for level in list(self._long_martingale_levels):
                if level["executed"]:
                    continue
                if float(candle.LowPrice) <= level["price"]:
                    volume = self._round_volume(level["volume"])
                    if volume <= 0:
                        level["executed"] = True
                        continue
                    if self.Position < 0:
                        self.BuyMarket(abs(self.Position))
                    self.BuyMarket(volume)
                    level["executed"] = True
                    if self.StopLossDistance > 0:
                        desired = float(candle.ClosePrice) - self.StopLossDistance
                        self._long_stop = min(self._long_stop, desired) if self._long_stop is not None else desired
            self._long_martingale_levels = [l for l in self._long_martingale_levels if not l["executed"]]

        if self.Position <= 0:
            for level in list(self._short_martingale_levels):
                if level["executed"]:
                    continue
                if float(candle.HighPrice) >= level["price"]:
                    volume = self._round_volume(level["volume"])
                    if volume <= 0:
                        level["executed"] = True
                        continue
                    if self.Position > 0:
                        self.SellMarket(self.Position)
                    self.SellMarket(volume)
                    level["executed"] = True
                    if self.StopLossDistance > 0:
                        desired = float(candle.ClosePrice) + self.StopLossDistance
                        self._short_stop = max(self._short_stop, desired) if self._short_stop is not None else desired
            self._short_martingale_levels = [l for l in self._short_martingale_levels if not l["executed"]]

    def _open_long(self, entry_price, volume):
        volume = self._round_volume(volume)
        if volume <= 0:
            return
        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
        self.BuyMarket(volume)
        self._long_stop = entry_price - self.StopLossDistance if self.StopLossDistance > 0 else None
        self._long_take = entry_price + self.TakeProfitDistance if self.TakeProfitDistance > 0 else None
        self._short_stop = None
        self._short_take = None
        if self.EnableMartingale:
            self._build_martingale_levels(True, entry_price, volume)
        else:
            self._long_martingale_levels = []
        self._short_martingale_levels = []

    def _open_short(self, entry_price, volume):
        volume = self._round_volume(volume)
        if volume <= 0:
            return
        if self.Position > 0:
            self.SellMarket(self.Position)
        self.SellMarket(volume)
        self._short_stop = entry_price + self.StopLossDistance if self.StopLossDistance > 0 else None
        self._short_take = entry_price - self.TakeProfitDistance if self.TakeProfitDistance > 0 else None
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
        for i in range(1, self.MartingaleSteps + 1):
            volume *= self.MartingaleMultiplier
            volume = min(volume, self.MaxVolume)
            rounded_volume = self._round_volume(volume)
            if rounded_volume <= 0:
                break
            distance = self.MartingaleStepDistance * i
            if distance <= 0:
                break
            price = entry_price - distance if is_long else entry_price + distance
            target_list.append({"price": price, "volume": rounded_volume, "executed": False})

        if is_long:
            self._long_martingale_levels = target_list
        else:
            self._short_martingale_levels = target_list

    def _get_initial_volume(self):
        volume = self.BaseVolume
        if self.MaxVolume > 0 and volume > self.MaxVolume:
            volume = self.MaxVolume
        return self._round_volume(volume)

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

    def _round_volume(self, volume):
        if volume <= 0:
            return 0.0
        sec = self.Security
        step = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 0.0
        if step > 0:
            volume = math.floor(volume / step) * step
        if volume < 0:
            volume = 0.0
        if self.MaxVolume > 0 and volume > self.MaxVolume:
            volume = self.MaxVolume
        return volume

    def OnReseted(self):
        super(alligator_fractal_martingale_strategy, self).OnReseted()
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
