import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, DateTime
from System.Collections.Generic import IEnumerable
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order


# ---------------------------------------------------------------------------
# Helper classes (ported from C# inner classes)
# ---------------------------------------------------------------------------

class RollingBuffer:
    """Fixed-capacity circular buffer of floats with aggregate helpers."""

    def __init__(self, capacity):
        self._capacity = max(1, capacity)
        self._buffer = [0.0] * self._capacity
        self._start = 0
        self._count = 0

    @property
    def count(self):
        return self._count

    def add(self, value):
        if self._count < self._capacity:
            index = (self._start + self._count) % self._capacity
            self._buffer[index] = value
            self._count += 1
        else:
            self._buffer[self._start] = value
            self._start = (self._start + 1) % self._capacity

    def enumerate_recent(self, n):
        if n > self._count:
            n = self._count
        result = []
        for i in range(n):
            index = (self._start + self._count - n + i) % self._capacity
            result.append(self._buffer[index])
        return result

    def max_val(self, n):
        if self._count == 0:
            return 0.0
        if n > self._count:
            n = self._count
        best = -1e308
        for i in range(n):
            index = (self._start + self._count - n + i) % self._capacity
            v = self._buffer[index]
            if v > best:
                best = v
        return best

    def min_val(self, n):
        if self._count == 0:
            return 0.0
        if n > self._count:
            n = self._count
        best = 1e308
        for i in range(n):
            index = (self._start + self._count - n + i) % self._capacity
            v = self._buffer[index]
            if v < best:
                best = v
        return best

    def average(self, n):
        if self._count == 0:
            return 0.0
        if n > self._count or n <= 0:
            n = self._count
        total = 0.0
        for i in range(n):
            index = (self._start + self._count - n + i) % self._capacity
            total += self._buffer[index]
        return total / n


class SecurityContext:
    """Per-security state: rolling closes, highs, lows, true-ranges, and Level1 prices."""

    def __init__(self, security, correlation_capacity, range_capacity, atr_capacity):
        self.security = security
        self._closes = RollingBuffer(max(2, correlation_capacity))
        self._highs = RollingBuffer(max(1, range_capacity))
        self._lows = RollingBuffer(max(1, range_capacity))
        self._true_ranges = RollingBuffer(max(1, atr_capacity))
        self._previous_close = 0.0
        self._has_previous_close = False
        self.last_close = 0.0
        self.best_bid = None
        self.best_ask = None

    @property
    def close_count(self):
        return self._closes.count

    @property
    def true_range_count(self):
        return self._true_ranges.count

    def update(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._closes.add(close)
        self._highs.add(high)
        self._lows.add(low)

        if self._has_previous_close:
            rng = high - low
            high_diff = abs(high - self._previous_close)
            low_diff = abs(low - self._previous_close)
            true_range = max(rng, max(high_diff, low_diff))
        else:
            true_range = high - low
            self._has_previous_close = True

        self._true_ranges.add(true_range)
        self._previous_close = close
        self.last_close = close

    def update_level1(self, message):
        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)
        if bid is not None:
            self.best_bid = float(bid)
        if ask is not None:
            self.best_ask = float(ask)

    def has_correlation_data(self, required):
        if required <= 0:
            return self._closes.count >= 2
        return self._closes.count >= required

    def has_range_data(self, required):
        return self._highs.count >= required and self._lows.count >= required

    def get_recent_closes(self, n):
        return self._closes.enumerate_recent(n)

    def get_high(self, n):
        return self._highs.max_val(n)

    def get_low(self, n):
        return self._lows.min_val(n)

    def get_average_true_range(self, n):
        return self._true_ranges.average(n)

    def get_spread_points(self):
        step = self.security.PriceStep
        if step is not None:
            step = float(step)
        else:
            step = 0.0
        if self.best_bid is None or self.best_ask is None or step <= 0.0:
            return 1e308
        return (self.best_ask - self.best_bid) / step


class HedgePairKey:
    """Hashable pair identifier for two securities."""

    def __init__(self, first, second):
        self.first = first
        self.second = second

    def __eq__(self, other):
        if not isinstance(other, HedgePairKey):
            return False
        return self.first == other.first and self.second == other.second

    def __hash__(self):
        return hash((id(self.first), id(self.second)))


