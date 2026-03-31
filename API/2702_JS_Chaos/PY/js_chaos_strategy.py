import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SmoothedMovingAverage,
    SimpleMovingAverage,
    StandardDeviation,
    DecimalIndicatorValue,
)


class js_chaos_strategy(Strategy):
    """JS Chaos: Alligator + AO + AC + fractals with staged entries and trailing."""

    def __init__(self):
        super(js_chaos_strategy, self).__init__()

        self._use_time = self.Param("UseTime", False) \
            .SetDisplay("Use Time", "Enable trading window", "General")
        self._open_hour = self.Param("OpenHour", 7) \
            .SetDisplay("Open Hour", "Hour to start trading", "General")
        self._close_hour = self.Param("CloseHour", 18) \
            .SetDisplay("Close Hour", "Hour to stop trading", "General")
        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Base Volume", "Base volume for staged entries", "Trading")
        self._indent_pips = self.Param("IndentingPips", 0) \
            .SetDisplay("Indenting (pips)", "Offset from fractal level", "Trading")
        self._fibo1 = self.Param("Fibo1", 1.618) \
            .SetGreaterThanZero() \
            .SetDisplay("Fibo 1", "Primary take-profit multiplier", "Targets")
        self._fibo2 = self.Param("Fibo2", 4.618) \
            .SetGreaterThanZero() \
            .SetDisplay("Fibo 2", "Secondary take-profit multiplier", "Targets")
        self._use_close_positions = self.Param("UseClosePositions", True) \
            .SetDisplay("Close Positions", "Exit when lips cross previous open", "Risk")
        self._use_trailing = self.Param("UseTrailing", True) \
            .SetDisplay("Use Trailing", "Enable MA trailing stop", "Risk")
        self._use_breakeven = self.Param("UseBreakeven", True) \
            .SetDisplay("Use Breakeven", "Move secondary trade to breakeven", "Risk")
        self._breakeven_plus_pips = self.Param("BreakevenPlusPips", 1) \
            .SetDisplay("Breakeven Plus", "Additional pips for breakeven", "Risk")
        self._fractal_lookback = self.Param("FractalLookback", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fractal Lookback", "Bars required to confirm fractal levels", "Indicator")
        self._jaw_shift = self.Param("JawShift", 8) \
            .SetDisplay("Jaw Shift", "Shift applied to the jaw moving average", "Indicator")
        self._teeth_shift = self.Param("TeethShift", 5) \
            .SetDisplay("Teeth Shift", "Shift applied to the teeth moving average", "Indicator")
        self._lips_shift = self.Param("LipsShift", 3) \
            .SetDisplay("Lips Shift", "Shift applied to the lips moving average", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")

        self._jaw_queue = []
        self._teeth_queue = []
        self._lips_queue = []
        self._jaw_value = None
        self._teeth_value = None
        self._lips_value = None
        self._ma21_value = None
        self._ao_current = None
        self._ao_prev = None
        self._ac_current = None
        self._ac_prev = None
        self._std_dev_current = None
        self._std_dev_prev = None
        self._highs = [None] * 5
        self._lows = [None] * 5
        self._buffer_count = 0
        self._up_fractals = []
        self._down_fractals = []
        self._pending_orders = []
        self._active_trades = []
        self._pip_size = 0.0
        self._indent_value = 0.0
        self._breakeven_plus_value = 0.0
        self._price_settings_ready = False
        self._prev_open = None

    @property
    def UseTime(self):
        return self._use_time.Value
    @property
    def OpenHour(self):
        return int(self._open_hour.Value)
    @property
    def CloseHour(self):
        return int(self._close_hour.Value)
    @property
    def BaseVolume(self):
        return float(self._base_volume.Value)
    @property
    def IndentingPips(self):
        return int(self._indent_pips.Value)
    @property
    def Fibo1(self):
        return float(self._fibo1.Value)
    @property
    def Fibo2(self):
        return float(self._fibo2.Value)
    @property
    def UseClosePositions(self):
        return self._use_close_positions.Value
    @property
    def UseTrailing(self):
        return self._use_trailing.Value
    @property
    def UseBreakeven(self):
        return self._use_breakeven.Value
    @property
    def BreakevenPlusPips(self):
        return int(self._breakeven_plus_pips.Value)
    @property
    def FractalLookback(self):
        return int(self._fractal_lookback.Value)
    @property
    def JawShift(self):
        return int(self._jaw_shift.Value)
    @property
    def TeethShift(self):
        return int(self._teeth_shift.Value)
    @property
    def LipsShift(self):
        return int(self._lips_shift.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _update_price_settings(self):
        sec = self.Security
        if sec is None:
            return
        ps = sec.PriceStep
        if ps is None:
            return
        step = float(ps)
        if step <= 0:
            return
        decimals = self._count_decimals(step)
        pip = step * 10.0 if (decimals == 3 or decimals == 5) else step
        self._pip_size = pip
        self._indent_value = pip * self.IndentingPips
        self._breakeven_plus_value = pip * self.BreakevenPlusPips
        self._price_settings_ready = True

    def _count_decimals(self, value):
        v = abs(value)
        count = 0
        while v != int(v) and count < 10:
            v *= 10
            count += 1
        return count

    def _normalize_price(self, price):
        sec = self.Security
        if sec is not None and sec.PriceStep is not None:
            step = float(sec.PriceStep)
            if step > 0:
                return round(price / step) * step
        return price

    def OnStarted2(self, time):
        super(js_chaos_strategy, self).OnStarted2(time)

        self._update_price_settings()

        self._jaw_ind = SmoothedMovingAverage()
        self._jaw_ind.Length = 13
        self._teeth_ind = SmoothedMovingAverage()
        self._teeth_ind.Length = 8
        self._lips_ind = SmoothedMovingAverage()
        self._lips_ind.Length = 5
        self._ma21_ind = SmoothedMovingAverage()
        self._ma21_ind.Length = 21
        self._ao_short = SimpleMovingAverage()
        self._ao_short.Length = 5
        self._ao_long = SimpleMovingAverage()
        self._ao_long.Length = 34
        self._ao_sma = SimpleMovingAverage()
        self._ao_sma.Length = 5
        self._std_dev = StandardDeviation()
        self._std_dev.Length = 10

        self._jaw_queue = []
        self._teeth_queue = []
        self._lips_queue = []
        self._jaw_value = None
        self._teeth_value = None
        self._lips_value = None
        self._ma21_value = None
        self._ao_current = None
        self._ao_prev = None
        self._ac_current = None
        self._ac_prev = None
        self._std_dev_current = None
        self._std_dev_prev = None
        self._highs = [None] * 5
        self._lows = [None] * 5
        self._buffer_count = 0
        self._up_fractals = []
        self._down_fractals = []
        self._pending_orders = []
        self._active_trades = []
        self._prev_open = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self._price_settings_ready:
            self._update_price_settings()

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        t = candle.ServerTime

        # Alligator
        self._update_alligator(median, t)

        # MA21
        ma_iv = DecimalIndicatorValue(self._ma21_ind, Decimal(float(candle.ClosePrice)), t)
        ma_iv.IsFinal = True
        ma_val = self._ma21_ind.Process(ma_iv)
        if ma_val.IsFormed:
            self._ma21_value = float(ma_val.Value)

        # AO
        ao_s_iv = DecimalIndicatorValue(self._ao_short, Decimal(median), t)
        ao_s_iv.IsFinal = True
        ao_short_val = self._ao_short.Process(ao_s_iv)

        ao_l_iv = DecimalIndicatorValue(self._ao_long, Decimal(median), t)
        ao_l_iv.IsFinal = True
        ao_long_val = self._ao_long.Process(ao_l_iv)

        if not self._ao_short.IsFormed or not self._ao_long.IsFormed:
            return

        ao = float(ao_short_val.Value) - float(ao_long_val.Value)
        ao_sma_iv = DecimalIndicatorValue(self._ao_sma, Decimal(ao), t)
        ao_sma_iv.IsFinal = True
        ao_sma_val = self._ao_sma.Process(ao_sma_iv)
        if not self._ao_sma.IsFormed:
            return

        ao_sma = float(ao_sma_val.Value)
        ac = ao - ao_sma

        # StdDev
        std_iv = DecimalIndicatorValue(self._std_dev, Decimal(float(candle.ClosePrice)), t)
        std_iv.IsFinal = True
        std_val = self._std_dev.Process(std_iv)
        if not self._std_dev.IsFormed:
            return

        std_dev = float(std_val.Value)

        if self._jaw_value is None or self._teeth_value is None or self._lips_value is None or self._ma21_value is None:
            return

        # Update history
        self._ao_prev = self._ao_current
        self._ao_current = ao
        self._ac_prev = self._ac_current
        self._ac_current = ac
        self._std_dev_prev = self._std_dev_current
        self._std_dev_current = std_dev

        self._update_fractals(candle)

        close_price = float(candle.ClosePrice)

        if self.UseTrailing:
            self._update_trailing(close_price)

        self._update_breakeven(close_price)
        self._handle_stops_and_targets(candle)
        self._update_breakeven(close_price)

        if self.UseClosePositions:
            self._apply_lips_exit()
            self._update_breakeven(close_price)

        signal = self._get_signal()
        can_trade = self._is_trading_time(candle.OpenTime)

        if can_trade:
            self._try_place_orders(signal, close_price)

        self._trigger_pending_orders(candle)

        if signal == 2:
            self._pending_orders = [o for o in self._pending_orders if o["side"] != "buy"]
        elif signal == 1:
            self._pending_orders = [o for o in self._pending_orders if o["side"] != "sell"]

        self._prev_open = float(candle.OpenPrice)

    def _update_alligator(self, median, t):
        jaw_iv = DecimalIndicatorValue(self._jaw_ind, Decimal(median), t)
        jaw_iv.IsFinal = True
        jaw_val = self._jaw_ind.Process(jaw_iv)
        if jaw_val.IsFormed:
            self._jaw_queue.append(float(jaw_val.Value))
            if len(self._jaw_queue) > self.JawShift:
                self._jaw_value = self._jaw_queue.pop(0)

        teeth_iv = DecimalIndicatorValue(self._teeth_ind, Decimal(median), t)
        teeth_iv.IsFinal = True
        teeth_val = self._teeth_ind.Process(teeth_iv)
        if teeth_val.IsFormed:
            self._teeth_queue.append(float(teeth_val.Value))
            if len(self._teeth_queue) > self.TeethShift:
                self._teeth_value = self._teeth_queue.pop(0)

        lips_iv = DecimalIndicatorValue(self._lips_ind, Decimal(median), t)
        lips_iv.IsFinal = True
        lips_val = self._lips_ind.Process(lips_iv)
        if lips_val.IsFormed:
            self._lips_queue.append(float(lips_val.Value))
            if len(self._lips_queue) > self.LipsShift:
                self._lips_value = self._lips_queue.pop(0)

    def _update_fractals(self, candle):
        for i in range(4):
            self._highs[i] = self._highs[i + 1]
            self._lows[i] = self._lows[i + 1]
        self._highs[4] = float(candle.HighPrice)
        self._lows[4] = float(candle.LowPrice)

        if self._buffer_count < 5:
            self._buffer_count += 1

        # Increment ages
        for f in self._up_fractals:
            f["age"] += 1
        for f in self._down_fractals:
            f["age"] += 1

        if self._buffer_count < 5:
            self._trim_fractals(self._up_fractals)
            self._trim_fractals(self._down_fractals)
            return

        up_fractal = None
        down_fractal = None

        h = self._highs
        if all(x is not None for x in h):
            if h[2] > h[0] and h[2] > h[1] and h[2] > h[3] and h[2] > h[4]:
                up_fractal = h[2]

        lo = self._lows
        if all(x is not None for x in lo):
            if lo[2] < lo[0] and lo[2] < lo[1] and lo[2] < lo[3] and lo[2] < lo[4]:
                down_fractal = lo[2]

        if up_fractal is not None:
            price = self._normalize_price(up_fractal + self._indent_value)
            self._up_fractals.insert(0, {"price": price, "age": 0})

        if down_fractal is not None:
            price = self._normalize_price(down_fractal - self._indent_value)
            self._down_fractals.insert(0, {"price": price, "age": 0})

        self._trim_fractals(self._up_fractals)
        self._trim_fractals(self._down_fractals)

    def _trim_fractals(self, levels):
        i = len(levels) - 1
        while i >= 0:
            if levels[i]["age"] >= self.FractalLookback:
                levels.pop(i)
            i -= 1

    def _get_signal(self):
        if self._ao_current is None or self._ao_prev is None:
            return 0
        if self._lips_value is None or self._teeth_value is None or self._jaw_value is None:
            return 0

        ao0 = self._ao_current
        ao1 = self._ao_prev
        lips = self._lips_value
        teeth = self._teeth_value
        jaw = self._jaw_value

        if ao0 > ao1 and ao1 > 0 and lips > teeth and teeth > jaw:
            return 1
        if ao0 < ao1 and ao1 < 0 and lips < teeth and teeth < jaw:
            return 2
        return 0

    def _try_place_orders(self, signal, close_price):
        if signal == 1:
            up_frac = self._get_latest_fractal(self._up_fractals)
            if up_frac is not None:
                self._try_create_buy_orders(up_frac, self._lips_value, close_price)
        elif signal == 2:
            down_frac = self._get_latest_fractal(self._down_fractals)
            if down_frac is not None:
                self._try_create_sell_orders(down_frac, self._lips_value, close_price)

    def _get_latest_fractal(self, levels):
        return levels[0]["price"] if len(levels) > 0 else None

    def _try_create_buy_orders(self, up_fractal, lips, close_price):
        if up_fractal <= lips:
            return
        if any(t["side"] == "buy" for t in self._active_trades):
            return

        has_primary = any(o["side"] == "buy" and o["is_primary"] for o in self._pending_orders)
        has_secondary = any(o["side"] == "buy" and not o["is_primary"] for o in self._pending_orders)

        if not has_primary:
            distance = up_fractal - lips
            if self._pip_size > 0:
                if distance <= self._pip_size:
                    return
                if close_price + self._pip_size >= up_fractal:
                    return
            tp = lips + distance * self.Fibo1
            if tp <= 0:
                return
            if self._pip_size > 0 and tp - up_fractal <= self._pip_size:
                return
            vol = self.BaseVolume * 2.0
            if vol > 0:
                self._pending_orders.append({
                    "side": "buy", "price": self._normalize_price(up_fractal),
                    "stop_loss": self._normalize_price(lips),
                    "take_profit": self._normalize_price(tp),
                    "volume": vol, "is_primary": True
                })

        has_primary = any(o["side"] == "buy" and o["is_primary"] for o in self._pending_orders)
        if not has_primary or has_secondary:
            return

        distance = up_fractal - lips
        if self._pip_size > 0:
            if distance <= self._pip_size:
                return
            if close_price + self._pip_size >= up_fractal:
                return
        tp2 = lips + distance * self.Fibo2
        if tp2 <= 0:
            return
        if self._pip_size > 0 and tp2 - up_fractal <= self._pip_size:
            return
        vol2 = self.BaseVolume
        if vol2 > 0:
            self._pending_orders.append({
                "side": "buy", "price": self._normalize_price(up_fractal),
                "stop_loss": self._normalize_price(lips),
                "take_profit": self._normalize_price(tp2),
                "volume": vol2, "is_primary": False
            })

    def _try_create_sell_orders(self, down_fractal, lips, close_price):
        if down_fractal >= lips:
            return
        if any(t["side"] == "sell" for t in self._active_trades):
            return

        has_primary = any(o["side"] == "sell" and o["is_primary"] for o in self._pending_orders)
        has_secondary = any(o["side"] == "sell" and not o["is_primary"] for o in self._pending_orders)

        if not has_primary:
            distance = lips - down_fractal
            if self._pip_size > 0:
                if distance <= self._pip_size:
                    return
                if close_price - self._pip_size <= down_fractal:
                    return
            tp = lips - distance * self.Fibo1
            if tp <= 0:
                return
            if self._pip_size > 0 and down_fractal - tp <= self._pip_size:
                return
            vol = self.BaseVolume * 2.0
            if vol > 0:
                self._pending_orders.append({
                    "side": "sell", "price": self._normalize_price(down_fractal),
                    "stop_loss": self._normalize_price(lips),
                    "take_profit": self._normalize_price(tp),
                    "volume": vol, "is_primary": True
                })

        has_primary = any(o["side"] == "sell" and o["is_primary"] for o in self._pending_orders)
        if not has_primary or has_secondary:
            return

        distance = lips - down_fractal
        if self._pip_size > 0:
            if distance <= self._pip_size:
                return
            if close_price - self._pip_size <= down_fractal:
                return
        tp2 = lips - distance * self.Fibo2
        if tp2 <= 0:
            return
        if self._pip_size > 0 and down_fractal - tp2 <= self._pip_size:
            return
        vol2 = self.BaseVolume
        if vol2 > 0:
            self._pending_orders.append({
                "side": "sell", "price": self._normalize_price(down_fractal),
                "stop_loss": self._normalize_price(lips),
                "take_profit": self._normalize_price(tp2),
                "volume": vol2, "is_primary": False
            })

    def _trigger_pending_orders(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        i = len(self._pending_orders) - 1
        while i >= 0:
            pending = self._pending_orders[i]
            triggered = False
            if pending["side"] == "buy":
                triggered = h >= pending["price"]
            else:
                triggered = lo <= pending["price"]
            if triggered:
                self._execute_trade(pending)
                self._pending_orders.pop(i)
            i -= 1

    def _execute_trade(self, order):
        if order["volume"] <= 0:
            return
        if order["side"] == "buy":
            self.BuyMarket()
        else:
            self.SellMarket()
        self._active_trades.append({
            "side": order["side"],
            "volume": order["volume"],
            "entry_price": order["price"],
            "stop_loss": order["stop_loss"],
            "take_profit": order["take_profit"],
            "is_primary": order["is_primary"],
            "moved_to_breakeven": False
        })

    def _update_trailing(self, close_price):
        if self._ma21_value is None:
            return
        if self._std_dev_current is None or self._std_dev_prev is None:
            return
        if self._ao_current is None or self._ao_prev is None:
            return
        if self._ac_current is None or self._ac_prev is None:
            return

        ma21 = self._ma21_value
        std0 = self._std_dev_current
        std1 = self._std_dev_prev
        ao0 = self._ao_current
        ao1 = self._ao_prev
        ac0 = self._ac_current
        ac1 = self._ac_prev

        for trade in self._active_trades:
            if trade["side"] == "buy":
                sl = trade["stop_loss"]
                if (sl <= 0 or (sl != ma21 and sl < ma21)) and std0 > std1 and ao0 > ao1 and ac0 > ac1:
                    if self._pip_size <= 0 or ma21 + self._pip_size <= close_price:
                        trade["stop_loss"] = self._normalize_price(ma21)
            else:
                sl = trade["stop_loss"]
                if (sl <= 0 or (sl != ma21 and sl > ma21)) and std0 > std1 and ao0 < ao1 and ac0 < ac1:
                    if self._pip_size <= 0 or ma21 - self._pip_size >= close_price:
                        trade["stop_loss"] = self._normalize_price(ma21)

    def _update_breakeven(self, close_price):
        if not self.UseBreakeven or self._breakeven_plus_value <= 0:
            return
        for trade in self._active_trades:
            if trade["is_primary"] or trade["moved_to_breakeven"]:
                continue
            primary_exists = any(t["side"] == trade["side"] and t["is_primary"] for t in self._active_trades)
            if primary_exists:
                continue
            if trade["side"] == "buy":
                if close_price >= trade["entry_price"] + self._breakeven_plus_value and trade["stop_loss"] < trade["entry_price"]:
                    trade["stop_loss"] = self._normalize_price(trade["entry_price"] + self._breakeven_plus_value)
                    trade["moved_to_breakeven"] = True
            else:
                if close_price <= trade["entry_price"] - self._breakeven_plus_value and trade["stop_loss"] > trade["entry_price"]:
                    trade["stop_loss"] = self._normalize_price(trade["entry_price"] - self._breakeven_plus_value)
                    trade["moved_to_breakeven"] = True

    def _handle_stops_and_targets(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        i = len(self._active_trades) - 1
        while i >= 0:
            trade = self._active_trades[i]
            close = False
            if trade["side"] == "buy":
                if trade["take_profit"] > 0 and h >= trade["take_profit"]:
                    close = True
                elif trade["stop_loss"] > 0 and lo <= trade["stop_loss"]:
                    close = True
            else:
                if trade["take_profit"] > 0 and lo <= trade["take_profit"]:
                    close = True
                elif trade["stop_loss"] > 0 and h >= trade["stop_loss"]:
                    close = True
            if close:
                self._close_trade(trade)
                self._active_trades.pop(i)
            i -= 1

    def _apply_lips_exit(self):
        if self._prev_open is None or self._lips_value is None:
            return
        prev_open = self._prev_open
        lips = self._lips_value
        if lips > prev_open:
            self._close_trades_by_side("buy")
        if lips < prev_open:
            self._close_trades_by_side("sell")

    def _close_trades_by_side(self, side):
        i = len(self._active_trades) - 1
        while i >= 0:
            trade = self._active_trades[i]
            if trade["side"] == side:
                self._close_trade(trade)
                self._active_trades.pop(i)
            i -= 1

    def _close_trade(self, trade):
        if trade["side"] == "buy":
            self.SellMarket()
        else:
            self.BuyMarket()

    def _is_trading_time(self, time):
        if not self.UseTime:
            return True
        hour = time.Hour
        trading = False
        if self.OpenHour > self.CloseHour:
            trading = hour <= self.CloseHour or hour >= self.OpenHour
        elif self.OpenHour < self.CloseHour:
            trading = hour >= self.OpenHour and hour <= self.CloseHour
        else:
            trading = hour == self.OpenHour

        day_of_week = int(time.DayOfWeek)
        if day_of_week == 1 and hour < 3:
            trading = False
        if day_of_week >= 5 and hour > 18:
            trading = False
        month = time.Month
        day = time.Day
        if month == 1 and day < 10:
            trading = False
        if month == 12 and day > 20:
            trading = False
        return trading

    def OnReseted(self):
        super(js_chaos_strategy, self).OnReseted()
        self._jaw_queue = []
        self._teeth_queue = []
        self._lips_queue = []
        self._jaw_value = None
        self._teeth_value = None
        self._lips_value = None
        self._ma21_value = None
        self._ao_current = None
        self._ao_prev = None
        self._ac_current = None
        self._ac_prev = None
        self._std_dev_current = None
        self._std_dev_prev = None
        self._highs = [None] * 5
        self._lows = [None] * 5
        self._buffer_count = 0
        self._up_fractals = []
        self._down_fractals = []
        self._pending_orders = []
        self._active_trades = []
        self._pip_size = 0.0
        self._indent_value = 0.0
        self._breakeven_plus_value = 0.0
        self._price_settings_ready = False
        self._prev_open = None

    def CreateClone(self):
        return js_chaos_strategy()
