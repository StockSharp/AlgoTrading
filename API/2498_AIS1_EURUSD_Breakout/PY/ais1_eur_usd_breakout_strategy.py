import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class ais1_eur_usd_breakout_strategy(Strategy):
    def __init__(self):
        super(ais1_eur_usd_breakout_strategy, self).__init__()

        self._account_reserve = self.Param("AccountReserve", 0.2)
        self._order_reserve = self.Param("OrderReserve", 0.04)
        self._take_factor = self.Param("TakeFactor", 0.8)
        self._stop_factor = self.Param("StopFactor", 1.0)
        self._trail_factor = self.Param("TrailFactor", 5.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

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
        self._candle_count = 0
        self._day_high = 0.0
        self._day_low = 999999999.0
        self._day_close = 0.0
        self._day_candle_count = 0

    @property
    def AccountReserve(self):
        return self._account_reserve.Value

    @AccountReserve.setter
    def AccountReserve(self, value):
        self._account_reserve.Value = value

    @property
    def OrderReserve(self):
        return self._order_reserve.Value

    @OrderReserve.setter
    def OrderReserve(self, value):
        self._order_reserve.Value = value

    @property
    def TakeFactor(self):
        return self._take_factor.Value

    @TakeFactor.setter
    def TakeFactor(self, value):
        self._take_factor.Value = value

    @property
    def StopFactor(self):
        return self._stop_factor.Value

    @StopFactor.setter
    def StopFactor(self, value):
        self._stop_factor.Value = value

    @property
    def TrailFactor(self):
        return self._trail_factor.Value

    @TrailFactor.setter
    def TrailFactor(self, value):
        self._trail_factor.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ais1_eur_usd_breakout_strategy, self).OnStarted(time)

        self._reset_position_state()
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._prev_day_close = 0.0
        self._prev_trail_range = 0.0
        self._has_prev_day = False
        self._has_prev_trail = False
        self._candle_count = 0
        self._day_high = 0.0
        self._day_low = 999999999.0
        self._day_close = 0.0
        self._day_candle_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        self._candle_count += 1
        self._day_candle_count += 1

        if high > self._day_high:
            self._day_high = high
        if low < self._day_low:
            self._day_low = low
        self._day_close = close

        day_bars = 288
        if self._day_candle_count >= day_bars:
            self._prev_day_high = self._day_high
            self._prev_day_low = self._day_low
            self._prev_day_close = self._day_close
            self._has_prev_day = True
            self._day_high = 0.0
            self._day_low = 999999999.0
            self._day_close = 0.0
            self._day_candle_count = 0

        trail_range = high - low
        self._prev_trail_range = trail_range
        self._has_prev_trail = True

        if not self._has_prev_day:
            return

        day_range = self._prev_day_high - self._prev_day_low
        if day_range <= 0.0:
            return

        average = (self._prev_day_high + self._prev_day_low) / 2.0
        take_distance = day_range * float(self.TakeFactor)
        stop_distance = day_range * float(self.StopFactor)

        trail_r = self._prev_trail_range if self._has_prev_trail else trail_range
        trail_distance = trail_r * float(self.TrailFactor)

        if self.Position != 0:
            self._handle_existing_position(candle, trail_distance)
            return

        self._try_enter_position(candle, average, stop_distance, take_distance)

    def _handle_existing_position(self, candle, trail_distance):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_take > 0.0 and high >= self._long_take:
                self.SellMarket()
                self._reset_position_state()
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
                self._reset_position_state()

        elif self.Position < 0:
            if self._short_take > 0.0 and low <= self._short_take:
                self.BuyMarket()
                self._reset_position_state()
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
                self._reset_position_state()

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

            self.BuyMarket()
            self._entry_price = entry_price
            self._long_stop = stop_price
            self._long_take = entry_price + take_distance
            self._long_trail = 0.0
            self._short_stop = 0.0
            self._short_take = 0.0
            self._short_trail = 0.0

        elif breakout_down:
            entry_price = close
            stop_price = self._prev_day_low + stop_distance
            risk = stop_price - entry_price
            if risk <= 0.0:
                return

            self.SellMarket()
            self._entry_price = entry_price
            self._short_stop = stop_price
            self._short_take = entry_price - take_distance
            self._short_trail = 0.0
            self._long_stop = 0.0
            self._long_take = 0.0
            self._long_trail = 0.0

    def _reset_position_state(self):
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._long_trail = 0.0
        self._short_trail = 0.0

    def OnReseted(self):
        super(ais1_eur_usd_breakout_strategy, self).OnReseted()
        self._reset_position_state()
        self._prev_day_high = 0.0
        self._prev_day_low = 0.0
        self._prev_day_close = 0.0
        self._prev_trail_range = 0.0
        self._has_prev_day = False
        self._has_prev_trail = False
        self._candle_count = 0
        self._day_high = 0.0
        self._day_low = 999999999.0
        self._day_close = 0.0
        self._day_candle_count = 0

    def CreateClone(self):
        return ais1_eur_usd_breakout_strategy()
