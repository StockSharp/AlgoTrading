import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class double_ma_crossover_strategy(Strategy):
    """
    Double moving average crossover with breakout confirmation and trailing protection.
    Uses SMA crossover to generate pending entries that fire on breakout.
    Manages stop-loss, take-profit, and multi-level trailing stop.
    """

    def __init__(self):
        super(double_ma_crossover_strategy, self).__init__()
        self._fast_ma_period = self.Param("FastMaPeriod", 5) \
            .SetDisplay("Fast MA Period", "Period for the fast moving average", "General")
        self._slow_ma_period = self.Param("SlowMaPeriod", 15) \
            .SetDisplay("Slow MA Period", "Period for the slow moving average", "General")
        self._breakout_pips = self.Param("BreakoutPips", 15) \
            .SetDisplay("Breakout Pips", "Distance in price steps added before entry", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 25) \
            .SetDisplay("Stop Loss Pips", "Protective stop in price steps", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 0) \
            .SetDisplay("Take Profit Pips", "Take profit distance in price steps", "Risk")
        self._use_trailing = self.Param("UseTrailingStop", False) \
            .SetDisplay("Use Trailing", "Enable trailing stop management", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 40) \
            .SetDisplay("Trailing Stop Pips", "Trailing distance in price steps", "Risk")
        self._level1_trigger = self.Param("Level1TriggerPips", 20) \
            .SetDisplay("Level 1 Trigger", "Profit in steps for first trailing adjustment", "Risk")
        self._level1_offset = self.Param("Level1OffsetPips", 20) \
            .SetDisplay("Level 1 Offset", "Offset in steps after first trigger", "Risk")
        self._level2_trigger = self.Param("Level2TriggerPips", 30) \
            .SetDisplay("Level 2 Trigger", "Profit in steps for second trailing adjustment", "Risk")
        self._level2_offset = self.Param("Level2OffsetPips", 20) \
            .SetDisplay("Level 2 Offset", "Offset in steps after second trigger", "Risk")
        self._level3_trigger = self.Param("Level3TriggerPips", 50) \
            .SetDisplay("Level 3 Trigger", "Profit in steps for third trailing adjustment", "Risk")
        self._level3_offset = self.Param("Level3OffsetPips", 20) \
            .SetDisplay("Level 3 Offset", "Offset in steps after third trigger", "Risk")
        self._use_time_limit = self.Param("UseTimeLimit", False) \
            .SetDisplay("Use Time Limit", "Restrict entries to trading window", "Schedule")
        self._start_hour = self.Param("StartHour", 11) \
            .SetDisplay("Start Hour", "Hour when new setups become valid", "Schedule")
        self._stop_hour = self.Param("StopHour", 16) \
            .SetDisplay("Stop Hour", "Hour after which no new setups created", "Schedule")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for analysis", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = None
        self._current_stop = None
        self._current_tp = None
        self._max_price = 0.0
        self._min_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(double_ma_crossover_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(double_ma_crossover_strategy, self).OnStarted2(time)
        self._reset_state()

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self._fast_ma_period.Value
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self._slow_ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_ma, slow_ma):
        if candle.State != CandleStates.Finished:
            return

        fast_ma = float(fast_ma)
        slow_ma = float(slow_ma)

        if not self._has_prev:
            self._prev_fast = fast_ma
            self._prev_slow = slow_ma
            self._has_prev = True
            return

        cross_up = fast_ma > slow_ma and self._prev_fast <= self._prev_slow
        cross_down = fast_ma < slow_ma and self._prev_fast >= self._prev_slow

        self._manage_open_position(candle, cross_up, cross_down)
        self._trigger_pending_entries(candle)

        if self._use_time_limit.Value and not self._is_trading_time(candle.OpenTime):
            if cross_up:
                self._pending_sell_price = None
            if cross_down:
                self._pending_buy_price = None
            self._prev_fast = fast_ma
            self._prev_slow = slow_ma
            return

        if cross_down:
            self._pending_buy_price = None
        if cross_up:
            self._pending_sell_price = None

        if self.Position == 0:
            breakout = self._get_breakout_distance()
            if cross_up:
                self._pending_buy_price = float(candle.ClosePrice) + breakout
            elif cross_down:
                self._pending_sell_price = float(candle.ClosePrice) - breakout

        self._trigger_pending_entries(candle)
        self._prev_fast = fast_ma
        self._prev_slow = slow_ma

    def _manage_open_position(self, candle, cross_up, cross_down):
        if self.Position == 0:
            if self._entry_price is not None:
                self._reset_position_state()
            return

        if self._entry_price is None:
            self._entry_price = float(candle.ClosePrice)
            self._max_price = float(candle.ClosePrice)
            self._min_price = float(candle.ClosePrice)

        self._update_extremes(candle)
        self._update_trailing_stop(candle)

        if self._check_stops_and_targets(candle):
            return

        if self.Position > 0 and cross_down:
            self.SellMarket()
            self._reset_position_state()
            return
        if self.Position < 0 and cross_up:
            self.BuyMarket()
            self._reset_position_state()

    def _update_extremes(self, candle):
        self._max_price = max(self._max_price, float(candle.HighPrice))
        self._min_price = min(self._min_price, float(candle.LowPrice))

    def _update_trailing_stop(self, candle):
        if not self._use_trailing.Value or self._entry_price is None:
            return

        entry = self._entry_price
        close = float(candle.ClosePrice)
        step = self._get_price_step()

        # Level 3 trailing (type 3 logic from C#)
        trigger1 = step * abs(self._level1_trigger.Value)
        if trigger1 > 0:
            if self.Position > 0 and self._max_price - entry >= trigger1:
                candidate = entry + trigger1 - step * abs(self._level1_offset.Value)
                self._update_stop_long(candidate)
            elif self.Position < 0 and entry - self._min_price >= trigger1:
                candidate = entry - trigger1 + step * abs(self._level1_offset.Value)
                self._update_stop_short(candidate)

        trigger2 = step * abs(self._level2_trigger.Value)
        if trigger2 > 0:
            if self.Position > 0 and self._max_price - entry >= trigger2:
                candidate = entry + trigger2 - step * abs(self._level2_offset.Value)
                self._update_stop_long(candidate)
            elif self.Position < 0 and entry - self._min_price >= trigger2:
                candidate = entry - trigger2 + step * abs(self._level2_offset.Value)
                self._update_stop_short(candidate)

        trigger3 = step * abs(self._level3_trigger.Value)
        if trigger3 > 0:
            if self.Position > 0 and self._max_price - entry >= trigger3:
                candidate = close - step * abs(self._level3_offset.Value)
                self._update_stop_long(candidate)
            elif self.Position < 0 and entry - self._min_price >= trigger3:
                candidate = close + step * abs(self._level3_offset.Value)
                self._update_stop_short(candidate)

    def _update_stop_long(self, candidate):
        if self._current_stop is None or candidate > self._current_stop:
            self._current_stop = candidate

    def _update_stop_short(self, candidate):
        if self._current_stop is None or candidate < self._current_stop:
            self._current_stop = candidate

    def _check_stops_and_targets(self, candle):
        if self.Position > 0:
            if self._current_tp is not None and float(candle.HighPrice) >= self._current_tp:
                self.SellMarket()
                self._reset_position_state()
                return True
            if self._current_stop is not None and float(candle.LowPrice) <= self._current_stop:
                self.SellMarket()
                self._reset_position_state()
                return True
        elif self.Position < 0:
            if self._current_tp is not None and float(candle.LowPrice) <= self._current_tp:
                self.BuyMarket()
                self._reset_position_state()
                return True
            if self._current_stop is not None and float(candle.HighPrice) >= self._current_stop:
                self.BuyMarket()
                self._reset_position_state()
                return True
        return False

    def _trigger_pending_entries(self, candle):
        if self.Position != 0:
            if self.Position > 0:
                self._pending_buy_price = None
            else:
                self._pending_sell_price = None
            return

        step = self._get_price_step()
        if self._pending_buy_price is not None and float(candle.HighPrice) >= self._pending_buy_price:
            price = self._pending_buy_price
            self.BuyMarket()
            self._entry_price = price
            sl = self._stop_loss_pips.Value
            tp = self._take_profit_pips.Value
            self._current_stop = price - step * abs(sl) if sl > 0 else None
            self._current_tp = price + step * abs(tp) if tp > 0 else None
            self._max_price = price
            self._min_price = price
            self._pending_buy_price = None
            self._pending_sell_price = None
        elif self._pending_sell_price is not None and float(candle.LowPrice) <= self._pending_sell_price:
            price = self._pending_sell_price
            self.SellMarket()
            self._entry_price = price
            sl = self._stop_loss_pips.Value
            tp = self._take_profit_pips.Value
            self._current_stop = price + step * abs(sl) if sl > 0 else None
            self._current_tp = price - step * abs(tp) if tp > 0 else None
            self._max_price = price
            self._min_price = price
            self._pending_buy_price = None
            self._pending_sell_price = None

    def _is_trading_time(self, time):
        if not self._use_time_limit.Value:
            return True
        hour = time.Hour
        start = self._start_hour.Value
        stop = self._stop_hour.Value
        if start <= stop:
            return hour >= start and hour <= stop
        return hour >= start or hour <= stop

    def _get_breakout_distance(self):
        return self._get_price_step() * abs(self._breakout_pips.Value)

    def _get_price_step(self):
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        return step if step > 0 else 1.0

    def _reset_position_state(self):
        self._entry_price = None
        self._current_stop = None
        self._current_tp = None
        self._max_price = 0.0
        self._min_price = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None

    def _reset_state(self):
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._reset_position_state()

    def CreateClone(self):
        return double_ma_crossover_strategy()
