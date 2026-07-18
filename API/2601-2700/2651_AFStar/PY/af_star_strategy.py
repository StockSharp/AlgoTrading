import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class af_star_strategy(Strategy):
    """AFStar strategy: fast/slow EMA crossover scan with Williams %R channel breakout confirmation."""

    def __init__(self):
        super(af_star_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._start_fast = self.Param("StartFast", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Start Fast", "Lower bound for fast EMA period", "Indicator")
        self._end_fast = self.Param("EndFast", 3.5) \
            .SetGreaterThanZero() \
            .SetDisplay("End Fast", "Upper bound for fast EMA period", "Indicator")
        self._start_slow = self.Param("StartSlow", 8.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Start Slow", "Lower bound for slow EMA period", "Indicator")
        self._end_slow = self.Param("EndSlow", 9.0) \
            .SetGreaterThanZero() \
            .SetDisplay("End Slow", "Upper bound for slow EMA period", "Indicator")
        self._step_period = self.Param("StepPeriod", 0.2) \
            .SetGreaterThanZero() \
            .SetDisplay("Period Step", "Increment for scanning EMA periods", "Indicator")
        self._start_risk = self.Param("StartRisk", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Start Risk", "Lower bound for risk scan", "Williams %R")
        self._end_risk = self.Param("EndRisk", 2.8) \
            .SetGreaterThanZero() \
            .SetDisplay("End Risk", "Upper bound for risk scan", "Williams %R")
        self._step_risk = self.Param("StepRisk", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk Step", "Increment for risk parameter", "Williams %R")
        self._range_length = self.Param("RangeLength", 10) \
            .SetDisplay("Range Length", "Bars used to compute the average range filter", "Indicator")
        self._max_history = self.Param("MaxHistory", 512) \
            .SetDisplay("Max History", "Maximum candles stored for calculations", "General")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Delay in bars before executing a signal", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 1000) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in price steps", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 2000) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in price steps", "Risk")
        self._enable_buy_entries = self.Param("BuyEntriesEnabled", True) \
            .SetDisplay("Enable Buy Entries", "Allow long entries on buy signals", "Trading")
        self._enable_sell_entries = self.Param("SellEntriesEnabled", True) \
            .SetDisplay("Enable Sell Entries", "Allow short entries on sell signals", "Trading")
        self._enable_buy_exits = self.Param("BuyExitsEnabled", True) \
            .SetDisplay("Enable Buy Exits", "Allow closing longs on sell signals", "Trading")
        self._enable_sell_exits = self.Param("SellExitsEnabled", True) \
            .SetDisplay("Enable Sell Exits", "Allow closing shorts on buy signals", "Trading")

        self._candles_buf = []
        self._value2_history = []
        self._signal_queue = []
        self._last_wpr = -50.0
        self._prev_buy1 = False
        self._prev_sell1 = False
        self._prev_buy2 = False
        self._prev_sell2 = False
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StartFast(self):
        return float(self._start_fast.Value)
    @property
    def EndFast(self):
        return float(self._end_fast.Value)
    @property
    def StartSlow(self):
        return float(self._start_slow.Value)
    @property
    def EndSlow(self):
        return float(self._end_slow.Value)
    @property
    def StepPeriod(self):
        return float(self._step_period.Value)
    @property
    def StartRisk(self):
        return float(self._start_risk.Value)
    @property
    def EndRisk(self):
        return float(self._end_risk.Value)
    @property
    def StepRisk(self):
        return float(self._step_risk.Value)
    @property
    def RangeLength(self):
        return int(self._range_length.Value)
    @property
    def MaxHistory(self):
        return int(self._max_history.Value)
    @property
    def SignalBar(self):
        return int(self._signal_bar.Value)
    @property
    def StopLossPips(self):
        return int(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return int(self._take_profit_pips.Value)
    @property
    def BuyEntriesEnabled(self):
        return self._enable_buy_entries.Value
    @property
    def SellEntriesEnabled(self):
        return self._enable_sell_entries.Value
    @property
    def BuyExitsEnabled(self):
        return self._enable_buy_exits.Value
    @property
    def SellExitsEnabled(self):
        return self._enable_sell_exits.Value

    def OnStarted2(self, time):
        super(af_star_strategy, self).OnStarted2(time)

        self._candles_buf = []
        self._value2_history = []
        self._signal_queue = []
        self._last_wpr = -50.0
        self._prev_buy1 = False
        self._prev_sell1 = False
        self._prev_buy2 = False
        self._prev_sell2 = False
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)

        self._candles_buf.append((o, h, lo, c))
        self._value2_history.append(0.0)

        mx = self.MaxHistory
        if len(self._candles_buf) > mx:
            self._candles_buf.pop(0)
            if len(self._value2_history) > 0:
                self._value2_history.pop(0)

        self._apply_stops(candle)

        signal = self._compute_signal()
        if signal is not None:
            self._signal_queue.append(signal)
            while len(self._signal_queue) > self.SignalBar:
                active = self._signal_queue.pop(0)
                self._execute_signal(active, c)

    def _get_min_history(self):
        return 12 + 3 + self.SignalBar

    def _get_close(self, offset):
        return self._candles_buf[-(offset + 1)][3]

    def _get_open(self, offset):
        return self._candles_buf[-(offset + 1)][0]

    def _get_high(self, offset):
        return self._candles_buf[-(offset + 1)][1]

    def _get_low(self, offset):
        return self._candles_buf[-(offset + 1)][2]

    def _compute_avg_range(self):
        s = 0.0
        for i in range(self.RangeLength):
            s += abs(self._get_high(i) - self._get_low(i))
        return s / self.RangeLength

    def _find_mro1(self, rng):
        for offset in range(9):
            if abs(self._get_open(offset) - self._get_close(offset + 1)) >= rng * 2.0:
                return offset
        return -1

    def _find_mro2(self, rng):
        for offset in range(6):
            if abs(self._get_close(offset + 3) - self._get_close(offset)) >= rng * 4.6:
                return offset
        return -1

    def _get_williams_r(self, period):
        max_high = self._get_high(0)
        min_low = self._get_low(0)
        for i in range(1, min(period, len(self._candles_buf))):
            h = self._get_high(i)
            lo = self._get_low(i)
            if h > max_high:
                max_high = h
            if lo < min_low:
                min_low = lo
        close = self._get_close(0)
        rng = max_high - min_low
        if rng == 0:
            return self._last_wpr
        wpr = -(max_high - close) * 100.0 / rng
        self._last_wpr = wpr
        return wpr

    def _try_get_prev_value2(self, offset):
        idx = len(self._value2_history) - 1 - offset
        if idx >= 0:
            return True, self._value2_history[idx]
        return False, 0.0

    def _enum_range(self, start, end, step):
        if step <= 0:
            return
        vals = []
        if start <= end:
            v = start
            while v <= end + 0.0000001:
                vals.append(v)
                v += step
        else:
            v = start
            while v >= end - 0.0000001:
                vals.append(v)
                v -= step
        return vals

    def _compute_signal(self):
        if len(self._candles_buf) < self._get_min_history():
            return None

        buy1 = False
        sell1 = False

        for slow in self._enum_range(self.StartSlow, self.EndSlow, self.StepPeriod):
            for fast in self._enum_range(self.StartFast, self.EndFast, self.StepPeriod):
                slow_per = 2.0 / (slow + 1.0)
                fast_per = 2.0 / (fast + 1.0)

                slow_cur = self._get_close(0) * slow_per + self._get_close(1) * (1.0 - slow_per)
                slow_prev = self._get_close(1) * slow_per + self._get_close(2) * (1.0 - slow_per)
                fast_cur = self._get_close(0) * fast_per + self._get_close(1) * (1.0 - fast_per)
                fast_prev = self._get_close(1) * fast_per + self._get_close(2) * (1.0 - fast_per)

                if not buy1 and fast_prev < slow_prev and fast_cur > slow_cur:
                    buy1 = True
                    break
                if not sell1 and fast_prev > slow_prev and fast_cur < slow_cur:
                    sell1 = True
                    break

            if buy1 or sell1:
                break

        avg_range = self._compute_avg_range()
        mro1 = self._find_mro1(avg_range)
        mro2 = self._find_mro2(avg_range)
        value2 = 0.0
        has_buy2 = False
        has_sell2 = False

        for risk in self._enum_range(self.StartRisk, self.EndRisk, self.StepRisk):
            value10 = 3.0 + risk * 2.0
            x1 = 67.0 + risk
            x2 = 33.0 - risk

            value11 = value10
            if mro1 > -1:
                value11 = 3.0
            if mro2 > -1:
                value11 = 4.0

            period = max(1, int(value11))
            wpr = self._get_williams_r(period)
            value2 = 100.0 - abs(wpr)

            if not has_sell2 and value2 < x2:
                offset = 1
                while True:
                    ok, prev = self._try_get_prev_value2(offset)
                    if ok and prev >= x2 and prev <= x1:
                        offset += 1
                    else:
                        break
                ok2, prev_outside = self._try_get_prev_value2(offset)
                if ok2 and prev_outside > x1:
                    has_sell2 = True

            if not has_buy2 and value2 > x1:
                offset = 1
                while True:
                    ok, prev = self._try_get_prev_value2(offset)
                    if ok and prev >= x2 and prev <= x1:
                        offset += 1
                    else:
                        break
                ok2, prev_outside = self._try_get_prev_value2(offset)
                if ok2 and prev_outside < x2:
                    has_buy2 = True

            if has_buy2 or has_sell2:
                break

        buy_signal = (buy1 and has_buy2) or (buy1 and self._prev_buy2) or (self._prev_buy1 and has_buy2)
        sell_signal = (sell1 and has_sell2) or (sell1 and self._prev_sell2) or (self._prev_sell1 and has_sell2)

        if buy_signal and sell_signal:
            buy_signal = False
            sell_signal = False

        self._prev_buy1 = buy1
        self._prev_sell1 = sell1
        self._prev_buy2 = has_buy2
        self._prev_sell2 = has_sell2

        self._value2_history[-1] = value2

        return (buy_signal, sell_signal)

    def _execute_signal(self, signal, close):
        buy_arrow, sell_arrow = signal

        if buy_arrow:
            if self.SellExitsEnabled:
                self._exit_short()
            if self.BuyEntriesEnabled and self.Position == 0:
                self.BuyMarket()
                self._init_long_targets(close)

        if sell_arrow:
            if self.BuyExitsEnabled:
                self._exit_long()
            if self.SellEntriesEnabled and self.Position == 0:
                self.SellMarket()
                self._init_short_targets(close)

    def _apply_stops(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop is not None and lo <= self._long_stop:
                self._exit_long()
            elif self._long_take is not None and h >= self._long_take:
                self._exit_long()

        if self.Position < 0:
            if self._short_stop is not None and h >= self._short_stop:
                self._exit_short()
            elif self._short_take is not None and lo <= self._short_take:
                self._exit_short()

    def _exit_long(self):
        if self.Position > 0:
            self.SellMarket()
            self._long_stop = None
            self._long_take = None

    def _exit_short(self):
        if self.Position < 0:
            self.BuyMarket()
            self._short_stop = None
            self._short_take = None

    def _init_long_targets(self, entry):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._long_stop = entry - step * self.StopLossPips if self.StopLossPips > 0 else None
        self._long_take = entry + step * self.TakeProfitPips if self.TakeProfitPips > 0 else None

    def _init_short_targets(self, entry):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._short_stop = entry + step * self.StopLossPips if self.StopLossPips > 0 else None
        self._short_take = entry - step * self.TakeProfitPips if self.TakeProfitPips > 0 else None

    def OnReseted(self):
        super(af_star_strategy, self).OnReseted()
        self._candles_buf = []
        self._value2_history = []
        self._signal_queue = []
        self._last_wpr = -50.0
        self._prev_buy1 = False
        self._prev_sell1 = False
        self._prev_buy2 = False
        self._prev_sell2 = False
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def CreateClone(self):
        return af_star_strategy()
