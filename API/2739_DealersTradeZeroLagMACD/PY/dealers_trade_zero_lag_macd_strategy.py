import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import ZeroLagExponentialMovingAverage, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class dealers_trade_zero_lag_macd_strategy(Strategy):
    """
    Grid strategy based on zero lag MACD slope with adaptive spacing and money management.
    Uses two ZLEMA indicators to compute MACD, smoothed by signal EMA.
    Manages a grid of long/short entries with trailing stops, SL/TP, and account protection.
    """

    def __init__(self):
        super(dealers_trade_zero_lag_macd_strategy, self).__init__()
        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetDisplay("Base Volume", "Initial order volume", "Trading")
        self._risk_percent = self.Param("RiskPercent", 5.0) \
            .SetDisplay("Risk Percent", "Risk per trade when base volume is zero", "Trading")
        self._max_positions = self.Param("MaxPositions", 2) \
            .SetDisplay("Max Positions", "Maximum simultaneous entries", "Risk")
        self._interval_pips = self.Param("IntervalPips", 50) \
            .SetDisplay("Interval (pips)", "Base spacing between entries", "Grid")
        self._interval_coefficient = self.Param("IntervalCoefficient", 1.2) \
            .SetDisplay("Interval Coefficient", "Spacing multiplier for additional entries", "Grid")
        self._stop_loss_pips = self.Param("StopLossPips", 0) \
            .SetDisplay("Stop Loss (pips)", "Distance to protective stop", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit (pips)", "Base take profit distance", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step (pips)", "Extra move required to tighten trail", "Risk")
        self._tp_coefficient = self.Param("TakeProfitCoefficient", 1.2) \
            .SetDisplay("TP Coefficient", "Take profit multiplier per entry", "Risk")
        self._secure_profit = self.Param("SecureProfit", 300.0) \
            .SetDisplay("Secure Profit", "Cumulative profit to trigger protection", "Risk")
        self._account_protection = self.Param("AccountProtection", True) \
            .SetDisplay("Account Protection", "Enable profit locking", "Risk")
        self._positions_for_protection = self.Param("PositionsForProtection", 3) \
            .SetDisplay("Positions For Protection", "Entries required for protection", "Risk")
        self._reverse_condition = self.Param("ReverseCondition", False) \
            .SetDisplay("Reverse Condition", "Invert MACD slope logic", "General")
        self._fast_length = self.Param("FastLength", 14) \
            .SetDisplay("Fast Length", "Fast ZLEMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow Length", "Slow ZLEMA length", "Indicators")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal Length", "Signal smoothing length", "Indicators")
        self._max_volume = self.Param("MaxVolume", 5.0) \
            .SetDisplay("Max Volume", "Maximum volume per entry", "Trading")
        self._lot_multiplier = self.Param("LotMultiplier", 1.6) \
            .SetDisplay("Lot Multiplier", "Multiplier applied to each new entry", "Trading")
        self._minimum_balance = self.Param("MinimumBalance", 0.0) \
            .SetDisplay("Minimum Balance", "Stop trading below this balance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._long_entries = []
        self._short_entries = []
        self._pip_size = 0.0
        self._last_long_entry_price = 0.0
        self._last_short_entry_price = 0.0
        self._previous_macd = 0.0
        self._has_previous_macd = False
        self._signal_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dealers_trade_zero_lag_macd_strategy, self).OnReseted()
        self._long_entries = []
        self._short_entries = []
        self._last_long_entry_price = 0.0
        self._last_short_entry_price = 0.0
        self._previous_macd = 0.0
        self._has_previous_macd = False
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(dealers_trade_zero_lag_macd_strategy, self).OnStarted(time)

        fast_zlema = ZeroLagExponentialMovingAverage()
        fast_zlema.Length = self._fast_length.Value
        slow_zlema = ZeroLagExponentialMovingAverage()
        slow_zlema.Length = self._slow_length.Value
        self._signal_ema = ExponentialMovingAverage()
        self._signal_ema.Length = self._signal_length.Value

        decimals = 0
        step = 0.0001
        if self.Security is not None:
            if self.Security.Decimals is not None:
                decimals = int(self.Security.Decimals)
            if self.Security.PriceStep is not None:
                step = float(self.Security.PriceStep)
        if step <= 0:
            step = 0.0001
        factor = 10.0 if (decimals == 3 or decimals == 5) else 1.0
        self._pip_size = step * factor

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_zlema, slow_zlema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_zlema)
            self.DrawIndicator(area, slow_zlema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)
        macd = fast_val - slow_val

        sig_input = DecimalIndicatorValue(self._signal_ema, macd, candle.CloseTime)
        sig_input.IsFinal = True
        self._signal_ema.Process(sig_input)

        if not self._has_previous_macd:
            self._previous_macd = macd
            self._has_previous_macd = True
            return

        direction = 3
        if macd > self._previous_macd and macd != 0 and self._previous_macd != 0:
            direction = 2
        elif macd < self._previous_macd and macd != 0 and self._previous_macd != 0:
            direction = 1

        if self._reverse_condition.Value:
            if direction == 1:
                direction = 2
            elif direction == 2:
                direction = 1

        self._previous_macd = macd

        open_positions = len(self._long_entries) + len(self._short_entries)
        continue_opening = open_positions <= self._max_positions.Value

        if direction != 3 and open_positions > self._max_positions.Value:
            self._close_minimum_profit(float(candle.ClosePrice))
            return

        closed_this_bar = self._manage_positions(candle)
        if closed_this_bar:
            return

        total_profit = self._get_total_profit(float(candle.ClosePrice))
        if (self._account_protection.Value and open_positions > self._positions_for_protection.Value
                and total_profit >= self._secure_profit.Value):
            self._close_maximum_profit(float(candle.ClosePrice))
            return

        if not continue_opening:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if direction == 2:
            self._try_open_long(candle, open_positions)
        elif direction == 1:
            self._try_open_short(candle, open_positions)

    def _try_open_long(self, candle, open_positions):
        interval = self._get_interval_distance(open_positions)
        can_open = len(self._long_entries) == 0 or self._last_long_entry_price - float(candle.ClosePrice) >= interval
        if not can_open:
            return

        stop_dist = self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else 0.0
        take_dist = self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else 0.0
        if take_dist > 0:
            take_dist *= self._pow(self._tp_coefficient.Value, open_positions + 1)

        trailing_dist = self._trailing_stop_pips.Value * self._pip_size if self._trailing_stop_pips.Value > 0 else 0.0
        trailing_step = self._trailing_step_pips.Value * self._pip_size if self._trailing_step_pips.Value > 0 else 0.0

        price = float(candle.ClosePrice)
        entry = {
            "side": "buy",
            "entry_price": price,
            "volume": 1.0,
            "stop_loss": price - stop_dist if stop_dist > 0 else None,
            "take_profit": price + take_dist if take_dist > 0 else None,
            "trailing_distance": trailing_dist,
            "trailing_step": trailing_step,
            "trailing_stop": None,
        }
        self._long_entries.append(entry)
        self._last_long_entry_price = price
        self.BuyMarket()

    def _try_open_short(self, candle, open_positions):
        interval = self._get_interval_distance(open_positions)
        can_open = len(self._short_entries) == 0 or float(candle.ClosePrice) - self._last_short_entry_price >= interval
        if not can_open:
            return

        stop_dist = self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else 0.0
        take_dist = self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else 0.0
        if take_dist > 0:
            take_dist *= self._pow(self._tp_coefficient.Value, open_positions + 1)

        trailing_dist = self._trailing_stop_pips.Value * self._pip_size if self._trailing_stop_pips.Value > 0 else 0.0
        trailing_step = self._trailing_step_pips.Value * self._pip_size if self._trailing_step_pips.Value > 0 else 0.0

        price = float(candle.ClosePrice)
        entry = {
            "side": "sell",
            "entry_price": price,
            "volume": 1.0,
            "stop_loss": price + stop_dist if stop_dist > 0 else None,
            "take_profit": price - take_dist if take_dist > 0 else None,
            "trailing_distance": trailing_dist,
            "trailing_step": trailing_step,
            "trailing_stop": None,
        }
        self._short_entries.append(entry)
        self._last_short_entry_price = price
        self.SellMarket()

    def _manage_positions(self, candle):
        closed = False
        if self._manage_entries(self._long_entries, candle, True):
            closed = True
        if self._manage_entries(self._short_entries, candle, False):
            closed = True
        return closed

    def _manage_entries(self, entries, candle, is_long):
        closed = False
        to_remove = []
        for i, entry in enumerate(entries):
            if is_long:
                if entry["stop_loss"] is not None and float(candle.LowPrice) <= entry["stop_loss"]:
                    self.SellMarket()
                    to_remove.append(i)
                    closed = True
                    continue
                if entry["take_profit"] is not None and float(candle.HighPrice) >= entry["take_profit"]:
                    self.SellMarket()
                    to_remove.append(i)
                    closed = True
                    continue
                if entry["trailing_distance"] > 0:
                    profit = float(candle.ClosePrice) - entry["entry_price"]
                    if profit > entry["trailing_distance"] + entry["trailing_step"]:
                        new_stop = float(candle.ClosePrice) - entry["trailing_distance"]
                        if entry["trailing_stop"] is None or entry["trailing_stop"] < new_stop:
                            entry["trailing_stop"] = new_stop
                    if entry["trailing_stop"] is not None and float(candle.LowPrice) <= entry["trailing_stop"]:
                        self.SellMarket()
                        to_remove.append(i)
                        closed = True
            else:
                if entry["stop_loss"] is not None and float(candle.HighPrice) >= entry["stop_loss"]:
                    self.BuyMarket()
                    to_remove.append(i)
                    closed = True
                    continue
                if entry["take_profit"] is not None and float(candle.LowPrice) <= entry["take_profit"]:
                    self.BuyMarket()
                    to_remove.append(i)
                    closed = True
                    continue
                if entry["trailing_distance"] > 0:
                    profit = entry["entry_price"] - float(candle.ClosePrice)
                    if profit > entry["trailing_distance"] + entry["trailing_step"]:
                        new_stop = float(candle.ClosePrice) + entry["trailing_distance"]
                        if entry["trailing_stop"] is None or entry["trailing_stop"] > new_stop:
                            entry["trailing_stop"] = new_stop
                    if entry["trailing_stop"] is not None and float(candle.HighPrice) >= entry["trailing_stop"]:
                        self.BuyMarket()
                        to_remove.append(i)
                        closed = True

        for i in reversed(to_remove):
            entries.pop(i)
        return closed

    def _close_maximum_profit(self, price):
        best = None
        best_profit = -999999999.0
        best_list = None
        best_idx = -1

        for i, entry in enumerate(self._long_entries):
            p = price - entry["entry_price"]
            if p > best_profit:
                best_profit = p
                best = entry
                best_list = self._long_entries
                best_idx = i

        for i, entry in enumerate(self._short_entries):
            p = entry["entry_price"] - price
            if p > best_profit:
                best_profit = p
                best = entry
                best_list = self._short_entries
                best_idx = i

        if best is not None:
            if best["side"] == "buy":
                self.SellMarket()
            else:
                self.BuyMarket()
            best_list.pop(best_idx)

    def _close_minimum_profit(self, price):
        worst = None
        worst_profit = 999999999.0
        worst_list = None
        worst_idx = -1

        for i, entry in enumerate(self._long_entries):
            p = price - entry["entry_price"]
            if p < worst_profit:
                worst_profit = p
                worst = entry
                worst_list = self._long_entries
                worst_idx = i

        for i, entry in enumerate(self._short_entries):
            p = entry["entry_price"] - price
            if p < worst_profit:
                worst_profit = p
                worst = entry
                worst_list = self._short_entries
                worst_idx = i

        if worst is not None:
            if worst["side"] == "buy":
                self.SellMarket()
            else:
                self.BuyMarket()
            worst_list.pop(worst_idx)

    def _get_total_profit(self, price):
        total = 0.0
        for entry in self._long_entries:
            total += price - entry["entry_price"]
        for entry in self._short_entries:
            total += entry["entry_price"] - price
        return total

    def _get_interval_distance(self, open_positions):
        distance = self._interval_pips.Value * self._pip_size if self._interval_pips.Value > 0 else 0.0
        if distance <= 0:
            return 0.0
        if open_positions > 0:
            distance *= self._pow(self._interval_coefficient.Value, open_positions)
        return distance

    def _pow(self, value, exponent):
        result = 1.0
        for _ in range(exponent):
            result *= value
        return result

    def CreateClone(self):
        return dealers_trade_zero_lag_macd_strategy()
