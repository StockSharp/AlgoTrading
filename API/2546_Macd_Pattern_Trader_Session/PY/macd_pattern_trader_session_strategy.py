import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (MovingAverageConvergenceDivergence, MovingAverageConvergenceDivergenceSignal,
    ExponentialMovingAverage, SimpleMovingAverage)
from StockSharp.Algo.Strategies import Strategy


class macd_pattern_trader_session_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_session_strategy, self).__init__()

        self._min_partial_volume = self.Param("MinPartialVolume", 0.01)
        self._profit_threshold = self.Param("ProfitThreshold", 5.0)
        self._history_limit = self.Param("HistoryLimit", 1024)

        self._p1_enabled = self.Param("Pattern1Enabled", True)
        self._p1_sl_bars = self.Param("Pattern1StopLossBars", 22)
        self._p1_tp_bars = self.Param("Pattern1TakeProfitBars", 32)
        self._p1_offset = self.Param("Pattern1Offset", 40)
        self._p1_fast = self.Param("Pattern1FastEma", 24)
        self._p1_slow = self.Param("Pattern1SlowEma", 13)
        self._p1_max = self.Param("Pattern1MaxThreshold", 0.0095)
        self._p1_min = self.Param("Pattern1MinThreshold", -0.0045)

        self._p2_enabled = self.Param("Pattern2Enabled", True)
        self._p2_sl_bars = self.Param("Pattern2StopLossBars", 2)
        self._p2_tp_bars = self.Param("Pattern2TakeProfitBars", 2)
        self._p2_offset = self.Param("Pattern2Offset", 50)
        self._p2_fast = self.Param("Pattern2FastEma", 17)
        self._p2_slow = self.Param("Pattern2SlowEma", 7)
        self._p2_max = self.Param("Pattern2MaxThreshold", 0.0045)
        self._p2_min = self.Param("Pattern2MinThreshold", -0.0035)

        self._p3_enabled = self.Param("Pattern3Enabled", True)
        self._p3_sl_bars = self.Param("Pattern3StopLossBars", 8)
        self._p3_tp_bars = self.Param("Pattern3TakeProfitBars", 12)
        self._p3_offset = self.Param("Pattern3Offset", 2)
        self._p3_fast = self.Param("Pattern3FastEma", 32)
        self._p3_slow = self.Param("Pattern3SlowEma", 2)
        self._p3_max = self.Param("Pattern3MaxThreshold", 0.0015)
        self._p3_sec_max = self.Param("Pattern3SecondaryMax", 0.004)
        self._p3_min = self.Param("Pattern3MinThreshold", -0.005)
        self._p3_sec_min = self.Param("Pattern3SecondaryMin", -0.0005)

        self._p4_enabled = self.Param("Pattern4Enabled", True)
        self._p4_sl_bars = self.Param("Pattern4StopLossBars", 10)
        self._p4_tp_bars = self.Param("Pattern4TakeProfitBars", 32)
        self._p4_offset = self.Param("Pattern4Offset", 45)
        self._p4_fast = self.Param("Pattern4FastEma", 4)
        self._p4_slow = self.Param("Pattern4SlowEma", 9)
        self._p4_max = self.Param("Pattern4MaxThreshold", 0.0165)
        self._p4_sec_max = self.Param("Pattern4SecondaryMax", 0.0001)
        self._p4_min = self.Param("Pattern4MinThreshold", -0.0005)
        self._p4_sec_min = self.Param("Pattern4SecondaryMin", -0.0006)

        self._p5_enabled = self.Param("Pattern5Enabled", True)
        self._p5_sl_bars = self.Param("Pattern5StopLossBars", 8)
        self._p5_tp_bars = self.Param("Pattern5TakeProfitBars", 47)
        self._p5_offset = self.Param("Pattern5Offset", 45)
        self._p5_fast = self.Param("Pattern5FastEma", 6)
        self._p5_slow = self.Param("Pattern5SlowEma", 2)
        self._p5_pri_max = self.Param("Pattern5PrimaryMax", 0.0005)
        self._p5_max = self.Param("Pattern5MaxThreshold", 0.0015)
        self._p5_pri_min = self.Param("Pattern5PrimaryMin", -0.0005)
        self._p5_min = self.Param("Pattern5MinThreshold", -0.003)

        self._p6_enabled = self.Param("Pattern6Enabled", True)
        self._p6_sl_bars = self.Param("Pattern6StopLossBars", 26)
        self._p6_tp_bars = self.Param("Pattern6TakeProfitBars", 42)
        self._p6_offset = self.Param("Pattern6Offset", 20)
        self._p6_fast = self.Param("Pattern6FastEma", 4)
        self._p6_slow = self.Param("Pattern6SlowEma", 8)
        self._p6_max = self.Param("Pattern6MaxThreshold", 0.0005)
        self._p6_min = self.Param("Pattern6MinThreshold", -0.001)
        self._p6_max_bars = self.Param("Pattern6MaxBars", 5)
        self._p6_min_bars = self.Param("Pattern6MinBars", 5)
        self._p6_count_bars = self.Param("Pattern6CountBars", 4)

        self._ema1_period = self.Param("Ema1Period", 7)
        self._ema2_period = self.Param("Ema2Period", 21)
        self._sma_period = self.Param("SmaPeriod", 98)
        self._ema3_period = self.Param("Ema3Period", 365)

        self._lot_size = self.Param("LotSize", 0.1)
        self._use_time_filter = self.Param("UseTimeFilter", False)
        self._session_start = self.Param("SessionStart", 7)
        self._session_end = self.Param("SessionEnd", 17)
        self._use_martingale = self.Param("UseMartingale", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._history = []
        self._point_size = 0.0001
        self._current_volume = 0.1
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._realized_pnl = 0.0
        self._entry_direction = 0
        self._current_stop = None
        self._current_take = None
        self._long_partial_stage = 0
        self._short_partial_stage = 0
        self._bars_bup = 0
        self._p6_short_counter = 0
        self._p6_short_blocked = False
        self._p6_long_counter = 0
        self._p6_long_blocked = False
        self._p6_short_ready = False
        self._p6_long_ready = False

        self._macd_prevs = {}

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LotSize(self):
        return self._lot_size.Value

    @LotSize.setter
    def LotSize(self, value):
        self._lot_size.Value = value

    @property
    def UseMartingale(self):
        return self._use_martingale.Value

    @UseMartingale.setter
    def UseMartingale(self, value):
        self._use_martingale.Value = value

    def OnStarted(self, time):
        super(macd_pattern_trader_session_strategy, self).OnStarted(time)

        self._point_size = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        if self._point_size <= 0.0:
            self._point_size = 0.0001

        self._current_volume = float(self.LotSize)
        self._history = []
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._realized_pnl = 0.0
        self._entry_direction = 0
        self._current_stop = None
        self._current_take = None
        self._long_partial_stage = 0
        self._short_partial_stage = 0
        self._bars_bup = 0
        self._p6_short_counter = 0
        self._p6_short_blocked = False
        self._p6_long_counter = 0
        self._p6_long_blocked = False
        self._p6_short_ready = False
        self._p6_long_ready = False

        self._macds = []
        for i in range(6):
            fast = int(getattr(self, '_p%d_fast' % (i+1)).Value)
            slow = int(getattr(self, '_p%d_slow' % (i+1)).Value)
            macd = MovingAverageConvergenceDivergenceSignal()
            macd.Macd.ShortMa.Length = fast
            macd.Macd.LongMa.Length = slow
            macd.SignalMa.Length = 1
            self._macds.append(macd)

        self._ema1 = ExponentialMovingAverage()
        self._ema1.Length = int(self._ema1_period.Value)
        self._ema2 = ExponentialMovingAverage()
        self._ema2.Length = int(self._ema2_period.Value)
        self._sma1 = SimpleMovingAverage()
        self._sma1.Length = int(self._sma_period.Value)
        self._ema3 = ExponentialMovingAverage()
        self._ema3.Length = int(self._ema3_period.Value)

        self._macd_prevs = {}
        for i in range(6):
            self._macd_prevs[i] = [None, None, None]

        self._ema1_prev = None
        self._ema2_prev = None
        self._sma_prev = None
        self._ema3_prev = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandleRaw).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandleRaw(self, candle):
        if candle.State != CandleStates.Finished:
            return

        macd_values = []
        for m in self._macds:
            val = m.Process(candle)
            macd_values.append(val)

        ema1_val = self._ema1.Process(candle)
        ema2_val = self._ema2.Process(candle)
        sma1_val = self._sma1.Process(candle)
        ema3_val = self._ema3.Process(candle)

        self._process_candle(candle, macd_values, ema1_val, ema2_val, sma1_val, ema3_val)

    def _process_candle(self, candle, macd_values, ema1_val, ema2_val, sma1_val, ema3_val):
        self._history.append(candle)
        limit = int(self._history_limit.Value)
        if len(self._history) > limit:
            self._history.pop(0)

        macd_currents = []
        for i, val in enumerate(macd_values):
            macd_line = val.Macd if hasattr(val, 'Macd') else None
            if macd_line is not None:
                macd_currents.append(float(macd_line))
            else:
                macd_currents.append(None)

        ema1_cur = float(ema1_val) if ema1_val is not None else 0.0
        ema2_cur = float(ema2_val) if ema2_val is not None else 0.0
        sma_cur = float(sma1_val) if sma1_val is not None else 0.0
        ema3_cur = float(ema3_val) if ema3_val is not None else 0.0

        all_ready = True
        series = []
        for i in range(6):
            mc = macd_currents[i]
            if mc is None:
                all_ready = False
                break
            prevs = self._macd_prevs[i]
            if prevs[0] is None or prevs[1] is None or prevs[2] is None:
                prevs[2] = prevs[1]
                prevs[1] = prevs[0]
                prevs[0] = mc
                all_ready = False
                continue
            curr = prevs[0]
            last = prevs[1]
            last3 = prevs[2]
            prevs[2] = prevs[1]
            prevs[1] = prevs[0]
            prevs[0] = mc
            series.append((curr, last, last3))

        if not all_ready or len(series) < 6:
            self._update_prev_indicators(ema1_cur, ema2_cur, sma_cur, ema3_cur)
            return

        for m in self._macds:
            if not m.IsFormed:
                self._update_prev_indicators(ema1_cur, ema2_cur, sma_cur, ema3_cur)
                return

        if not self._ema1.IsFormed or not self._ema2.IsFormed or not self._sma1.IsFormed or not self._ema3.IsFormed:
            self._update_prev_indicators(ema1_cur, ema2_cur, sma_cur, ema3_cur)
            return

        if self._ema1_prev is None or self._ema2_prev is None or self._sma_prev is None or self._ema3_prev is None:
            self._update_prev_indicators(ema1_cur, ema2_cur, sma_cur, ema3_cur)
            return

        self._check_risk(candle)

        use_tf = bool(self._use_time_filter.Value)
        in_session = not use_tf or self._is_in_session(candle)

        if in_session:
            self._process_pattern6(candle, series[5][0], series[5][1], series[5][2])
            self._process_pattern5(candle, series[4][0], series[4][1], series[4][2])
            self._process_pattern4(candle, series[3][0], series[3][1], series[3][2])
            self._process_pattern3(candle, series[2][0], series[2][1], series[2][2])
            self._process_pattern2(candle, series[1][0], series[1][1], series[1][2])
            self._process_pattern1(candle, series[0][0], series[0][1], series[0][2])

        if in_session:
            self._manage_position(candle, self._ema1_prev, self._ema2_prev, self._sma_prev, self._ema3_prev)

        self._update_prev_indicators(ema1_cur, ema2_cur, sma_cur, ema3_cur)

    def _process_pattern1(self, candle, curr, last, last3):
        if not bool(self._p1_enabled.Value):
            return
        p1_max = float(self._p1_max.Value)
        p1_min = float(self._p1_min.Value)

        if curr > p1_max and curr < last and last > last3 and curr > 0.0 and last3 < p1_max and self.Position >= 0:
            stop = self._calc_stop(False, int(self._p1_sl_bars.Value), int(self._p1_offset.Value))
            take = self._calc_take(False, int(self._p1_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_short(candle, stop, take)
                self._short_partial_stage = 0

        if curr < p1_min and curr > last and last < last3 and curr < 0.0 and last3 > p1_min and self.Position <= 0:
            stop = self._calc_stop(True, int(self._p1_sl_bars.Value), int(self._p1_offset.Value))
            take = self._calc_take(True, int(self._p1_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_long(candle, stop, take)
                self._long_partial_stage = 0

    def _process_pattern2(self, candle, curr, last, last3):
        if not bool(self._p2_enabled.Value):
            return
        p2_max = float(self._p2_max.Value)
        p2_min = float(self._p2_min.Value)

        if curr > 0.0 and curr > last and last < last3 and curr > p2_min and curr < 0.0 and self.Position >= 0:
            stop = self._calc_stop(False, int(self._p2_sl_bars.Value), int(self._p2_offset.Value))
            take = self._calc_take(False, int(self._p2_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_short(candle, stop, take)
                self._short_partial_stage = 0

        if curr < 0.0 and curr < last and last > last3 and curr < p2_max and curr > 0.0 and self.Position <= 0:
            stop = self._calc_stop(True, int(self._p2_sl_bars.Value), int(self._p2_offset.Value))
            take = self._calc_take(True, int(self._p2_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_long(candle, stop, take)
                self._long_partial_stage = 0

    def _process_pattern3(self, candle, curr, last, last3):
        if not bool(self._p3_enabled.Value):
            return
        sec_max = float(self._p3_sec_max.Value)
        pri_max = float(self._p3_max.Value)
        sec_min = float(self._p3_sec_min.Value)
        pri_min = float(self._p3_min.Value)

        if curr > sec_max:
            self._bars_bup += 1

        if curr < pri_max and curr < last and last > last3 and last > pri_max and last > sec_max and self.Position >= 0:
            stop = self._calc_stop(False, int(self._p3_sl_bars.Value), int(self._p3_offset.Value))
            take = self._calc_take(False, int(self._p3_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_short(candle, stop, take)
                self._short_partial_stage = 0
                self._bars_bup = 0

        if curr > pri_min and curr > last and last < last3 and last < pri_min and last < sec_min and self.Position <= 0:
            stop = self._calc_stop(True, int(self._p3_sl_bars.Value), int(self._p3_offset.Value))
            take = self._calc_take(True, int(self._p3_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_long(candle, stop, take)
                self._long_partial_stage = 0

    def _process_pattern4(self, candle, curr, last, last3):
        if not bool(self._p4_enabled.Value):
            return
        p4_max = float(self._p4_max.Value)
        p4_sec_max = float(self._p4_sec_max.Value)
        p4_min = float(self._p4_min.Value)
        p4_sec_min = float(self._p4_sec_min.Value)

        if curr > p4_max and curr < last and last > last3 and last < p4_sec_max and self.Position >= 0:
            stop = self._calc_stop(False, int(self._p4_sl_bars.Value), int(self._p4_offset.Value))
            take = self._calc_take(False, int(self._p4_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_short(candle, stop, take)
                self._short_partial_stage = 0

        if curr < p4_min and curr > last and last < last3 and last > p4_sec_min and self.Position <= 0:
            stop = self._calc_stop(True, int(self._p4_sl_bars.Value), int(self._p4_offset.Value))
            take = self._calc_take(True, int(self._p4_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_long(candle, stop, take)
                self._long_partial_stage = 0

    def _process_pattern5(self, candle, curr, last, last3):
        if not bool(self._p5_enabled.Value):
            return
        pri_min = float(self._p5_pri_min.Value)
        min_th = float(self._p5_min.Value)
        pri_max = float(self._p5_pri_max.Value)
        max_th = float(self._p5_max.Value)

        if curr < pri_min and curr > min_th and curr < last and last > last3 and last > min_th and self.Position >= 0:
            stop = self._calc_stop(False, int(self._p5_sl_bars.Value), int(self._p5_offset.Value))
            take = self._calc_take(False, int(self._p5_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_short(candle, stop, take)
                self._short_partial_stage = 0

        if curr > pri_max and curr < max_th and curr > last and last < last3 and last < max_th and self.Position <= 0:
            stop = self._calc_stop(True, int(self._p5_sl_bars.Value), int(self._p5_offset.Value))
            take = self._calc_take(True, int(self._p5_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_long(candle, stop, take)
                self._long_partial_stage = 0

    def _process_pattern6(self, candle, curr, last, last3):
        if not bool(self._p6_enabled.Value):
            return
        p6_max = float(self._p6_max.Value)
        p6_min = float(self._p6_min.Value)
        max_bars = int(self._p6_max_bars.Value)
        min_bars = int(self._p6_min_bars.Value)
        count_bars = int(self._p6_count_bars.Value)

        if curr < p6_max:
            self._p6_short_blocked = False
        if curr > p6_max and self._p6_short_counter <= max_bars and not self._p6_short_blocked:
            self._p6_short_counter += 1
        if self._p6_short_counter > max_bars:
            self._p6_short_counter = 0
            self._p6_short_blocked = True
        if self._p6_short_counter < min_bars and curr < p6_max:
            self._p6_short_counter = 0
        if curr < p6_max and self._p6_short_counter > count_bars:
            self._p6_short_ready = True

        if self._p6_short_ready and self.Position >= 0:
            stop = self._calc_stop(False, int(self._p6_sl_bars.Value), int(self._p6_offset.Value))
            take = self._calc_take(False, int(self._p6_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_short(candle, stop, take)
                self._p6_short_ready = False
                self._p6_short_counter = 0
                self._p6_short_blocked = False
                self._short_partial_stage = 0

        if curr > p6_min:
            self._p6_long_blocked = False
        if curr < p6_min and self._p6_long_counter <= max_bars and not self._p6_long_blocked:
            self._p6_long_counter += 1
        if self._p6_long_counter > max_bars:
            self._p6_long_counter = 0
            self._p6_long_blocked = True
        if self._p6_long_counter < min_bars and curr > p6_min:
            self._p6_long_counter = 0
        if curr > p6_min and self._p6_long_counter > count_bars:
            self._p6_long_ready = True

        if self._p6_long_ready and self.Position <= 0:
            stop = self._calc_stop(True, int(self._p6_sl_bars.Value), int(self._p6_offset.Value))
            take = self._calc_take(True, int(self._p6_tp_bars.Value))
            if stop is not None and take is not None:
                self._enter_long(candle, stop, take)
                self._p6_long_ready = False
                self._p6_long_counter = 0
                self._p6_long_blocked = False
                self._long_partial_stage = 0

    def _enter_long(self, candle, stop_price, take_price):
        self.BuyMarket()
        self._entry_direction = 1
        self._entry_price = float(candle.ClosePrice)
        self._open_volume = self._current_volume
        self._realized_pnl = 0.0
        self._current_stop = stop_price
        self._current_take = take_price
        self._long_partial_stage = 0
        self._short_partial_stage = 0

    def _enter_short(self, candle, stop_price, take_price):
        self.SellMarket()
        self._entry_direction = -1
        self._entry_price = float(candle.ClosePrice)
        self._open_volume = self._current_volume
        self._realized_pnl = 0.0
        self._current_stop = stop_price
        self._current_take = take_price
        self._long_partial_stage = 0
        self._short_partial_stage = 0
        self._bars_bup = 0

    def _manage_position(self, candle, ema1_prev, ema2_prev, sma_prev, ema3_prev):
        if self._entry_direction == 0 or self._open_volume <= 0.0:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        profit_th = float(self._profit_threshold.Value)

        if self._entry_direction > 0:
            profit = (close - self._entry_price) * self._open_volume
            if profit > profit_th and close > ema2_prev and self._long_partial_stage == 0:
                self.SellMarket()
                self._long_partial_stage = 1
            elif profit > profit_th and high > (sma_prev + ema3_prev) / 2.0 and self._long_partial_stage == 1:
                self.SellMarket()
                self._long_partial_stage = 2
        elif self._entry_direction < 0:
            profit = (self._entry_price - close) * self._open_volume
            if profit > profit_th and close < ema2_prev and self._short_partial_stage == 0:
                self.BuyMarket()
                self._short_partial_stage = 1
            elif profit > profit_th and low < (sma_prev + ema3_prev) / 2.0 and self._short_partial_stage == 1:
                self.BuyMarket()
                self._short_partial_stage = 2

    def _check_risk(self, candle):
        if self._entry_direction == 0 or self._open_volume <= 0.0:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._entry_direction > 0:
            if self._current_stop is not None and low <= self._current_stop:
                self.SellMarket()
                self._complete_trade(self._current_stop)
                return
            if self._current_take is not None and high >= self._current_take:
                self.SellMarket()
                self._complete_trade(self._current_take)
                return
        elif self._entry_direction < 0:
            if self._current_stop is not None and high >= self._current_stop:
                self.BuyMarket()
                self._complete_trade(self._current_stop)
                return
            if self._current_take is not None and low <= self._current_take:
                self.BuyMarket()
                self._complete_trade(self._current_take)
                return

    def _complete_trade(self, exit_price):
        if self._entry_direction != 0 and self._open_volume > 0.0:
            diff = (exit_price - self._entry_price) * self._entry_direction
            self._realized_pnl = diff * self._open_volume

        if self.UseMartingale and self._realized_pnl < 0.0:
            self._current_volume *= 2.0
        else:
            self._current_volume = float(self.LotSize)

        self._entry_direction = 0
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._realized_pnl = 0.0
        self._current_stop = None
        self._current_take = None
        self._long_partial_stage = 0
        self._short_partial_stage = 0

    def _calc_stop(self, is_long, stop_bars, offset_points):
        if stop_bars <= 0:
            return None
        offset = offset_points * self._point_size
        if is_long:
            lowest = self._get_lowest_low(stop_bars)
            return lowest - offset if lowest is not None else None
        else:
            highest = self._get_highest_high(stop_bars)
            return highest + offset if highest is not None else None

    def _calc_take(self, is_long, take_bars):
        if take_bars <= 0:
            return None
        return self._get_chunk_extreme(is_long, take_bars, 0)

    def _get_chunk_extreme(self, is_long, length, offset):
        start_idx = len(self._history) - 1 - offset
        end_idx = start_idx - (length - 1)
        if start_idx < 0 or end_idx < 0:
            return None

        if is_long:
            extreme = -1e18
        else:
            extreme = 1e18

        for i in range(start_idx, end_idx - 1, -1):
            c = self._history[i]
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

        next_offset = offset + length
        next_extreme = self._get_chunk_extreme(is_long, length, next_offset)
        if next_extreme is not None:
            if is_long and next_extreme > extreme:
                return next_extreme
            elif not is_long and next_extreme < extreme:
                return next_extreme

        return extreme

    def _get_highest_high(self, bars):
        if bars <= 0 or len(self._history) == 0:
            return None
        result = None
        end = max(0, len(self._history) - bars)
        for i in range(len(self._history) - 1, end - 1, -1):
            v = float(self._history[i].HighPrice)
            if result is None or v > result:
                result = v
        return result

    def _get_lowest_low(self, bars):
        if bars <= 0 or len(self._history) == 0:
            return None
        result = None
        end = max(0, len(self._history) - bars)
        for i in range(len(self._history) - 1, end - 1, -1):
            v = float(self._history[i].LowPrice)
            if result is None or v < result:
                result = v
        return result

    def _update_prev_indicators(self, ema1, ema2, sma, ema3):
        self._ema1_prev = ema1
        self._ema2_prev = ema2
        self._sma_prev = sma
        self._ema3_prev = ema3

    def _is_in_session(self, candle):
        hour = candle.OpenTime.TimeOfDay.Hours
        start = int(self._session_start.Value)
        end = int(self._session_end.Value)
        if start <= end:
            return hour >= start and hour <= end
        return hour >= start or hour <= end

    def OnReseted(self):
        super(macd_pattern_trader_session_strategy, self).OnReseted()
        self._history = []
        self._point_size = 0.0001
        self._current_volume = 0.1
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._realized_pnl = 0.0
        self._entry_direction = 0
        self._current_stop = None
        self._current_take = None
        self._long_partial_stage = 0
        self._short_partial_stage = 0
        self._bars_bup = 0
        self._p6_short_counter = 0
        self._p6_short_blocked = False
        self._p6_long_counter = 0
        self._p6_long_blocked = False
        self._p6_short_ready = False
        self._p6_long_ready = False
        self._macd_prevs = {}
        self._ema1_prev = None
        self._ema2_prev = None
        self._sma_prev = None
        self._ema3_prev = None

    def CreateClone(self):
        return macd_pattern_trader_session_strategy()
