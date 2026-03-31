import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class maximus_vx_lite_strategy(Strategy):
    def __init__(self):
        super(maximus_vx_lite_strategy, self).__init__()

        self._delay_open = self.Param("DelayOpen", 2)
        self._distance_points = self.Param("DistancePoints", 850)
        self._range_points = self.Param("RangePoints", 500)
        self._history_depth = self.Param("HistoryDepth", 200)
        self._range_lookback = self.Param("RangeLookback", 40)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._stop_loss_points = self.Param("StopLossPoints", 1000)

        self._history = []
        self._upper_max = 0.0
        self._upper_min = 0.0
        self._lower_max = 0.0
        self._lower_min = 0.0
        self._price_step = 1.0
        self._ext_distance = 0.0
        self._ext_range = 0.0
        self._ext_stop_loss = 0.0
        self._last_buy_time = None
        self._last_sell_time = None
        self._active_stop = None
        self._active_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(maximus_vx_lite_strategy, self).OnStarted2(time)

        self._update_derived_values()

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _update_derived_values(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0
        self._price_step = step
        self._ext_distance = self._distance_points.Value * step
        self._ext_range = self._range_points.Value * step
        self._ext_stop_loss = self._stop_loss_points.Value * step

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_derived_values()

        self._update_history(candle)

        if self.Position == 0:
            self._active_stop = None
            self._active_take = None

        self._find_high_low(float(candle.ClosePrice))

        if self._handle_stops_and_targets(candle):
            return

        self._try_enter_positions(candle)

    def _update_history(self, candle):
        self._history.insert(0, (float(candle.HighPrice), float(candle.LowPrice)))
        while len(self._history) > self._history_depth.Value:
            self._history.pop()

    def _find_high_low(self, current_close):
        if len(self._history) == 0:
            return

        recalc = (current_close - 100.0 * self._price_step > self._lower_max or
                  current_close + 100.0 * self._price_step < self._lower_min or
                  current_close - 100.0 * self._price_step > self._upper_max or
                  current_close + 100.0 * self._price_step < self._upper_min)

        if not recalc:
            return

        half_range = self._range_points.Value * 0.5 * self._price_step

        found_upper = False
        for i in range(len(self._history)):
            high = self._history[i][0]
            if current_close - self._ext_range <= high:
                continue
            window_max, window_min = self._get_range_window(i)
            if window_max == 0 and window_min == 0:
                continue
            if (window_max - window_min <= self._ext_range and
                    current_close + self._ext_range > window_max and
                    current_close + self._ext_range > window_min):
                found_upper = True
                break

        base_value = Math.Floor(current_close + 100.0 * self._price_step)
        if found_upper:
            base_value = Math.Floor((current_close + 100.0 * self._price_step) * 100.0) / 100.0
        self._upper_max = base_value + half_range
        self._upper_min = base_value - half_range

        lower_found = False
        lower_max = 0.0
        lower_min = 0.0

        for i in range(len(self._history)):
            high = self._history[i][0]
            if current_close - self._ext_range <= high:
                continue
            window_max, window_min = self._get_range_window(i)
            if window_max == 0 and window_min == 0:
                continue
            if (window_max - window_min <= self._ext_range and
                    current_close - self._ext_range > window_max and
                    current_close - self._ext_range > window_min):
                lower_max = window_max
                lower_min = window_min
                lower_found = True
                break

        if not lower_found:
            base_value = Math.Floor((current_close - 100.0 * self._price_step) * 100.0) / 100.0
            lower_max = base_value + half_range
            lower_min = base_value - half_range

        self._lower_max = lower_max
        self._lower_min = lower_min

    def _get_range_window(self, start_index):
        count = min(self._range_lookback.Value, len(self._history) - start_index)
        if count <= 0:
            return (0.0, 0.0)

        max_val = -1e18
        min_val = 1e18

        for j in range(count):
            index = start_index + j
            if index >= len(self._history):
                break
            h, l = self._history[index]
            if h > max_val:
                max_val = h
            if l < min_val:
                min_val = l

        return (max_val, min_val)

    def _handle_stops_and_targets(self, candle):
        if self.Position > 0:
            if self._active_stop is not None and float(candle.LowPrice) <= self._active_stop:
                self.SellMarket(self.Position)
                self._reset_after_exit()
                return True
            if self._active_take is not None and float(candle.HighPrice) >= self._active_take:
                self.SellMarket(self.Position)
                self._reset_after_exit()
                return True
        elif self.Position < 0:
            vol = abs(self.Position)
            if self._active_stop is not None and float(candle.HighPrice) >= self._active_stop:
                self.BuyMarket(vol)
                self._reset_after_exit()
                return True
            if self._active_take is not None and float(candle.LowPrice) <= self._active_take:
                self.BuyMarket(vol)
                self._reset_after_exit()
                return True
        return False

    def _try_enter_positions(self, candle):
        price = float(candle.ClosePrice)
        if price <= 0:
            return

        now = candle.CloseTime

        has_long = self.Position > 0
        has_short = self.Position < 0

        allow_buy = not has_long
        allow_sell = not has_short

        if self._delay_open.Value == 0:
            if has_long:
                allow_buy = False
            if has_short:
                allow_sell = False

        buy_primary = self._lower_max != 0 and self._upper_min != 0 and price - self._ext_distance > self._lower_max
        buy_secondary = self._upper_max != 0 and price - self._ext_distance > self._upper_max

        if allow_buy and (buy_primary or buy_secondary) and self.Position <= 0:
            stop_price = price - self._ext_stop_loss if self._stop_loss_points.Value > 0 else None

            if buy_primary:
                diff = self._upper_min - self._lower_max
                temp_tp = diff / 3.0 * 2.0 * self._price_step
                if temp_tp < self._ext_range:
                    temp_tp = self._ext_range
                take_price = price + temp_tp
            else:
                take_price = price + 2.0 * self._ext_range

            order_volume = float(self.Volume) + (abs(self.Position) if self.Position < 0 else 0)
            self.BuyMarket(order_volume)
            self._active_stop = stop_price
            self._active_take = take_price
            self._last_buy_time = now
            return

        sell_primary = self._upper_min != 0 and price + self._ext_distance < self._upper_min
        sell_secondary = self._lower_min != 0 and price + self._ext_distance < self._lower_min

        if allow_sell and (sell_primary or sell_secondary) and self.Position >= 0:
            stop_price = price + self._ext_stop_loss if self._stop_loss_points.Value > 0 else None

            if sell_primary:
                diff = self._upper_min - self._lower_max
                temp_tp = diff / 3.0 * 2.0 * self._price_step
                if temp_tp < self._ext_range:
                    temp_tp = self._ext_range
                take_price = price - temp_tp
            else:
                take_price = price - 2.0 * self._ext_range

            order_volume = float(self.Volume) + (abs(self.Position) if self.Position > 0 else 0)
            self.SellMarket(order_volume)
            self._active_stop = stop_price
            self._active_take = take_price
            self._last_sell_time = now

    def _reset_after_exit(self):
        self._active_stop = None
        self._active_take = None
        self._last_buy_time = None
        self._last_sell_time = None

    def OnReseted(self):
        super(maximus_vx_lite_strategy, self).OnReseted()
        self._history = []
        self._upper_max = 0.0
        self._upper_min = 0.0
        self._lower_max = 0.0
        self._lower_min = 0.0
        self._price_step = 1.0
        self._ext_distance = 0.0
        self._ext_range = 0.0
        self._ext_stop_loss = 0.0
        self._last_buy_time = None
        self._last_sell_time = None
        self._active_stop = None
        self._active_take = None

    def CreateClone(self):
        return maximus_vx_lite_strategy()