class HedgeState:
    """Mutable state for one hedge pair."""

    def __init__(self, key):
        self.key = key
        first_id = key.first.Id if key.first is not None else "?"
        second_id = key.second.Id if key.second is not None else "?"
        self.tag = "HEDGE_{0}_{1}".format(first_id, second_id)
        self.is_positive = True
        self.atr_ratio = 0.0
        self.is_open = False
        self.dir1 = 0
        self.dir2 = 0
        self.volume1 = 0.0
        self.volume2 = 0.0
        self.entry1 = 0.0
        self.entry2 = 0.0

    @property
    def first(self):
        return self.key.first

    @property
    def second(self):
        return self.key.second


# Hedge action enum
HEDGE_NONE = 0
HEDGE_BUY_MAIN_SELL_SUB = 1
HEDGE_SELL_MAIN_BUY_SUB = 2
HEDGE_BUY_BOTH = 3
HEDGE_SELL_BOTH = 4


# ---------------------------------------------------------------------------
# Strategy
# ---------------------------------------------------------------------------

class multicurrency_overlay_hedge_strategy(Strategy):
    """
    Multicurrency overlay hedge strategy converted from MQL.
    Scans a universe of forex symbols, pairs positively/negatively correlated
    instruments and opens hedged blocks when the overlay threshold is breached.
    """

    def __init__(self):
        super(multicurrency_overlay_hedge_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Time frame used for analysis", "General")

        self._range_length = self.Param("RangeLength", 400) \
            .SetDisplay("Range Length", "Bars used to build price envelopes", "Parameters")

        self._correlation_lookback = self.Param("CorrelationLookback", 500) \
            .SetDisplay("Correlation Lookback", "Bars used for Pearson correlation", "Parameters")

        self._atr_lookback = self.Param("AtrLookback", 200) \
            .SetDisplay("ATR Lookback", "Bars used to compute ATR ratio", "Parameters")

        self._correlation_threshold = self.Param("CorrelationThreshold", 0.9) \
            .SetDisplay("Correlation Threshold", "Absolute correlation required for pairing", "Parameters")

        self._overlay_threshold = self.Param("OverlayThreshold", 100.0) \
            .SetDisplay("Overlay Threshold", "Distance in points to trigger hedging", "Trading")

        self._take_profit_by_points = self.Param("TakeProfitByPoints", True) \
            .SetDisplay("TP by Points", "Enable point based take profit", "Risk")

        self._take_profit_points = self.Param("TakeProfitPoints", 10.0) \
            .SetDisplay("Points Target", "Mutual take profit in points", "Risk")

        self._take_profit_by_currency = self.Param("TakeProfitByCurrency", False) \
            .SetDisplay("TP by Currency", "Enable currency based take profit", "Risk")

        self._take_profit_currency = self.Param("TakeProfitCurrency", 10.0) \
            .SetDisplay("Currency Target", "Mutual take profit in account currency", "Risk")

        self._max_open_pairs = self.Param("MaxOpenPairs", 10) \
            .SetDisplay("Max Pairs", "Maximum simultaneously open hedges", "Risk")

        self._base_volume_param = self.Param("BaseVolume", 1.0) \
            .SetDisplay("Base Volume", "Secondary leg volume in lots", "Trading")

        self._recalc_hour = self.Param("RecalculationHour", 1) \
            .SetDisplay("Recalc Hour", "Hour to rebuild pair statistics", "Trading")

        self._max_spread = self.Param("MaxSpread", 10.0) \
            .SetDisplay("Max Spread", "Max allowed spread in points", "Trading")

        self._universe_param = self.Param("Universe", None) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")

        # Internal state
        self._contexts = {}           # Security -> SecurityContext
        self._pairs = {}              # HedgePairKey -> HedgeState
        self._pairs_by_security = {}  # Security -> [HedgePairKey]
        self._universe_list = []      # [Security]
        self._last_recalc_day = DateTime.MinValue

    # --- Properties ---------------------------------------------------------

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RangeLength(self):
        return int(self._range_length.Value)

    @property
    def CorrelationLookback(self):
        return int(self._correlation_lookback.Value)

    @property
    def AtrLookback(self):
        return int(self._atr_lookback.Value)

    @property
    def CorrelationThreshold(self):
        return float(self._correlation_threshold.Value)

    @property
    def OverlayThreshold(self):
        return float(self._overlay_threshold.Value)

    @property
    def TakeProfitByPoints(self):
        return bool(self._take_profit_by_points.Value)

    @property
    def TakeProfitPoints(self):
        return float(self._take_profit_points.Value)

    @property
    def TakeProfitByCurrency(self):
        return bool(self._take_profit_by_currency.Value)

    @property
    def TakeProfitCurrency(self):
        return float(self._take_profit_currency.Value)

    @property
    def MaxOpenPairs(self):
        return int(self._max_open_pairs.Value)

    @property
    def BaseVolume(self):
        return float(self._base_volume_param.Value)

    @property
    def RecalculationHour(self):
        return int(self._recalc_hour.Value)

    @property
    def MaxSpread(self):
        return float(self._max_spread.Value)

    # --- Overrides ----------------------------------------------------------

    def GetWorkingSecurities(self):
        result = []
        universe = self._universe_param.Value
        if universe is not None:
            for sec in universe:
                if sec is not None:
                    result.append((sec, self.CandleType))
        return result

    def OnReseted(self):
        super(multicurrency_overlay_hedge_strategy, self).OnReseted()
        self._contexts.clear()
        self._pairs.clear()
        self._pairs_by_security.clear()
        self._universe_list = []
        self._last_recalc_day = DateTime.MinValue

    def OnStarted2(self, time):
        super(multicurrency_overlay_hedge_strategy, self).OnStarted2(time)

        universe = self._universe_param.Value
        self._universe_list = []
        if universe is not None:
            for sec in universe:
                if sec is not None and sec not in self._universe_list:
                    self._universe_list.append(sec)

        if len(self._universe_list) < 2:
            raise Exception("Universe must contain at least two securities.")

        for sec in self._universe_list:
            corr_cap = max(2, self.CorrelationLookback)
            ctx = SecurityContext(sec, corr_cap, self.RangeLength, self.AtrLookback)
            self._contexts[sec] = ctx
            self._pairs_by_security[sec] = []

            self.SubscribeCandles(self.CandleType, True, sec) \
                .Bind(lambda candle, s=sec: self._process_candle(candle, s)) \
                .Start()

            self.SubscribeLevel1(sec) \
                .Bind(lambda msg, c=ctx: c.update_level1(msg)) \
                .Start()

        self.StartProtection(None, None)

    def CreateClone(self):
        return multicurrency_overlay_hedge_strategy()

    # --- Candle processing --------------------------------------------------

    def _process_candle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        ctx = self._contexts.get(security)
        if ctx is None:
            return

        ctx.update(candle)

        if self._should_recalculate(candle):
            self._recalculate_pairs()

        self._manage_open_hedges()

        pair_keys = self._pairs_by_security.get(security)
        if pair_keys is not None:
            for key in list(pair_keys):
                self._try_open_hedge(key)

    # --- Recalculation timing -----------------------------------------------

    def _should_recalculate(self, candle):
        try:
            open_time = candle.OpenTime.UtcDateTime
        except Exception:
            open_time = candle.OpenTime

        day = open_time.Date
        if day == self._last_recalc_day:
            return False

        if open_time.Hour < self.RecalculationHour:
            return False

        self._last_recalc_day = day
        return True

    # --- Pair recalculation -------------------------------------------------

    def _recalculate_pairs(self):
        for lst in self._pairs_by_security.values():
            del lst[:]

        count = len(self._universe_list)
        correlation_lookback = self.CorrelationLookback
        correlation_threshold = self.CorrelationThreshold

        for i in range(count):
            first = self._universe_list[i]
            first_ctx = self._contexts[first]
            if not first_ctx.has_correlation_data(correlation_lookback):
                continue

            for j in range(i + 1, count):
                second = self._universe_list[j]
                second_ctx = self._contexts[second]
                if not second_ctx.has_correlation_data(correlation_lookback):
                    continue

                correlation = self._calculate_correlation(first_ctx, second_ctx)
                abs_corr = abs(correlation)
                if abs_corr < correlation_threshold:
                    continue

                atr_ratio = self._calculate_atr_ratio(first_ctx, second_ctx)
                if atr_ratio <= 0.0:
                    continue

                key = HedgePairKey(first, second)
                state = self._pairs.get(key)
                if state is None:
                    state = HedgeState(key)
                    self._pairs[key] = state

                state.is_positive = correlation >= 0.0
                state.atr_ratio = atr_ratio

                self._pairs_by_security[first].append(key)
                self._pairs_by_security[second].append(key)

        to_remove = []
        for key, state in list(self._pairs.items()):
            if state.is_open:
                continue
            first_list = self._pairs_by_security.get(key.first)
            if first_list is None or key not in first_list:
                to_remove.append(key)

        for key in to_remove:
            del self._pairs[key]

    # --- Open hedge management ----------------------------------------------

    def _manage_open_hedges(self):
        for key, state in list(self._pairs.items()):
            if not state.is_open:
                continue

            points = self._calculate_points(state)
            if self.TakeProfitByPoints and points >= self.TakeProfitPoints:
                self._close_hedge(state, "TP_POINTS")
                continue

            currency = self._calculate_currency(state)
            if self.TakeProfitByCurrency and currency >= self.TakeProfitCurrency:
                self._close_hedge(state, "TP_CURRENCY")

    # --- Try opening a hedge ------------------------------------------------

    def _try_open_hedge(self, key):
        state = self._pairs.get(key)
        if state is None:
            return

        if state.is_open:
            return

        first_ctx = self._contexts.get(key.first)
        second_ctx = self._contexts.get(key.second)
        if first_ctx is None or second_ctx is None:
            return

        range_length = self.RangeLength
        if not first_ctx.has_range_data(range_length) or not second_ctx.has_range_data(range_length):
            return

        if not self._is_security_available(key.first) or not self._is_security_available(key.second):
            return

        max_open = self.MaxOpenPairs
        if max_open > 0 and self._get_open_pairs_count() >= max_open:
            return

        if not self._is_spread_within_limit(first_ctx) or not self._is_spread_within_limit(second_ctx):
            return

        action = self._determine_action(state, first_ctx, second_ctx)
        if action == HEDGE_NONE:
            return

        base_volume = self.BaseVolume
        if base_volume <= 0.0:
            return

        scaled_volume = base_volume * state.atr_ratio
        if scaled_volume <= 0.0:
            return

        dir_first, dir_second = self._get_directions(action)
        target_first = dir_first * scaled_volume
        target_second = dir_second * base_volume

        self._trade_to_target(key.first, target_first, state.tag)
        self._trade_to_target(key.second, target_second, state.tag)

        state.dir1 = dir_first
        state.dir2 = dir_second
        state.volume1 = scaled_volume
        state.volume2 = base_volume
        state.entry1 = first_ctx.last_close
        state.entry2 = second_ctx.last_close
        state.is_open = True

    # --- Utilities ----------------------------------------------------------

    def _is_security_available(self, security):
        for key, state in self._pairs.items():
            if not state.is_open:
                continue
            if key.first == security or key.second == security:
                return False
        return True

    def _get_open_pairs_count(self):
        count = 0
        for state in self._pairs.values():
            if state.is_open:
                count += 1
        return count

    def _is_spread_within_limit(self, ctx):
        max_spread = self.MaxSpread
        if max_spread <= 0.0:
            return True
        spread = ctx.get_spread_points()
        if spread >= 1e308:
            return True
        return spread <= max_spread

    # --- Determine hedge action ---------------------------------------------

    def _determine_action(self, state, first_ctx, second_ctx):
        range_length = self.RangeLength
        high_main = first_ctx.get_high(range_length)
        low_main = first_ctx.get_low(range_length)
        if high_main <= low_main:
            return HEDGE_NONE

        if state.is_positive:
            sub_high = second_ctx.get_high(range_length)
            sub_low = second_ctx.get_low(range_length)
        else:
            sub_high = second_ctx.get_low(range_length)
            sub_low = second_ctx.get_high(range_length)

        if sub_high <= sub_low:
            return HEDGE_NONE

        main_center = (high_main + low_main) / 2.0
        sub_center = (sub_high + sub_low) / 2.0
        denominator = sub_high - sub_low
        if denominator == 0.0:
            return HEDGE_NONE

        pips_ratio = (high_main - low_main) / denominator
        if pips_ratio == 0.0:
            return HEDGE_NONE

        sub_close_offset = second_ctx.last_close - sub_center
        synthetic_close = main_center + sub_close_offset * pips_ratio

        step = first_ctx.security.PriceStep
        if step is not None:
            step = float(step)
        else:
            step = 0.0
        if step <= 0.0:
            step = 1.0

        hedge_range = (first_ctx.last_close - synthetic_close) / step
        overlay_threshold = self.OverlayThreshold

        if hedge_range < -overlay_threshold:
            return HEDGE_BUY_MAIN_SELL_SUB if state.is_positive else HEDGE_BUY_BOTH

        if hedge_range > overlay_threshold:
            return HEDGE_SELL_MAIN_BUY_SUB if state.is_positive else HEDGE_SELL_BOTH

        return HEDGE_NONE

    def _get_directions(self, action):
        if action == HEDGE_BUY_MAIN_SELL_SUB:
            return (1, -1)
        elif action == HEDGE_SELL_MAIN_BUY_SUB:
            return (-1, 1)
        elif action == HEDGE_BUY_BOTH:
            return (1, 1)
        elif action == HEDGE_SELL_BOTH:
            return (-1, -1)
        return (0, 0)

    # --- Order execution ----------------------------------------------------

    def _trade_to_target(self, security, target_volume, tag):
        if self.Portfolio is None:
            return

        pos_val = self.GetPositionValue(security, self.Portfolio)
        current = float(pos_val) if pos_val is not None else 0.0
        diff = target_volume - current
        if abs(diff) < 1e-6:
            return

        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Volume = abs(diff)
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Type = OrderTypes.Market
        order.Comment = tag
        self.RegisterOrder(order)

    def _close_hedge(self, state, reason):
        self._trade_to_target(state.first, 0.0, reason)
        self._trade_to_target(state.second, 0.0, reason)

        state.is_open = False
        state.dir1 = 0
        state.dir2 = 0
        state.volume1 = 0.0
        state.volume2 = 0.0
        state.entry1 = 0.0
        state.entry2 = 0.0

    # --- P&L calculations ---------------------------------------------------

    def _calculate_points(self, state):
        first_ctx = self._contexts.get(state.first)
        second_ctx = self._contexts.get(state.second)
        if first_ctx is None or second_ctx is None:
            return 0.0

        step_first = first_ctx.security.PriceStep
        step_first = float(step_first) if step_first is not None else 1.0
        if step_first == 0.0:
            step_first = 1.0

        step_second = second_ctx.security.PriceStep
        step_second = float(step_second) if step_second is not None else 1.0
        if step_second == 0.0:
            step_second = 1.0

        move_first = state.dir1 * (first_ctx.last_close - state.entry1) / step_first * state.volume1
        move_second = state.dir2 * (second_ctx.last_close - state.entry2) / step_second * state.volume2
        return move_first + move_second

    def _calculate_currency(self, state):
        first_ctx = self._contexts.get(state.first)
        second_ctx = self._contexts.get(state.second)
        if first_ctx is None or second_ctx is None:
            return 0.0

        step_first = first_ctx.security.PriceStep
        step_first = float(step_first) if step_first is not None else 1.0
        if step_first == 0.0:
            step_first = 1.0

        step_second = second_ctx.security.PriceStep
        step_second = float(step_second) if step_second is not None else 1.0
        if step_second == 0.0:
            step_second = 1.0

        # Try to get the monetary value of one price step; fall back to the step itself.
        price_step_first = step_first
        price_step_second = step_second
        try:
            v = self.GetSecurityValue[object](first_ctx.security, Level1Fields.StepPrice)
            if v is not None:
                price_step_first = float(v)
        except Exception:
            pass
        try:
            v = self.GetSecurityValue[object](second_ctx.security, Level1Fields.StepPrice)
            if v is not None:
                price_step_second = float(v)
        except Exception:
            pass

        pnl_first = state.dir1 * (first_ctx.last_close - state.entry1) / step_first * price_step_first * state.volume1
        pnl_second = state.dir2 * (second_ctx.last_close - state.entry2) / step_second * price_step_second * state.volume2
        return pnl_first + pnl_second

    # --- Correlation --------------------------------------------------------

    def _calculate_correlation(self, first_ctx, second_ctx):
        lookback = self.CorrelationLookback
        available = min(first_ctx.close_count, second_ctx.close_count)
        if lookback <= 0 or lookback > available:
            lookback = available
        if lookback < 2:
            return 0.0

        xs = first_ctx.get_recent_closes(lookback)
        ys = second_ctx.get_recent_closes(lookback)

        sum_x = 0.0
        sum_y = 0.0
        sum_xy = 0.0
        sum_x2 = 0.0
        sum_y2 = 0.0

        for x, y in zip(xs, ys):
            sum_x += x
            sum_y += y
            sum_xy += x * y
            sum_x2 += x * x
            sum_y2 += y * y

        numerator = lookback * sum_xy - sum_x * sum_y
        denom_part1 = lookback * sum_x2 - sum_x * sum_x
        denom_part2 = lookback * sum_y2 - sum_y * sum_y
        if denom_part1 <= 0.0 or denom_part2 <= 0.0:
            return 0.0

        denominator = math.sqrt(denom_part1 * denom_part2)
        if denominator == 0.0:
            return 0.0

        return numerator / denominator

    # --- ATR ratio ----------------------------------------------------------

    def _calculate_atr_ratio(self, first_ctx, second_ctx):
        lookback = self.AtrLookback
        available = min(first_ctx.true_range_count, second_ctx.true_range_count)
        if lookback <= 0 or lookback > available:
            lookback = available
        if lookback <= 0:
            return 0.0

        atr_first = first_ctx.get_average_true_range(lookback)
        atr_second = second_ctx.get_average_true_range(lookback)
        if atr_first <= 0.0 or atr_second <= 0.0:
            return 0.0

        return atr_second / atr_first
