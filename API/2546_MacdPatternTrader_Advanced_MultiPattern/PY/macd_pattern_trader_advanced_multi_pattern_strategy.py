import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (MovingAverageConvergenceDivergenceSignal,
    ExponentialMovingAverage, SimpleMovingAverage)
from StockSharp.Algo.Strategies import Strategy


class macd_pattern_trader_advanced_multi_pattern_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_advanced_multi_pattern_strategy, self).__init__()

        self._macd_history_length = self.Param("MacdHistoryLength", 3)
        self._candle_history_limit = self.Param("CandleHistoryLimit", 1000)
        self._min_partial_volume = self.Param("MinPartialVolume", 0.01)
        self._profit_threshold = self.Param("ProfitThreshold", 5.0)

        self._p1_enabled = self.Param("Pattern1Enabled", True)
        self._p1_sl_bars = self.Param("Pattern1StopLossBars", 22)
        self._p1_tp_bars = self.Param("Pattern1TakeProfitBars", 32)
        self._p1_offset = self.Param("Pattern1Offset", 40)
        self._p1_slow = self.Param("Pattern1Slow", 13)
        self._p1_fast = self.Param("Pattern1Fast", 24)
        self._p1_max = self.Param("Pattern1MaxThreshold", 0.0095)
        self._p1_min = self.Param("Pattern1MinThreshold", -0.0045)

        self._p2_enabled = self.Param("Pattern2Enabled", True)
        self._p2_sl_bars = self.Param("Pattern2StopLossBars", 2)
        self._p2_tp_bars = self.Param("Pattern2TakeProfitBars", 2)
        self._p2_offset = self.Param("Pattern2Offset", 50)
        self._p2_slow = self.Param("Pattern2Slow", 7)
        self._p2_fast = self.Param("Pattern2Fast", 17)
        self._p2_max = self.Param("Pattern2MaxThreshold", 0.0045)
        self._p2_min = self.Param("Pattern2MinThreshold", -0.0035)

        self._p3_enabled = self.Param("Pattern3Enabled", True)
        self._p3_sl_bars = self.Param("Pattern3StopLossBars", 8)
        self._p3_tp_bars = self.Param("Pattern3TakeProfitBars", 12)
        self._p3_offset = self.Param("Pattern3Offset", 2)
        self._p3_slow = self.Param("Pattern3Slow", 2)
        self._p3_fast = self.Param("Pattern3Fast", 32)
        self._p3_max = self.Param("Pattern3MaxThreshold", 0.0015)
        self._p3_max_low = self.Param("Pattern3MaxLowThreshold", 0.004)
        self._p3_min = self.Param("Pattern3MinThreshold", -0.005)
        self._p3_min_high = self.Param("Pattern3MinHighThreshold", -0.0005)

        self._p4_enabled = self.Param("Pattern4Enabled", True)
        self._p4_sl_bars = self.Param("Pattern4StopLossBars", 10)
        self._p4_tp_bars = self.Param("Pattern4TakeProfitBars", 32)
        self._p4_offset = self.Param("Pattern4Offset", 45)
        self._p4_slow = self.Param("Pattern4Slow", 9)
        self._p4_fast = self.Param("Pattern4Fast", 4)
        self._p4_max = self.Param("Pattern4MaxThreshold", 0.0165)
        self._p4_max_low = self.Param("Pattern4MaxLowThreshold", 0.0001)
        self._p4_min = self.Param("Pattern4MinThreshold", -0.0005)
        self._p4_min_high = self.Param("Pattern4MinHighThreshold", -0.0006)

        self._p5_enabled = self.Param("Pattern5Enabled", True)
        self._p5_sl_bars = self.Param("Pattern5StopLossBars", 8)
        self._p5_tp_bars = self.Param("Pattern5TakeProfitBars", 47)
        self._p5_offset = self.Param("Pattern5Offset", 45)
        self._p5_slow = self.Param("Pattern5Slow", 2)
        self._p5_fast = self.Param("Pattern5Fast", 6)
        self._p5_max_neutral = self.Param("Pattern5MaxNeutralThreshold", 0.0005)
        self._p5_max = self.Param("Pattern5MaxThreshold", 0.0015)
        self._p5_min_neutral = self.Param("Pattern5MinNeutralThreshold", -0.0005)
        self._p5_min = self.Param("Pattern5MinThreshold", -0.003)

        self._p6_enabled = self.Param("Pattern6Enabled", True)
        self._p6_sl_bars = self.Param("Pattern6StopLossBars", 26)
        self._p6_tp_bars = self.Param("Pattern6TakeProfitBars", 42)
        self._p6_offset = self.Param("Pattern6Offset", 20)
        self._p6_slow = self.Param("Pattern6Slow", 8)
        self._p6_fast = self.Param("Pattern6Fast", 4)
        self._p6_max = self.Param("Pattern6MaxThreshold", 0.0005)
        self._p6_min = self.Param("Pattern6MinThreshold", -0.001)
        self._p6_max_bars = self.Param("Pattern6MaxBars", 5)
        self._p6_min_bars = self.Param("Pattern6MinBars", 5)
        self._p6_trigger_bars = self.Param("Pattern6TriggerBars", 4)

        self._ema_period1 = self.Param("EmaPeriod1", 7)
        self._ema_period2 = self.Param("EmaPeriod2", 21)
        self._sma_period3 = self.Param("SmaPeriod3", 98)
        self._ema_period4 = self.Param("EmaPeriod4", 365)

        self._initial_volume = self.Param("InitialVolume", 0.1)
        self._use_time_filter = self.Param("UseTimeFilter", False)
        self._start_time = self.Param("StartTime", 7)
        self._stop_time = self.Param("StopTime", 17)
        self._use_martingale = self.Param("UseMartingale", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._candles = []
        self._macd_histories = [[], [], [], [], [], []]
        self._p1_was_above = False
        self._p1_was_below = False
        self._p2_was_positive = False
        self._p2_was_negative = False
        self._p2_sell_armed = False
        self._p2_buy_armed = False
        self._p3_bars_bup = 0
        self._p6_bars_above = 0
        self._p6_bars_below = 0
        self._p6_sell_blocked = False
        self._p6_buy_blocked = False
        self._p6_sell_ready = False
        self._p6_buy_ready = False
        self._current_volume = 0.1
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(macd_pattern_trader_advanced_multi_pattern_strategy, self).OnStarted(time)

        self._current_volume = float(self._initial_volume.Value)
        self._candles = []
        self._macd_histories = [[], [], [], [], [], []]
        self._p1_was_above = False
        self._p1_was_below = False
        self._p2_was_positive = False
        self._p2_was_negative = False
        self._p2_sell_armed = False
        self._p2_buy_armed = False
        self._p3_bars_bup = 0
        self._p6_bars_above = 0
        self._p6_bars_below = 0
        self._p6_sell_blocked = False
        self._p6_buy_blocked = False
        self._p6_sell_ready = False
        self._p6_buy_ready = False
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

        self._macds = []
        params = [
            (self._p1_fast, self._p1_slow),
            (self._p2_fast, self._p2_slow),
            (self._p3_fast, self._p3_slow),
            (self._p4_fast, self._p4_slow),
            (self._p5_fast, self._p5_slow),
            (self._p6_fast, self._p6_slow),
        ]
        for fast_p, slow_p in params:
            macd = MovingAverageConvergenceDivergenceSignal()
            macd.Macd.ShortMa.Length = int(fast_p.Value)
            macd.Macd.LongMa.Length = int(slow_p.Value)
            macd.SignalMa.Length = 1
            self._macds.append(macd)

        self._ema1 = ExponentialMovingAverage()
        self._ema1.Length = int(self._ema_period1.Value)
        self._ema2 = ExponentialMovingAverage()
        self._ema2.Length = int(self._ema_period2.Value)
        self._sma3 = SimpleMovingAverage()
        self._sma3.Length = int(self._sma_period3.Value)
        self._ema4 = ExponentialMovingAverage()
        self._ema4.Length = int(self._ema_period4.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandleRaw).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandleRaw(self, candle):
        if candle.State != CandleStates.Finished:
            return

        macd_vals = []
        for m in self._macds:
            macd_vals.append(m.Process(candle))

        ema1_val = self._ema1.Process(candle)
        ema2_val = self._ema2.Process(candle)
        sma3_val = self._sma3.Process(candle)
        ema4_val = self._ema4.Process(candle)

        self._candles.append(candle)
        limit = int(self._candle_history_limit.Value)
        if len(self._candles) > limit:
            self._candles.pop(0)

        self._check_risk(candle)

        macd_currents = []
        all_final = True
        for val in macd_vals:
            if not val.IsFinal:
                all_final = False
                break
            macd_line = val.Macd if hasattr(val, 'Macd') else None
            if macd_line is not None:
                macd_currents.append(float(macd_line))
            else:
                all_final = False
                break

        if not all_final:
            self._update_macd_histories(macd_vals)
            return

        if not self._ema2.IsFormed or not self._sma3.IsFormed or not self._ema4.IsFormed:
            self._update_macd_histories(macd_vals)
            return

        ema2 = float(ema2_val)
        sma3 = float(sma3_val)
        ema4 = float(ema4_val)

        self._manage_positions(candle, ema2, sma3, ema4)

        hist_len = int(self._macd_history_length.Value)
        use_tf = bool(self._use_time_filter.Value)
        time_ok = not use_tf or self._is_in_window(candle)

        if time_ok:
            for i in range(6):
                h = self._macd_histories[i]
                if len(h) < 2:
                    continue
                prev1 = h[-1]
                prev2 = h[-2] if len(h) >= 2 else None
                if prev1 is None or prev2 is None:
                    continue
                mc = macd_currents[i]
                if i == 0 and bool(self._p1_enabled.Value):
                    self._process_p1(candle, mc, prev1, prev2)
                elif i == 1 and bool(self._p2_enabled.Value):
                    self._process_p2(candle, mc, prev1, prev2)
                elif i == 2 and bool(self._p3_enabled.Value):
                    self._process_p3(candle, mc, prev1, prev2)
                elif i == 3 and bool(self._p4_enabled.Value):
                    self._process_p4(candle, mc, prev1, prev2)
                elif i == 4 and bool(self._p5_enabled.Value):
                    self._process_p5(candle, mc, prev1, prev2)
                elif i == 5 and bool(self._p6_enabled.Value):
                    self._process_p6(candle, mc, prev1, prev2)

        self._update_macd_histories(macd_vals)

    def _process_p1(self, candle, mc, ml, ml3):
        p1_max = float(self._p1_max.Value)
        p1_min = float(self._p1_min.Value)
        if mc > p1_max:
            self._p1_was_above = True
        if mc < 0.0:
            self._p1_was_above = False
        if mc < p1_max and mc < ml and ml > ml3 and self._p1_was_above and mc > 0.0 and ml3 < p1_max:
            if self._try_open_short(candle, int(self._p1_sl_bars.Value), int(self._p1_offset.Value), int(self._p1_tp_bars.Value)):
                self._p1_was_above = False

        if mc < p1_min:
            self._p1_was_below = True
        if mc > 0.0:
            self._p1_was_below = False
        if mc > p1_min and mc < 0.0 and mc > ml and ml < ml3 and self._p1_was_below and ml3 > p1_min:
            if self._try_open_long(candle, int(self._p1_sl_bars.Value), int(self._p1_offset.Value), int(self._p1_tp_bars.Value)):
                self._p1_was_below = False

    def _process_p2(self, candle, mc, ml, ml3):
        p2_max = float(self._p2_max.Value)
        p2_min = float(self._p2_min.Value)
        if mc > 0.0:
            self._p2_was_positive = True
            self._p2_sell_armed = False
        if mc > ml and ml < ml3 and self._p2_was_positive and mc > p2_min and mc < 0.0 and not self._p2_sell_armed:
            self._p2_sell_armed = True
        if self._p2_sell_armed and mc < ml and ml > ml3 and mc < 0.0:
            if self._try_open_short(candle, int(self._p2_sl_bars.Value), int(self._p2_offset.Value), int(self._p2_tp_bars.Value)):
                self._p2_was_positive = False
                self._p2_sell_armed = False

        if mc < 0.0:
            self._p2_was_negative = True
            self._p2_buy_armed = False
        if mc < p2_max and mc < ml and ml > ml3 and self._p2_was_negative and mc > 0.0:
            self._p2_buy_armed = True
        if self._p2_buy_armed and mc > ml and ml < ml3 and mc > 0.0:
            if self._try_open_long(candle, int(self._p2_sl_bars.Value), int(self._p2_offset.Value), int(self._p2_tp_bars.Value)):
                self._p2_was_negative = False
                self._p2_buy_armed = False

    def _process_p3(self, candle, mc, ml, ml3):
        p3_max = float(self._p3_max.Value)
        p3_max_low = float(self._p3_max_low.Value)
        p3_min = float(self._p3_min.Value)
        p3_min_high = float(self._p3_min_high.Value)

        if mc > p3_max_low:
            self._p3_bars_bup += 1
        if mc < p3_max and mc < ml and ml > ml3 and mc > 0.0:
            if self._try_open_short(candle, int(self._p3_sl_bars.Value), int(self._p3_offset.Value), int(self._p3_tp_bars.Value)):
                self._p3_bars_bup = 0
                return

        if mc > p3_min and mc > ml and ml < ml3 and mc < 0.0:
            self._try_open_long(candle, int(self._p3_sl_bars.Value), int(self._p3_offset.Value), int(self._p3_tp_bars.Value))

    def _process_p4(self, candle, mc, ml, ml3):
        p4_max = float(self._p4_max.Value)
        p4_min = float(self._p4_min.Value)
        if mc > p4_max and mc < ml and ml > ml3:
            self._try_open_short(candle, int(self._p4_sl_bars.Value), int(self._p4_offset.Value), int(self._p4_tp_bars.Value))
        if mc < p4_min and mc > ml and ml < ml3:
            self._try_open_long(candle, int(self._p4_sl_bars.Value), int(self._p4_offset.Value), int(self._p4_tp_bars.Value))

    def _process_p5(self, candle, mc, ml, ml3):
        p5_min_neutral = float(self._p5_min_neutral.Value)
        p5_min = float(self._p5_min.Value)
        p5_max_neutral = float(self._p5_max_neutral.Value)
        p5_max = float(self._p5_max.Value)

        if mc < p5_min_neutral and mc < ml and ml > ml3 and mc < p5_min and ml > p5_min:
            self._try_open_short(candle, int(self._p5_sl_bars.Value), int(self._p5_offset.Value), int(self._p5_tp_bars.Value))

        if mc > p5_max_neutral and mc > ml and ml < ml3 and mc > p5_max and ml < p5_max:
            self._try_open_long(candle, int(self._p5_sl_bars.Value), int(self._p5_offset.Value), int(self._p5_tp_bars.Value))

    def _process_p6(self, candle, mc, ml, ml3):
        p6_max = float(self._p6_max.Value)
        p6_min = float(self._p6_min.Value)
        max_bars = int(self._p6_max_bars.Value)
        min_bars = int(self._p6_min_bars.Value)
        trigger = int(self._p6_trigger_bars.Value)

        if mc < p6_max:
            self._p6_sell_blocked = False
        if mc > p6_max and self._p6_bars_above <= max_bars and not self._p6_sell_blocked:
            self._p6_bars_above += 1
        if self._p6_bars_above > max_bars:
            self._p6_bars_above = 0
            self._p6_sell_blocked = True
        if self._p6_bars_above < min_bars and mc < p6_max:
            self._p6_bars_above = 0
        if mc < p6_max and self._p6_bars_above > trigger:
            self._p6_sell_ready = True

        if self._p6_sell_ready:
            if self._try_open_short(candle, int(self._p6_sl_bars.Value), int(self._p6_offset.Value), int(self._p6_tp_bars.Value)):
                self._p6_sell_ready = False
                self._p6_bars_above = 0
                self._p6_sell_blocked = False
                return

        if mc > p6_min:
            self._p6_buy_blocked = False
        if mc < p6_min and self._p6_bars_below <= max_bars and not self._p6_buy_blocked:
            self._p6_bars_below += 1
        if self._p6_bars_below > max_bars:
            self._p6_buy_blocked = True
            self._p6_bars_below = 0
        if self._p6_bars_below < min_bars and mc > p6_min:
            self._p6_bars_below = 0
        if mc > p6_min and self._p6_bars_below > trigger:
            self._p6_buy_ready = True

        if self._p6_buy_ready:
            if self._try_open_long(candle, int(self._p6_sl_bars.Value), int(self._p6_offset.Value), int(self._p6_tp_bars.Value)):
                self._p6_buy_ready = False
                self._p6_bars_below = 0
                self._p6_buy_blocked = False

    def _try_open_long(self, candle, sl_bars, offset, tp_bars):
        stop = self._calc_stop_price(True, sl_bars, offset)
        take = self._calc_take_price(True, tp_bars)
        if stop is None or take is None:
            return False
        self.BuyMarket()
        self._long_stop = stop
        self._long_take = take
        self._short_stop = None
        self._short_take = None
        return True

    def _try_open_short(self, candle, sl_bars, offset, tp_bars):
        stop = self._calc_stop_price(False, sl_bars, offset)
        take = self._calc_take_price(False, tp_bars)
        if stop is None or take is None:
            return False
        self.SellMarket()
        self._short_stop = stop
        self._short_take = take
        self._long_stop = None
        self._long_take = None
        return True

    def _calc_stop_price(self, is_long, sl_bars, offset):
        if sl_bars <= 0 or len(self._candles) < sl_bars:
            return None
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        offset_val = offset * step
        if is_long:
            lowest = None
            start = max(0, len(self._candles) - sl_bars)
            for i in range(start, len(self._candles)):
                v = float(self._candles[i].LowPrice)
                if lowest is None or v < lowest:
                    lowest = v
            return lowest - offset_val if lowest is not None else None
        else:
            highest = None
            start = max(0, len(self._candles) - sl_bars)
            for i in range(start, len(self._candles)):
                v = float(self._candles[i].HighPrice)
                if highest is None or v > highest:
                    highest = v
            return highest + offset_val if highest is not None else None

    def _calc_take_price(self, is_long, tp_bars):
        if tp_bars <= 0:
            return None
        return self._get_segment_extreme(is_long, tp_bars, 0)

    def _get_segment_extreme(self, is_long, count, start):
        start_idx = len(self._candles) - 1 - start
        end_idx = start_idx - (count - 1)
        if start_idx < 0 or end_idx < 0:
            return None
        if is_long:
            extreme = -1e18
        else:
            extreme = 1e18
        for i in range(start_idx, end_idx - 1, -1):
            c = self._candles[i]
            if is_long:
                v = float(c.HighPrice)
                if v > extreme:
                    extreme = v
            else:
                v = float(c.LowPrice)
                if v < extreme:
                    extreme = v
        if extreme == -1e18 or extreme == 1e18:
            return None
        next_start = start + count
        nxt = self._get_segment_extreme(is_long, count, next_start)
        if nxt is not None:
            if is_long and nxt > extreme:
                return nxt
            elif not is_long and nxt < extreme:
                return nxt
        return extreme

    def _manage_positions(self, candle, ema2, sma3, ema4):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        profit_th = float(self._profit_threshold.Value)

        if self.Position > 0:
            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
            elif self._long_take is not None and high >= self._long_take:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
        elif self.Position < 0:
            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None
            elif self._short_take is not None and low <= self._short_take:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None

    def _check_risk(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if self.Position > 0:
            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
            elif self._long_take is not None and high >= self._long_take:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
        elif self.Position < 0:
            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None
            elif self._short_take is not None and low <= self._short_take:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None

    def _update_macd_histories(self, macd_vals):
        hist_len = int(self._macd_history_length.Value)
        for i, val in enumerate(macd_vals):
            macd_line = val.Macd if hasattr(val, 'Macd') else None
            if macd_line is None:
                continue
            self._macd_histories[i].append(float(macd_line))
            if len(self._macd_histories[i]) > hist_len:
                self._macd_histories[i].pop(0)

    def _is_in_window(self, candle):
        hour = candle.OpenTime.TimeOfDay.Hours
        start = int(self._start_time.Value)
        stop = int(self._stop_time.Value)
        if start == stop:
            return True
        if start < stop:
            return hour > start and hour < stop
        return hour > start or hour < stop

    def OnReseted(self):
        super(macd_pattern_trader_advanced_multi_pattern_strategy, self).OnReseted()
        self._candles = []
        self._macd_histories = [[], [], [], [], [], []]
        self._p1_was_above = False
        self._p1_was_below = False
        self._p2_was_positive = False
        self._p2_was_negative = False
        self._p2_sell_armed = False
        self._p2_buy_armed = False
        self._p3_bars_bup = 0
        self._p6_bars_above = 0
        self._p6_bars_below = 0
        self._p6_sell_blocked = False
        self._p6_buy_blocked = False
        self._p6_sell_ready = False
        self._p6_buy_ready = False
        self._current_volume = float(self._initial_volume.Value)
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def CreateClone(self):
        return macd_pattern_trader_advanced_multi_pattern_strategy()
