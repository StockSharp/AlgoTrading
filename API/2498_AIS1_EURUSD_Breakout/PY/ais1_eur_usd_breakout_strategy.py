import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ais1_eur_usd_breakout_strategy(Strategy):
    def __init__(self):
        super(ais1_eur_usd_breakout_strategy, self).__init__()

        self._account_reserve = self.Param("AccountReserve", 0.2)
        self._order_reserve = self.Param("OrderReserve", 0.04)
        self._take_factor = self.Param("TakeFactor", 0.8)
        self._stop_factor = self.Param("StopFactor", 1.0)
        self._trail_factor = self.Param("TrailFactor", 5.0)
        self._entry_candle_type = self.Param("EntryCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._trail_candle_type = self.Param("TrailCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._prev_day_close = 0.0
        self._prev_trail_range = 0.0
        self._has_prev_day = False
        self._has_prev_trail = False
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._long_trail = 0.0
        self._short_trail = 0.0
        self._max_equity = 0.0
        self._next_action_time = None

    @property
    def CandleType(self):
        return self._entry_candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._entry_candle_type.Value = value

    def OnStarted(self, time):
        super(ais1_eur_usd_breakout_strategy, self).OnStarted(time)

        self._reset_position_state()
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._prev_day_close = 0.0
        self._prev_trail_range = 0.0
        self._has_prev_day = False
        self._has_prev_trail = False
        self._max_equity = self._get_equity()
        self._next_action_time = None

        daily_sub = self.SubscribeCandles(self._entry_candle_type.Value)
        daily_sub.Bind(self._process_daily_candle).Start()

        intraday_sub = self.SubscribeCandles(self._trail_candle_type.Value)
        intraday_sub.Bind(self._process_intraday_candle).Start()

    def _process_daily_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._prev_day_high = float(candle.HighPrice)
        self._prev_day_low = float(candle.LowPrice)
        self._prev_day_close = float(candle.ClosePrice)
        self._has_prev_day = True

    def _process_intraday_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._next_action_time is not None and candle.CloseTime <= self._next_action_time:
            self._update_trail_range(candle)
            return

        equity = self._get_equity()
        self._update_max_equity(equity)

        if self._is_drawdown_breached(equity):
            self._update_trail_range(candle)
            return

        if not self._has_prev_day:
            self._update_trail_range(candle)
            return

        day_range = self._prev_day_high - self._prev_day_low
        if day_range <= 0.0:
            self._update_trail_range(candle)
            return

        average = (self._prev_day_high + self._prev_day_low) / 2.0
        take_distance = day_range * float(self._take_factor.Value)
        stop_distance = day_range * float(self._stop_factor.Value)

        trail_r = self._prev_trail_range if self._has_prev_trail else (float(candle.HighPrice) - float(candle.LowPrice))
        trail_distance = trail_r * float(self._trail_factor.Value)

        if self.Position != 0:
            self._handle_existing_position(candle, trail_distance)
            self._update_trail_range(candle)
            return

        self._try_enter_position(candle, average, stop_distance, take_distance)
        self._update_trail_range(candle)

    def _handle_existing_position(self, candle, trail_distance):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_take > 0.0 and high >= self._long_take:
                self.SellMarket()
                self._reset_after_exit(candle.CloseTime)
                return

            trailing_stop = self._long_stop

            if trail_distance > 0.0 and close > self._entry_price:
                candidate = close - trail_distance
                if self._long_trail == 0.0 or candidate > self._long_trail:
                    self._long_trail = candidate

            if self._long_trail > 0.0:
                if trailing_stop > 0.0:
                    trailing_stop = max(trailing_stop, self._long_trail)
                else:
                    trailing_stop = self._long_trail

            if trailing_stop > 0.0 and low <= trailing_stop:
                self.SellMarket()
                self._reset_after_exit(candle.CloseTime)

        elif self.Position < 0:
            if self._short_take > 0.0 and low <= self._short_take:
                self.BuyMarket()
                self._reset_after_exit(candle.CloseTime)
                return

            trailing_stop = self._short_stop

            if trail_distance > 0.0 and close < self._entry_price:
                candidate = close + trail_distance
                if self._short_trail == 0.0 or candidate < self._short_trail:
                    self._short_trail = candidate

            if self._short_trail > 0.0:
                if trailing_stop > 0.0:
                    trailing_stop = min(trailing_stop, self._short_trail)
                else:
                    trailing_stop = self._short_trail

            if trailing_stop > 0.0 and high >= trailing_stop:
                self.BuyMarket()
                self._reset_after_exit(candle.CloseTime)

    def _try_enter_position(self, candle, average, stop_distance, take_distance):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        breakout_up = self._prev_day_close > average and high > self._prev_day_high
        breakout_down = self._prev_day_close < average and low < self._prev_day_low

        if breakout_up:
            entry_price = close
            stop_price = self._prev_day_high - stop_distance
            risk = entry_price - stop_price
            if risk <= 0.0:
                return

            volume = self._calculate_position_size(risk)
            if volume <= 0.0:
                return

            self.BuyMarket()
            self._entry_price = entry_price
            self._long_stop = stop_price
            self._long_take = entry_price + take_distance
            self._long_trail = 0.0
            self._short_stop = 0.0
            self._short_take = 0.0
            self._short_trail = 0.0
            self._next_action_time = candle.CloseTime.Add(TimeSpan.FromSeconds(5))

        elif breakout_down:
            entry_price = close
            stop_price = self._prev_day_low + stop_distance
            risk = stop_price - entry_price
            if risk <= 0.0:
                return

            volume = self._calculate_position_size(risk)
            if volume <= 0.0:
                return

            self.SellMarket()
            self._entry_price = entry_price
            self._short_stop = stop_price
            self._short_take = entry_price - take_distance
            self._short_trail = 0.0
            self._long_stop = 0.0
            self._long_take = 0.0
            self._long_trail = 0.0
            self._next_action_time = candle.CloseTime.Add(TimeSpan.FromSeconds(5))

    def _calculate_position_size(self, risk_per_unit):
        if risk_per_unit <= 0.0:
            return 0.0

        equity = self._get_equity()
        if equity <= 0.0:
            return 0.0

        max_risk = equity * float(self._order_reserve.Value)
        if max_risk <= 0.0:
            return 0.0

        raw_size = max_risk / risk_per_unit
        if raw_size <= 0.0:
            return 0.0

        sec = self.Security
        step = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 1.0
        min_volume = float(sec.MinVolume) if sec is not None and sec.MinVolume is not None else step
        max_volume = float(sec.MaxVolume) if sec is not None and sec.MaxVolume is not None else max(min_volume, step * 1000.0)

        import math
        steps = math.floor(raw_size / step)
        volume = steps * step

        if volume < min_volume:
            if raw_size >= min_volume:
                volume = min_volume
            else:
                return 0.0

        if volume > max_volume:
            volume = max_volume

        return volume

    def _update_trail_range(self, candle):
        self._prev_trail_range = float(candle.HighPrice) - float(candle.LowPrice)
        self._has_prev_trail = True

    def _reset_after_exit(self, time):
        self._reset_position_state()
        self._next_action_time = time.Add(TimeSpan.FromSeconds(5))

    def _reset_position_state(self):
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._long_trail = 0.0
        self._short_trail = 0.0

    def _update_max_equity(self, equity):
        if equity > self._max_equity:
            self._max_equity = equity

    def _is_drawdown_breached(self, equity):
        if self._max_equity <= 0.0:
            return False
        drawdown_limit = float(self._account_reserve.Value) - float(self._order_reserve.Value)
        if drawdown_limit <= 0.0:
            return False
        threshold = self._max_equity * (1.0 - drawdown_limit)
        return equity < threshold

    def _get_equity(self):
        pf = self.Portfolio
        if pf is None:
            return 0.0
        if pf.CurrentValue is not None:
            return float(pf.CurrentValue)
        if pf.BeginValue is not None:
            return float(pf.BeginValue)
        return 0.0

    def OnReseted(self):
        super(ais1_eur_usd_breakout_strategy, self).OnReseted()
        self._reset_position_state()
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._prev_day_close = 0.0
        self._prev_trail_range = 0.0
        self._has_prev_day = False
        self._has_prev_trail = False
        self._max_equity = 0.0
        self._next_action_time = None

    def CreateClone(self):
        return ais1_eur_usd_breakout_strategy()
