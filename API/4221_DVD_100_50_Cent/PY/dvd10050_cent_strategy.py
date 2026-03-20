import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class dvd10050_cent_strategy(Strategy):
    def __init__(self):
        super(dvd10050_cent_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Monitoring timeframe.", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 210.0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance.", "Orders")
        self._take_profit_pips = self.Param("TakeProfitPips", 18.0) \
            .SetDisplay("Take Profit (pips)", "Profit target distance.", "Orders")
        self._point_offset = self.Param("PointFromLevelGoPips", 50.0) \
            .SetDisplay("Base Offset (0.1 pips)", "Offset for 100 level grid.", "Filters")
        self._rise_filter = self.Param("RiseFilterPips", 700.0) \
            .SetDisplay("Rise Filter (0.1 pips)", "Hourly spike confirmation.", "Filters")
        self._high_level = self.Param("HighLevelPips", 600.0) \
            .SetDisplay("High Level (0.1 pips)", "One-minute spike rejection.", "Filters")
        self._low_level = self.Param("LowLevelPips", 250.0) \
            .SetDisplay("Low Level (0.1 pips)", "Half-hour consolidation ceiling.", "Filters")
        self._low_level2 = self.Param("LowLevel2Pips", 450.0) \
            .SetDisplay("Low Level 2 (0.1 pips)", "Hourly breakout confirmation.", "Filters")
        self._m1_hist_len = self.Param("M1HistoryLength", 64) \
            .SetDisplay("M1 History Length", "Number of M1 candles retained.", "History")
        self._h1_hist_len = self.Param("H1HistoryLength", 16) \
            .SetDisplay("H1 History Length", "Number of H1 candles retained.", "History")
        self._m30_hist_len = self.Param("M30HistoryLength", 16) \
            .SetDisplay("M30 History Length", "Number of M30 candles retained.", "History")
        self._m1_history = []
        self._m30_history = []
        self._h1_finished = []
        self._h1_fast = None
        self._h1_slow = None
        self._ravi_h1 = None
        self._pip_size = 0.0001
        self._point_value = 0.00001
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def PointFromLevelGoPips(self):
        return float(self._point_offset.Value)
    @property
    def RiseFilterPips(self):
        return float(self._rise_filter.Value)
    @property
    def HighLevelPips(self):
        return float(self._high_level.Value)
    @property
    def LowLevelPips(self):
        return float(self._low_level.Value)
    @property
    def LowLevel2Pips(self):
        return float(self._low_level2.Value)
    @property
    def M1HistoryLength(self):
        return self._m1_hist_len.Value
    @property
    def H1HistoryLength(self):
        return self._h1_hist_len.Value
    @property
    def M30HistoryLength(self):
        return self._m30_hist_len.Value

    def OnStarted(self, time):
        super(dvd10050_cent_strategy, self).OnStarted(time)
        self._m1_history = []
        self._m30_history = []
        self._h1_finished = []
        self._ravi_h1 = None
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        sec = self.Security
        step = 0.0001
        if sec is not None:
            ps = sec.PriceStep
            if ps is not None and float(ps) > 0:
                step = float(ps)
        self._pip_size = step
        self._point_value = step / 10.0
        self._h1_fast = SimpleMovingAverage()
        self._h1_fast.Length = 2
        self._h1_slow = SimpleMovingAverage()
        self._h1_slow.Length = 24
        sub_m1 = self.SubscribeCandles(self.CandleType)
        sub_m1.Bind(self.ProcessM1).Start()
        sub_m30 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        sub_m30.Bind(self.ProcessM30).Start()
        sub_h1 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(1)))
        sub_h1.Bind(self.ProcessH1).Start()

    def ProcessM1(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._m1_history.append((close, high, low))
        while len(self._m1_history) > self.M1HistoryLength:
            self._m1_history.pop(0)
        # manage position
        if self.Position > 0:
            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket()
                self._reset_levels()
                return
            if self._long_take is not None and high >= self._long_take:
                self.SellMarket()
                self._reset_levels()
                return
        elif self.Position < 0:
            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket()
                self._reset_levels()
                return
            if self._short_take is not None and low <= self._short_take:
                self.BuyMarket()
                self._reset_levels()
                return
        if self._ravi_h1 is None:
            return
        if self.Position != 0:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # try buy
        buy_score = self._calc_buy_score(close, high, low)
        if buy_score is not None and buy_score >= 0:
            entry = close
            sl = entry - self.StopLossPips * self._pip_size
            tp = entry + self.TakeProfitPips * self._pip_size
            self._entry_price = entry
            self._long_stop = sl
            self._long_take = tp
            self._short_stop = None
            self._short_take = None
            self.BuyMarket()
            return
        # try sell
        sell_score = self._calc_sell_score(close, high, low)
        if sell_score is not None and sell_score >= 0:
            entry = close
            sl = entry + self.StopLossPips * self._pip_size
            tp = entry - self.TakeProfitPips * self._pip_size
            self._entry_price = entry
            self._short_stop = sl
            self._short_take = tp
            self._long_stop = None
            self._long_take = None
            self.SellMarket()

    def ProcessM30(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._m30_history.append((high, low))
        while len(self._m30_history) > self.M30HistoryLength:
            self._m30_history.pop(0)

    def ProcessH1(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        op = float(candle.OpenPrice)
        self._h1_finished.append((high, low, op))
        while len(self._h1_finished) > self.H1HistoryLength:
            self._h1_finished.pop(0)
        from StockSharp.Algo.Indicators import DecimalIndicatorValue
        iv = DecimalIndicatorValue(self._h1_fast, op)
        iv.IsFinal = True
        self._h1_fast.Process(iv)
        iv2 = DecimalIndicatorValue(self._h1_slow, op)
        iv2.IsFinal = True
        self._h1_slow.Process(iv2)
        if not self._h1_fast.IsFormed or not self._h1_slow.IsFormed:
            return
        slow_v = float(self._h1_slow.GetCurrentValue())
        if slow_v == 0:
            return
        fast_v = float(self._h1_fast.GetCurrentValue())
        self._ravi_h1 = 100.0 * (fast_v - slow_v) / slow_v

    def _get_m1(self, shift):
        idx = len(self._m1_history) - 1 - shift
        if 0 <= idx < len(self._m1_history):
            return self._m1_history[idx]
        return None

    def _get_h1_high(self, shift):
        idx = len(self._h1_finished) - 1 - shift
        if 0 <= idx < len(self._h1_finished):
            return self._h1_finished[idx][0]
        return None

    def _get_h1_low(self, shift):
        idx = len(self._h1_finished) - 1 - shift
        if 0 <= idx < len(self._h1_finished):
            return self._h1_finished[idx][1]
        return None

    def _get_m30(self, shift):
        idx = len(self._m30_history) - 1 - shift
        if 0 <= idx < len(self._m30_history):
            return self._m30_history[idx]
        return None

    def _check_m1_high_above(self, threshold, count):
        for i in range(count):
            m = self._get_m1(i)
            if m is None:
                break
            if m[1] > threshold:
                return True
        return False

    def _check_m1_low_below(self, threshold, count):
        for i in range(count):
            m = self._get_m1(i)
            if m is None:
                break
            if m[2] < threshold:
                return True
        return False

    def _check_h1_break_above(self, threshold):
        for i in range(15):
            h = self._get_h1_high(i)
            if h is None:
                break
            if h > threshold:
                return True
        return False

    def _check_h1_break_below(self, threshold):
        for i in range(15):
            lo = self._get_h1_low(i)
            if lo is None:
                break
            if lo < threshold:
                return True
        return False

    def _check_m30_compression_above(self, threshold):
        for i in range(8):
            m = self._get_m30(i)
            if m is None:
                return False
            if m[0] >= threshold:
                return False
        return True

    def _check_m30_compression_below(self, threshold):
        for i in range(8):
            m = self._get_m30(i)
            if m is None:
                return False
            if m[1] <= threshold:
                return False
        return True

    def _calc_buy_score(self, close, high, low):
        if self._ravi_h1 is None:
            return None
        prev_m1 = self._get_m1(1)
        h1_low0 = self._get_h1_low(0)
        h1_low1 = self._get_h1_low(1)
        h1_low2 = self._get_h1_low(2)
        h1_high1 = self._get_h1_high(1)
        h1_high2 = self._get_h1_high(2)
        if prev_m1 is None or h1_low0 is None or h1_low1 is None or h1_low2 is None:
            return None
        if h1_high1 is None or h1_high2 is None:
            return None
        pv = self._point_value
        level100 = round(close, 2) + self.PointFromLevelGoPips * pv
        rise_thr = level100 + self.RiseFilterPips * pv
        base_low = level100 - self.PointFromLevelGoPips * pv
        tol = 30.0 * pv
        score = 0.0
        if self._ravi_h1 < 0:
            score += 10.0
        if h1_high1 > rise_thr or h1_high2 > rise_thr:
            score += 7.0
        if close < level100 and prev_m1[0] > level100 and h1_low0 > base_low + tol and h1_low1 > base_low + tol and h1_low2 > base_low:
            score += 45.0
        if self._check_m1_high_above(level100 + self.HighLevelPips * pv, 12):
            score -= 50.0
        if not self._check_h1_break_above(level100 + self.LowLevel2Pips * pv):
            score -= 50.0
        if self._check_m30_compression_above(level100 + self.LowLevelPips * pv):
            score -= 50.0
        return score

    def _calc_sell_score(self, close, high, low):
        if self._ravi_h1 is None:
            return None
        prev_m1 = self._get_m1(1)
        h1_high0 = self._get_h1_high(0)
        h1_high1 = self._get_h1_high(1)
        h1_high2 = self._get_h1_high(2)
        h1_low1 = self._get_h1_low(1)
        h1_low2 = self._get_h1_low(2)
        if prev_m1 is None or h1_high0 is None or h1_high1 is None or h1_high2 is None:
            return None
        if h1_low1 is None or h1_low2 is None:
            return None
        pv = self._point_value
        level100 = round(close, 2) - self.PointFromLevelGoPips * pv
        fall_thr = level100 - self.RiseFilterPips * pv
        base_high = level100 + self.PointFromLevelGoPips * pv
        tol = 30.0 * pv
        score = 0.0
        if self._ravi_h1 > 0:
            score += 10.0
        if h1_low1 < fall_thr or h1_low2 < fall_thr:
            score += 7.0
        if close > level100 and prev_m1[0] < level100 and h1_high0 < base_high - tol and h1_high1 < base_high - tol and h1_high2 < base_high:
            score += 45.0
        if self._check_m1_low_below(level100 - self.HighLevelPips * pv, 12):
            score -= 50.0
        if not self._check_h1_break_below(level100 - self.LowLevel2Pips * pv):
            score -= 50.0
        if self._check_m30_compression_below(level100 - self.LowLevelPips * pv):
            score -= 50.0
        return score

    def _reset_levels(self):
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._entry_price = 0.0

    def OnReseted(self):
        super(dvd10050_cent_strategy, self).OnReseted()
        self._m1_history = []
        self._m30_history = []
        self._h1_finished = []
        self._ravi_h1 = None
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def CreateClone(self):
        return dvd10050_cent_strategy()
