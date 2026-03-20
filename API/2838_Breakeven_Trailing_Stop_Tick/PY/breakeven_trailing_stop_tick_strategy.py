import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class breakeven_trailing_stop_tick_strategy(Strategy):
    def __init__(self):
        super(breakeven_trailing_stop_tick_strategy, self).__init__()

        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 1.0)
        self._enable_demo_entries = self.Param("EnableDemoEntries", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._point_value = 0.0
        self._long_stop_price = None
        self._short_stop_price = None
        self._exit_order_pending = False
        self._entry_price = 0.0
        self._last_demo_entry_time = None
        self._candle_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(breakeven_trailing_stop_tick_strategy, self).OnStarted(time)

        self._point_value = self._calculate_adjusted_point()
        self._candle_count = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        self._candle_count += 1

        if self._enable_demo_entries.Value:
            self._try_create_demo_entry(candle, price)

        if self.Position == 0:
            self._reset_trailing_state()
            return

        if self._trailing_stop_pips.Value <= 0 or self._point_value <= 0:
            return

        if self.Position > 0:
            self._update_long_trailing(price)
        elif self.Position < 0:
            self._update_short_trailing(price)

    def _try_create_demo_entry(self, candle, price):
        if self.Position != 0 or self._exit_order_pending:
            return

        server_time = candle.CloseTime
        if self._last_demo_entry_time is not None and (server_time - self._last_demo_entry_time).TotalMinutes < 30:
            return

        volume = float(self.Volume)
        if volume <= 0:
            return

        # Use candle count parity as deterministic pseudo-random for demo entries
        if self._candle_count % 2 == 0:
            self.BuyMarket(volume)
            self._entry_price = price
        else:
            self.SellMarket(volume)
            self._entry_price = price

        self._last_demo_entry_time = server_time

    def _update_long_trailing(self, current_price):
        entry_price = self._entry_price
        if entry_price <= 0:
            return

        stop_offset = self._trailing_stop_pips.Value * self._point_value
        step_offset = self._trailing_step_pips.Value * self._point_value
        if stop_offset <= 0:
            return

        activation_offset = stop_offset + step_offset
        if current_price - entry_price <= activation_offset:
            return

        threshold = current_price - activation_offset
        if self._long_stop_price is None or self._long_stop_price < threshold:
            new_stop = current_price - stop_offset
            if new_stop > 0:
                self._long_stop_price = new_stop

        if self._long_stop_price is not None and current_price <= self._long_stop_price:
            self._exit_long_position()

    def _update_short_trailing(self, current_price):
        entry_price = self._entry_price
        if entry_price <= 0:
            return

        stop_offset = self._trailing_stop_pips.Value * self._point_value
        step_offset = self._trailing_step_pips.Value * self._point_value
        if stop_offset <= 0:
            return

        activation_offset = stop_offset + step_offset
        if entry_price - current_price <= activation_offset:
            return

        threshold = current_price + activation_offset
        if self._short_stop_price is None or self._short_stop_price > threshold:
            new_stop = current_price + stop_offset
            self._short_stop_price = new_stop

        if self._short_stop_price is not None and current_price >= self._short_stop_price:
            self._exit_short_position()

    def _exit_long_position(self):
        if self._exit_order_pending:
            return
        volume = abs(self.Position)
        if volume <= 0:
            return
        self.SellMarket(volume)
        self._exit_order_pending = True

    def _exit_short_position(self):
        if self._exit_order_pending:
            return
        volume = abs(self.Position)
        if volume <= 0:
            return
        self.BuyMarket(volume)
        self._exit_order_pending = True

    def _reset_trailing_state(self):
        self._long_stop_price = None
        self._short_stop_price = None
        self._exit_order_pending = False
        self._entry_price = 0.0

    def _calculate_adjusted_point(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0:
            return 1.0
        decimals = self._count_decimals(step)
        return step * 10.0 if decimals == 3 or decimals == 5 else step

    def _count_decimals(self, value):
        value = abs(value)
        decimals = 0
        while value != int(value) and decimals < 10:
            value *= 10.0
            decimals += 1
        return decimals

    def OnReseted(self):
        super(breakeven_trailing_stop_tick_strategy, self).OnReseted()
        self._point_value = 0.0
        self._long_stop_price = None
        self._short_stop_price = None
        self._exit_order_pending = False
        self._entry_price = 0.0
        self._last_demo_entry_time = None
        self._candle_count = 0

    def CreateClone(self):
        return breakeven_trailing_stop_tick_strategy()
