import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class executor_ao_strategy(Strategy):
    def __init__(self):
        super(executor_ao_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetDisplay("Trade Volume", "Fixed order size", "Risk")
        self._ao_short_period = self.Param("AoShortPeriod", 5) \
            .SetDisplay("AO Short Period", "Fast period for Awesome Oscillator", "Indicators")
        self._ao_long_period = self.Param("AoLongPeriod", 34) \
            .SetDisplay("AO Long Period", "Slow period for Awesome Oscillator", "Indicators")
        self._minimum_ao_indent = self.Param("MinimumAoIndent", 0.001) \
            .SetDisplay("Minimum AO Indent", "Minimum distance from zero before signals are valid", "Logic")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0) \
            .SetDisplay("Take Profit (pips)", "Target distance expressed in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing distance in pips", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetDisplay("Trailing Step (pips)", "Minimum move before trailing adjusts", "Risk")

        self._ao = None
        self._current_ao = None
        self._previous_ao = None
        self._previous_ao2 = None
        self._pip_size = 0.0

        self._long_entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_entry_price = None
        self._short_stop = None
        self._short_take = None

    @property
    def trade_volume(self):
        return self._trade_volume.Value

    @property
    def ao_short_period(self):
        return self._ao_short_period.Value

    @property
    def ao_long_period(self):
        return self._ao_long_period.Value

    @property
    def minimum_ao_indent(self):
        return self._minimum_ao_indent.Value

    @property
    def stop_loss_pips(self):
        return self._stop_loss_pips.Value

    @property
    def take_profit_pips(self):
        return self._take_profit_pips.Value

    @property
    def trailing_stop_pips(self):
        return self._trailing_stop_pips.Value

    @property
    def trailing_step_pips(self):
        return self._trailing_step_pips.Value

    def OnReseted(self):
        super(executor_ao_strategy, self).OnReseted()
        self._ao = None
        self._current_ao = None
        self._previous_ao = None
        self._previous_ao2 = None
        self._pip_size = 0.0
        self._reset_long_state()
        self._reset_short_state()

    def OnStarted(self, time):
        super(executor_ao_strategy, self).OnStarted(time)

        self._pip_size = self._calculate_pip_size()

        self._ao = AwesomeOscillator()
        self._ao.ShortMa.Length = self.ao_short_period
        self._ao.LongMa.Length = self.ao_long_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(2)))
        subscription.Bind(self._process_candle)
        subscription.Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._ao is None:
            return

        ao_result = self._ao.Process(CandleIndicatorValue(self._ao, candle))
        if not self._ao.IsFormed:
            return

        previous_ao = self._current_ao
        previous_ao2 = self._previous_ao

        position_closed = self._handle_active_positions(candle, previous_ao)

        self._store_ao_value(float(ao_result.ToDecimal()))

        if position_closed:
            return
        if previous_ao is None or previous_ao2 is None or self._current_ao is None:
            return
        if self.Position != 0:
            return

        current = self._current_ao
        prev = previous_ao
        prev2 = previous_ao2
        indent = float(self.minimum_ao_indent)

        # Saucer buy: AO dips then rises, current <= -indent
        if current > prev and prev < prev2 and current <= -indent:
            self._open_long(float(candle.ClosePrice))
            return

        # Saucer sell: AO peaks then falls, current >= indent
        if current < prev and prev > prev2 and current >= indent:
            self._open_short(float(candle.ClosePrice))

    def _handle_active_positions(self, candle, previous_ao):
        if self.Position > 0:
            if self._long_entry_price is None:
                self._long_entry_price = float(candle.ClosePrice)
            self._update_trailing_long(candle)

            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket()
                self._reset_long_state()
                return True
            if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket()
                self._reset_long_state()
                return True
            if previous_ao is not None and previous_ao > 0.0:
                self.SellMarket()
                self._reset_long_state()
                return True

        elif self.Position < 0:
            if self._short_entry_price is None:
                self._short_entry_price = float(candle.ClosePrice)
            self._update_trailing_short(candle)

            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket()
                self._reset_short_state()
                return True
            if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket()
                self._reset_short_state()
                return True
            if previous_ao is not None and previous_ao < 0.0:
                self.BuyMarket()
                self._reset_short_state()
                return True
        else:
            self._reset_long_state()
            self._reset_short_state()

        return False

    def _open_long(self, price):
        self.BuyMarket()
        self._long_entry_price = price
        sl_pips = float(self.stop_loss_pips)
        tp_pips = float(self.take_profit_pips)
        self._long_stop = price - sl_pips * self._pip_size if sl_pips > 0.0 else None
        self._long_take = price + tp_pips * self._pip_size if tp_pips > 0.0 else None
        self._reset_short_state()

    def _open_short(self, price):
        self.SellMarket()
        self._short_entry_price = price
        sl_pips = float(self.stop_loss_pips)
        tp_pips = float(self.take_profit_pips)
        self._short_stop = price + sl_pips * self._pip_size if sl_pips > 0.0 else None
        self._short_take = price - tp_pips * self._pip_size if tp_pips > 0.0 else None
        self._reset_long_state()

    def _update_trailing_long(self, candle):
        trail_pips = float(self.trailing_stop_pips)
        step_pips = float(self.trailing_step_pips)
        if trail_pips <= 0.0 or step_pips <= 0.0 or self._long_entry_price is None:
            return

        trailing_distance = trail_pips * self._pip_size
        trailing_step = step_pips * self._pip_size
        price = float(candle.ClosePrice)
        entry = self._long_entry_price

        if price - entry > trailing_distance + trailing_step:
            minimal_allowed = price - (trailing_distance + trailing_step)
            if self._long_stop is None or self._long_stop < minimal_allowed:
                self._long_stop = price - trailing_distance

    def _update_trailing_short(self, candle):
        trail_pips = float(self.trailing_stop_pips)
        step_pips = float(self.trailing_step_pips)
        if trail_pips <= 0.0 or step_pips <= 0.0 or self._short_entry_price is None:
            return

        trailing_distance = trail_pips * self._pip_size
        trailing_step = step_pips * self._pip_size
        price = float(candle.ClosePrice)
        entry = self._short_entry_price

        if entry - price > trailing_distance + trailing_step:
            maximal_allowed = price + (trailing_distance + trailing_step)
            if self._short_stop is None or self._short_stop > maximal_allowed:
                self._short_stop = price + trailing_distance

    def _store_ao_value(self, value):
        self._previous_ao2 = self._previous_ao
        self._previous_ao = self._current_ao
        self._current_ao = value

    def _calculate_pip_size(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        decimals = self._get_decimal_places(step)
        factor = 10.0 if decimals == 3 or decimals == 5 else 1.0
        return step * factor

    def _get_decimal_places(self, value):
        value = abs(value)
        if value == 0.0:
            return 0
        count = 0
        while value != round(value) and count < 10:
            value *= 10.0
            count += 1
        return count

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_stop = None
        self._long_take = None

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_stop = None
        self._short_take = None

    def CreateClone(self):
        return executor_ao_strategy()
