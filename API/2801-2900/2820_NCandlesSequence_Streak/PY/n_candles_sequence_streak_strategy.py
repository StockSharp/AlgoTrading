import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class n_candles_sequence_streak_strategy(Strategy):
    def __init__(self):
        super(n_candles_sequence_streak_strategy, self).__init__()

        self._consecutive_candles = self.Param("ConsecutiveCandles", 4)
        self._take_profit_pips = self.Param("TakeProfitPips", 50)
        self._stop_loss_pips = self.Param("StopLossPips", 50)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10)
        self._trailing_step_pips = self.Param("TrailingStepPips", 4)
        self._max_positions = self.Param("MaxPositions", 2)
        self._use_trade_hours = self.Param("UseTradeHours", False)
        self._start_hour = self.Param("StartHour", 11)
        self._end_hour = self.Param("EndHour", 18)
        self._min_profit = self.Param("MinProfit", 3.0)
        self._closing_behavior = self.Param("ClosingBehavior", 0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._streak_count = 0
        self._last_direction = 0
        self._pattern_direction = 0
        self._entries_in_direction = 0
        self._black_sheep_triggered = False
        self._has_position = False
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(n_candles_sequence_streak_strategy, self).OnStarted2(time)

        self._pip_size = self._calculate_pip_size()

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_trailing_stops(candle)
        self._manage_floating_profit(candle)

        direction = self._get_candle_direction(candle)

        if direction == 0:
            self._handle_pattern_break()
            self._last_direction = 0
            self._streak_count = 0
            return

        if self._last_direction == direction:
            self._streak_count += 1
        else:
            if self._last_direction != 0:
                self._handle_pattern_break()
            self._last_direction = direction
            self._streak_count = 1

        if self._streak_count >= self._consecutive_candles.Value:
            if self._pattern_direction != direction:
                self._pattern_direction = direction
                self._entries_in_direction = 0
                self._black_sheep_triggered = False

            if direction > 0:
                self._try_enter_long(candle)
            else:
                self._try_enter_short(candle)

        self._manage_exits(candle)

    def _manage_floating_profit(self, candle):
        if self._min_profit.Value <= 0 or self.Position == 0 or not self._has_position:
            return

        floating = self._calculate_open_profit(float(candle.ClosePrice))
        if floating >= self._min_profit.Value:
            self._close_position()

    def _try_enter_long(self, candle):
        if self._entries_in_direction >= self._max_positions.Value:
            return

        if self._use_trade_hours.Value and not self._is_within_trade_hours(candle.CloseTime):
            return

        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._reset_position_state()
            return

        if self.Position != 0:
            return

        self.BuyMarket()
        self._initialize_long_state(float(candle.ClosePrice))

    def _try_enter_short(self, candle):
        if self._entries_in_direction >= self._max_positions.Value:
            return

        if self._use_trade_hours.Value and not self._is_within_trade_hours(candle.CloseTime):
            return

        if self.Position > 0:
            self.SellMarket(self.Position)
            self._reset_position_state()
            return

        if self.Position != 0:
            return

        self.SellMarket()
        self._initialize_short_state(float(candle.ClosePrice))

    def _initialize_long_state(self, entry_price):
        self._has_position = True
        self._entry_price = entry_price
        self._entries_in_direction = 1
        self._black_sheep_triggered = False

        stop_distance = self._to_price(self._stop_loss_pips.Value) if self._stop_loss_pips.Value > 0 else None
        take_distance = self._to_price(self._take_profit_pips.Value) if self._take_profit_pips.Value > 0 else None

        self._long_stop = entry_price - stop_distance if stop_distance is not None else None
        self._long_take = entry_price + take_distance if take_distance is not None else None
        self._short_stop = None
        self._short_take = None

    def _initialize_short_state(self, entry_price):
        self._has_position = True
        self._entry_price = entry_price
        self._entries_in_direction = 1
        self._black_sheep_triggered = False

        stop_distance = self._to_price(self._stop_loss_pips.Value) if self._stop_loss_pips.Value > 0 else None
        take_distance = self._to_price(self._take_profit_pips.Value) if self._take_profit_pips.Value > 0 else None

        self._short_stop = entry_price + stop_distance if stop_distance is not None else None
        self._short_take = entry_price - take_distance if take_distance is not None else None
        self._long_stop = None
        self._long_take = None

    def _manage_exits(self, candle):
        if not self._has_position:
            return

        if self.Position > 0:
            if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket(self.Position)
                self._reset_position_state()
                return
            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket(self.Position)
                self._reset_position_state()
                return
        elif self.Position < 0:
            if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket(abs(self.Position))
                self._reset_position_state()
                return
            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket(abs(self.Position))
                self._reset_position_state()
                return

    def _update_trailing_stops(self, candle):
        if not self._has_position or self._trailing_stop_pips.Value <= 0 or self._pip_size <= 0:
            return

        distance = self._to_price(self._trailing_stop_pips.Value)
        step = self._to_price(self._trailing_step_pips.Value) if self._trailing_step_pips.Value > 0 else 0.0

        if self.Position > 0:
            threshold = float(candle.ClosePrice) - (distance + step)
            if float(candle.ClosePrice) - self._entry_price > distance + step:
                if self._long_stop is None or self._long_stop < threshold:
                    self._long_stop = float(candle.ClosePrice) - distance
        elif self.Position < 0:
            threshold = float(candle.ClosePrice) + (distance + step)
            if self._entry_price - float(candle.ClosePrice) > distance + step:
                if self._short_stop is None or self._short_stop > threshold:
                    self._short_stop = float(candle.ClosePrice) + distance

    def _handle_pattern_break(self):
        if self._pattern_direction == 0 or self._black_sheep_triggered:
            return

        closing = self._closing_behavior.Value

        if closing == 0:
            self._close_position()
        elif closing == 1:
            if self._pattern_direction > 0 and self.Position < 0:
                self._close_position()
            elif self._pattern_direction < 0 and self.Position > 0:
                self._close_position()
        elif closing == 2:
            if self._pattern_direction > 0 and self.Position > 0:
                self._close_position()
            elif self._pattern_direction < 0 and self.Position < 0:
                self._close_position()

        self._black_sheep_triggered = True
        self._entries_in_direction = 0
        self._pattern_direction = 0

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(abs(self.Position))
        self._reset_position_state()

    def _reset_position_state(self):
        self._has_position = self.Position != 0
        self._entries_in_direction = 1 if self._has_position else 0
        if not self._has_position:
            self._entry_price = 0.0
            self._long_stop = None
            self._long_take = None
            self._short_stop = None
            self._short_take = None

    def _calculate_open_profit(self, current_price):
        if not self._has_position or self.Position == 0:
            return 0.0
        volume = abs(self.Position)
        if self.Position > 0:
            return (current_price - self._entry_price) * volume
        else:
            return (self._entry_price - current_price) * volume

    def _get_candle_direction(self, candle):
        if float(candle.OpenPrice) < float(candle.ClosePrice):
            return 1
        if float(candle.OpenPrice) > float(candle.ClosePrice):
            return -1
        return 0

    def _is_within_trade_hours(self, time):
        hour = time.Hour
        return hour >= self._start_hour.Value and hour <= self._end_hour.Value

    def _to_price(self, pips):
        return pips * self._pip_size

    def _calculate_pip_size(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0:
            return 0.0001
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
        super(n_candles_sequence_streak_strategy, self).OnReseted()
        self._streak_count = 0
        self._last_direction = 0
        self._pattern_direction = 0
        self._entries_in_direction = 0
        self._black_sheep_triggered = False
        self._has_position = False
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def CreateClone(self):
        return n_candles_sequence_streak_strategy()
