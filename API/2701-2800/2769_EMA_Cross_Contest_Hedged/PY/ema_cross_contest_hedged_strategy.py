import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class ema_cross_contest_hedged_strategy(Strategy):
    """
    EMA crossover strategy with hedged stop orders and trailing management.
    Converted from the MQL EMA Cross Contest Hedged.
    """

    def __init__(self):
        super(ema_cross_contest_hedged_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 140) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 120) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 30) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 1) \
            .SetDisplay("Trailing Step (pips)", "Minimum profit before trailing adjusts", "Risk")
        self._hedge_level_pips = self.Param("HedgeLevelPips", 6) \
            .SetDisplay("Hedge Level (pips)", "Distance between hedging stop orders", "Orders")
        self._close_opposite = self.Param("CloseOppositePositions", False) \
            .SetDisplay("Close Opposite", "Close positions on opposite crossover", "Risk")
        self._use_macd_filter = self.Param("UseMacdFilter", False) \
            .SetDisplay("Use MACD", "Require MACD confirmation", "Filters")
        self._pending_order_count = self.Param("PendingOrderCount", 1) \
            .SetDisplay("Pending Orders", "Pending stop orders per side", "Orders")
        self._pending_expiration_sec = self.Param("PendingExpirationSeconds", 65535) \
            .SetDisplay("Pending Expiration (s)", "Lifetime of hedging stop orders", "Orders")
        self._short_ma_period = self.Param("ShortMaPeriod", 4) \
            .SetDisplay("Short EMA Period", "Fast EMA length", "Indicators")
        self._long_ma_period = self.Param("LongMaPeriod", 24) \
            .SetDisplay("Long EMA Period", "Slow EMA length", "Indicators")
        self._use_previous_bar = self.Param("UsePreviousBar", True) \
            .SetDisplay("Use Previous Bar", "Use previous bar for signals", "General")

        self._ema_short_last = None
        self._ema_short_prev_last = None
        self._ema_long_last = None
        self._ema_long_prev_last = None
        self._macd_last = None

        self._current_volume = 0.0
        self._entry_price = 0.0
        self._long_stop = None
        self._long_tp = None
        self._short_stop = None
        self._short_tp = None
        self._long_trailing = None
        self._short_trailing = None
        self._pending_orders = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_cross_contest_hedged_strategy, self).OnReseted()
        self._ema_short_last = None
        self._ema_short_prev_last = None
        self._ema_long_last = None
        self._ema_long_prev_last = None
        self._macd_last = None
        self._current_volume = 0.0
        self._entry_price = 0.0
        self._long_stop = None
        self._long_tp = None
        self._short_stop = None
        self._short_tp = None
        self._long_trailing = None
        self._short_trailing = None
        self._pending_orders = []

    def OnStarted2(self, time):
        super(ema_cross_contest_hedged_strategy, self).OnStarted2(time)

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_ma_period.Value
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_ma_period.Value
        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_ema, long_ema, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_value, long_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if short_value.IsEmpty or long_value.IsEmpty:
            return

        ema_short = float(short_value.Value)
        ema_long = float(long_value.Value)

        macd_current = None
        if not macd_value.IsEmpty:
            macd_line = macd_value.Macd
            if macd_line is not None:
                macd_current = float(macd_line)

        self._process_pending_orders(candle)

        cross = self._detect_cross(ema_short, ema_long)

        macd_filter_val = None
        if self._use_macd_filter.Value:
            if self._use_previous_bar.Value:
                macd_filter_val = self._macd_last
            else:
                macd_filter_val = macd_current
            if macd_filter_val is None:
                self._update_history(ema_short, ema_long, macd_current)
                return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._update_history(ema_short, ema_long, macd_current)
            return

        if self._current_volume > 0:
            if self._close_opposite.Value and cross == 2:
                self._exit_long()
                self._update_history(ema_short, ema_long, macd_current)
                return
            if self._check_long_stops(candle):
                self._update_history(ema_short, ema_long, macd_current)
                return
        elif self._current_volume < 0:
            if self._close_opposite.Value and cross == 1:
                self._exit_short()
                self._update_history(ema_short, ema_long, macd_current)
                return
            if self._check_short_stops(candle):
                self._update_history(ema_short, ema_long, macd_current)
                return

        if self._current_volume == 0:
            if cross == 1 and (not self._use_macd_filter.Value or macd_filter_val >= 0):
                self._enter_long(float(candle.ClosePrice), candle.CloseTime)
                self._update_history(ema_short, ema_long, macd_current)
                return
            if cross == 2 and (not self._use_macd_filter.Value or macd_filter_val <= 0):
                self._enter_short(float(candle.ClosePrice), candle.CloseTime)
                self._update_history(ema_short, ema_long, macd_current)
                return

        self._update_history(ema_short, ema_long, macd_current)

    def _process_pending_orders(self, candle):
        if not self._pending_orders:
            return
        now = candle.CloseTime
        to_remove = []
        for order in self._pending_orders:
            if order["expire_time"] <= now:
                to_remove.append(order)
                continue
            triggered = False
            if order["side"] == "buy":
                triggered = float(candle.HighPrice) >= order["price"]
            else:
                triggered = float(candle.LowPrice) <= order["price"]
            if triggered:
                to_remove.append(order)
                if order["side"] == "buy":
                    self.BuyMarket()
                    self._register_long_entry(order["price"], 1.0, order.get("stop"), order.get("take"))
                else:
                    self.SellMarket()
                    self._register_short_entry(order["price"], 1.0, order.get("stop"), order.get("take"))
        for o in to_remove:
            if o in self._pending_orders:
                self._pending_orders.remove(o)

    def _enter_long(self, price, time):
        self.BuyMarket()
        sl = price - self._pip_to_price(self._stop_loss_pips.Value) if self._stop_loss_pips.Value > 0 else None
        tp = price + self._pip_to_price(self._take_profit_pips.Value) if self._take_profit_pips.Value > 0 else None
        self._register_long_entry(price, 1.0, sl, tp)
        self._short_stop = None
        self._short_tp = None
        self._short_trailing = None
        self._create_pending_orders(time, price, "buy")

    def _enter_short(self, price, time):
        self.SellMarket()
        sl = price + self._pip_to_price(self._stop_loss_pips.Value) if self._stop_loss_pips.Value > 0 else None
        tp = price - self._pip_to_price(self._take_profit_pips.Value) if self._take_profit_pips.Value > 0 else None
        self._register_short_entry(price, 1.0, sl, tp)
        self._long_stop = None
        self._long_tp = None
        self._long_trailing = None
        self._create_pending_orders(time, price, "sell")

    def _register_long_entry(self, price, volume, stop, take):
        prev_vol = self._current_volume
        self._current_volume += volume
        if prev_vol <= 0:
            self._entry_price = price
        else:
            self._entry_price = (prev_vol * self._entry_price + volume * price) / self._current_volume
        if stop is not None:
            self._long_stop = max(self._long_stop, stop) if self._long_stop is not None else stop
        if take is not None:
            self._long_tp = max(self._long_tp, take) if self._long_tp is not None else take
        self._long_trailing = None

    def _register_short_entry(self, price, volume, stop, take):
        prev_vol = self._current_volume
        self._current_volume -= volume
        if prev_vol >= 0:
            self._entry_price = price
        else:
            self._entry_price = (abs(prev_vol) * self._entry_price + volume * price) / abs(self._current_volume)
        if stop is not None:
            self._short_stop = min(self._short_stop, stop) if self._short_stop is not None else stop
        if take is not None:
            self._short_tp = min(self._short_tp, take) if self._short_tp is not None else take
        self._short_trailing = None

    def _check_long_stops(self, candle):
        trail_dist = self._pip_to_price(self._trailing_stop_pips.Value)
        trail_step = self._pip_to_price(self._trailing_step_pips.Value)

        if self._trailing_stop_pips.Value > 0 and self._current_volume > 0:
            profit = float(candle.ClosePrice) - self._entry_price
            if profit > trail_dist + trail_step:
                new_stop = float(candle.ClosePrice) - trail_dist
                min_adv = float(candle.ClosePrice) - (trail_dist + trail_step)
                if self._long_trailing is None or self._long_trailing < min_adv:
                    self._long_trailing = new_stop

        eff_stop = self._long_stop
        if self._long_trailing is not None:
            eff_stop = max(eff_stop, self._long_trailing) if eff_stop is not None else self._long_trailing

        if eff_stop is not None and float(candle.LowPrice) <= eff_stop:
            self._exit_long()
            return True
        if self._long_tp is not None and float(candle.HighPrice) >= self._long_tp:
            self._exit_long()
            return True
        return False

    def _check_short_stops(self, candle):
        trail_dist = self._pip_to_price(self._trailing_stop_pips.Value)
        trail_step = self._pip_to_price(self._trailing_step_pips.Value)

        if self._trailing_stop_pips.Value > 0 and self._current_volume < 0:
            profit = self._entry_price - float(candle.ClosePrice)
            if profit > trail_dist + trail_step:
                new_stop = float(candle.ClosePrice) + trail_dist
                max_adv = float(candle.ClosePrice) + trail_dist + trail_step
                if self._short_trailing is None or self._short_trailing > max_adv:
                    self._short_trailing = new_stop

        eff_stop = self._short_stop
        if self._short_trailing is not None:
            eff_stop = min(eff_stop, self._short_trailing) if eff_stop is not None else self._short_trailing

        if eff_stop is not None and float(candle.HighPrice) >= eff_stop:
            self._exit_short()
            return True
        if self._short_tp is not None and float(candle.LowPrice) <= self._short_tp:
            self._exit_short()
            return True
        return False

    def _exit_long(self):
        if self._current_volume <= 0:
            return
        self.SellMarket()
        self._current_volume = 0.0
        self._entry_price = 0.0
        self._long_stop = None
        self._long_tp = None
        self._long_trailing = None

    def _exit_short(self):
        if self._current_volume >= 0:
            return
        self.BuyMarket()
        self._current_volume = 0.0
        self._entry_price = 0.0
        self._short_stop = None
        self._short_tp = None
        self._short_trailing = None

    def _detect_cross(self, ema_short, ema_long):
        if self._use_previous_bar.Value:
            if (self._ema_short_last is None or self._ema_long_last is None or
                    self._ema_short_prev_last is None or self._ema_long_prev_last is None):
                return 0
            prev_s = self._ema_short_prev_last
            prev_l = self._ema_long_prev_last
            cur_s = self._ema_short_last
            cur_l = self._ema_long_last
        else:
            if self._ema_short_last is None or self._ema_long_last is None:
                return 0
            prev_s = self._ema_short_last
            prev_l = self._ema_long_last
            cur_s = ema_short
            cur_l = ema_long

        if prev_s < prev_l and cur_s > cur_l:
            return 1
        if prev_s > prev_l and cur_s < cur_l:
            return 2
        return 0

    def _update_history(self, ema_short, ema_long, macd_current):
        self._ema_short_prev_last = self._ema_short_last
        self._ema_long_prev_last = self._ema_long_last
        self._ema_short_last = ema_short
        self._ema_long_last = ema_long
        if macd_current is not None:
            self._macd_last = macd_current

    def _pip_to_price(self, pips):
        if pips <= 0:
            return 0.0
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0
        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)
        mult = 10.0 if decimals == 3 or decimals == 5 else 1.0
        return pips * step * mult

    def _create_pending_orders(self, time, price, side):
        self._pending_orders = []
        hedge_pips = self._hedge_level_pips.Value
        if hedge_pips <= 0:
            return
        distance = self._pip_to_price(hedge_pips)
        if distance <= 0:
            return

        exp_sec = self._pending_expiration_sec.Value
        expiration = time + TimeSpan.FromSeconds(exp_sec) if exp_sec > 0 else None

        stop_offset = self._pip_to_price(self._stop_loss_pips.Value) if self._stop_loss_pips.Value > 0 else 0
        take_offset = self._pip_to_price(self._take_profit_pips.Value) if self._take_profit_pips.Value > 0 else 0

        count = self._pending_order_count.Value
        for i in range(1, count + 1):
            if side == "buy":
                level_price = price + distance * i
            else:
                level_price = price - distance * i

            stop = None
            take = None
            if self._stop_loss_pips.Value > 0:
                stop = level_price - stop_offset if side == "buy" else level_price + stop_offset
            if self._take_profit_pips.Value > 0:
                take = level_price + take_offset if side == "buy" else level_price - take_offset

            self._pending_orders.append({
                "side": side,
                "price": level_price,
                "stop": stop,
                "take": take,
                "expire_time": expiration if expiration is not None else time + TimeSpan.FromDays(365)
            })

    def CreateClone(self):
        return ema_cross_contest_hedged_strategy()
