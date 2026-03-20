import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import AwesomeOscillator, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class aocci_strategy(Strategy):
    def __init__(self):
        super(aocci_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._cci_period = self.Param("CciPeriod", 55)
        self._signal_candle_shift = self.Param("SignalCandleShift", 0)
        self._big_jump_pips = self.Param("BigJumpPips", 100.0)
        self._double_jump_pips = self.Param("DoubleJumpPips", 100.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._ao = None
        self._cci = None

        self._last_ao_value = None
        self._cci_values = []
        self._max_cci_values = 0

        self._recent_candles = []
        self._max_recent_candles = 0

        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._pip_size = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(aocci_strategy, self).OnStarted(time)

        self._pip_size = self._calculate_pip_size()

        self._ao = AwesomeOscillator()
        self._cci = CommodityChannelIndex()
        self._cci.Length = self._cci_period.Value

        self._max_cci_values = max(self._signal_candle_shift.Value + 2, 2)
        self._max_recent_candles = max(self._signal_candle_shift.Value + 2, 6)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._ao, self._cci, self._process_candle).Start()

    def _process_candle(self, candle, ao_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        ao_val = float(ao_value)
        cci_val = float(cci_value)

        self._update_recent_candles(candle)
        self._update_cci_queue(cci_val)

        closed_position = self._handle_active_positions(candle)

        if self._ao is None or self._cci is None:
            self._last_ao_value = ao_val
            return

        if not self._ao.IsFormed or not self._cci.IsFormed:
            self._last_ao_value = ao_val
            return

        if self._last_ao_value is None:
            self._last_ao_value = ao_val
            return

        if len(self._cci_values) <= self._signal_candle_shift.Value + 1:
            self._last_ao_value = ao_val
            return

        if len(self._recent_candles) < 6:
            self._last_ao_value = ao_val
            return

        cci_shift0 = self._try_get_cci_value(self._signal_candle_shift.Value)
        cci_shift1 = self._try_get_cci_value(self._signal_candle_shift.Value + 1)

        if cci_shift0 is None or cci_shift1 is None:
            self._last_ao_value = ao_val
            return

        pivot_source = self._try_get_recent_candle(self._signal_candle_shift.Value + 1)
        if pivot_source is None:
            self._last_ao_value = ao_val
            return

        if self._should_skip_due_to_jumps():
            self._last_ao_value = ao_val
            return

        if closed_position or self.Position != 0:
            self._last_ao_value = ao_val
            return

        pivot = (float(pivot_source.HighPrice) + float(pivot_source.LowPrice) + float(pivot_source.ClosePrice)) / 3.0
        ao_prev = self._last_ao_value
        price = float(candle.ClosePrice)

        # Long condition from original MQL logic
        open_long = ao_val > 0 and cci_shift0 >= 0 and price > pivot and (ao_prev < 0 or cci_shift1 <= 0)

        # Short condition mirrors the original code
        open_short = ao_val > 0 and cci_shift0 >= 0 and price > pivot and (ao_prev < 0 or cci_shift1 <= 0)

        if open_long:
            volume = float(self.Volume) + abs(self.Position)
            if volume > 0:
                self.BuyMarket(volume)
                self._long_entry_price = price
                self._long_stop = price - self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else None
                self._long_take = price + self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else None
                self._reset_short_state()
        elif open_short:
            volume = float(self.Volume) + abs(self.Position)
            if volume > 0:
                self.SellMarket(volume)
                self._short_entry_price = price
                self._short_stop = price + self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else None
                self._short_take = price - self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else None
                self._reset_long_state()

        self._last_ao_value = ao_val

    def _handle_active_positions(self, candle):
        closed = False

        if self.Position > 0:
            if self._long_entry_price is None:
                self._long_entry_price = float(candle.ClosePrice)
            self._update_trailing_for_long(candle)

            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket(abs(self.Position))
                self._reset_long_state()
                closed = True
            elif self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket(abs(self.Position))
                self._reset_long_state()
                closed = True

        elif self.Position < 0:
            if self._short_entry_price is None:
                self._short_entry_price = float(candle.ClosePrice)
            self._update_trailing_for_short(candle)

            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket(abs(self.Position))
                self._reset_short_state()
                closed = True
            elif self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket(abs(self.Position))
                self._reset_short_state()
                closed = True
        else:
            self._reset_long_state()
            self._reset_short_state()

        return closed

    def _update_trailing_for_long(self, candle):
        if self._trailing_stop_pips.Value <= 0 or self._trailing_step_pips.Value <= 0 or self._long_entry_price is None:
            return
        trailing_stop = self._trailing_stop_pips.Value * self._pip_size
        trailing_step = self._trailing_step_pips.Value * self._pip_size
        price = float(candle.ClosePrice)
        entry = self._long_entry_price

        if price - entry > trailing_stop + trailing_step:
            minimal = price - (trailing_stop + trailing_step)
            if self._long_stop is None or self._long_stop < minimal:
                self._long_stop = price - trailing_stop

    def _update_trailing_for_short(self, candle):
        if self._trailing_stop_pips.Value <= 0 or self._trailing_step_pips.Value <= 0 or self._short_entry_price is None:
            return
        trailing_stop = self._trailing_stop_pips.Value * self._pip_size
        trailing_step = self._trailing_step_pips.Value * self._pip_size
        price = float(candle.ClosePrice)
        entry = self._short_entry_price

        if entry - price > trailing_stop + trailing_step:
            maximal = price + (trailing_stop + trailing_step)
            if self._short_stop is None or self._short_stop > maximal:
                self._short_stop = price + trailing_stop

    def _update_cci_queue(self, value):
        self._cci_values.append(value)
        while len(self._cci_values) > self._max_cci_values:
            self._cci_values.pop(0)

    def _update_recent_candles(self, candle):
        self._recent_candles.append(candle)
        while len(self._recent_candles) > self._max_recent_candles:
            self._recent_candles.pop(0)

    def _try_get_cci_value(self, shift):
        if shift < 0 or shift >= len(self._cci_values):
            return None
        target_index = len(self._cci_values) - 1 - shift
        if target_index < 0 or target_index >= len(self._cci_values):
            return None
        return self._cci_values[target_index]

    def _try_get_recent_candle(self, shift):
        if shift < 0 or shift >= len(self._recent_candles):
            return None
        target_index = len(self._recent_candles) - 1 - shift
        if target_index < 0 or target_index >= len(self._recent_candles):
            return None
        return self._recent_candles[target_index]

    def _should_skip_due_to_jumps(self):
        if self._pip_size <= 0:
            return False

        big_jump = self._big_jump_pips.Value * self._pip_size
        double_jump = self._double_jump_pips.Value * self._pip_size

        if self._big_jump_pips.Value > 0:
            for i in range(5):
                if abs(self._get_open_difference(i, i + 1)) >= big_jump:
                    return True

        if self._double_jump_pips.Value > 0:
            for i in range(4):
                if abs(self._get_open_difference(i, i + 2)) >= double_jump:
                    return True

        return False

    def _get_open_difference(self, first_shift, second_shift):
        first = self._try_get_recent_candle(first_shift)
        second = self._try_get_recent_candle(second_shift)
        if first is None or second is None:
            return 0.0
        return float(second.OpenPrice) - float(first.OpenPrice)

    def _calculate_pip_size(self):
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if price_step <= 0:
            price_step = 1.0
        decimals = self._get_decimal_places(price_step)
        factor = 10.0 if decimals == 3 or decimals == 5 else 1.0
        return price_step * factor

    def _get_decimal_places(self, value):
        value = abs(value)
        decimals = 0
        while value != int(value) and decimals < 10:
            value *= 10.0
            decimals += 1
        return decimals

    def _reset_long_state(self):
        self._long_stop = None
        self._long_take = None
        self._long_entry_price = None

    def _reset_short_state(self):
        self._short_stop = None
        self._short_take = None
        self._short_entry_price = None

    def OnReseted(self):
        super(aocci_strategy, self).OnReseted()
        self._ao = None
        self._cci = None
        self._last_ao_value = None
        self._cci_values = []
        self._max_cci_values = 0
        self._recent_candles = []
        self._max_recent_candles = 0
        self._reset_long_state()
        self._reset_short_state()
        self._pip_size = 0.0

    def CreateClone(self):
        return aocci_strategy()
