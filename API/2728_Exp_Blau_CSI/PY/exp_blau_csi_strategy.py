import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage,
    WeightedMovingAverage, JurikMovingAverage, DecimalIndicatorValue
)
from StockSharp.Algo.Strategies import Strategy


class exp_blau_csi_strategy(Strategy):
    def __init__(self):
        super(exp_blau_csi_strategy, self).__init__()

        self._entry_mode = self.Param("EntryMode", 0)
        self._smooth_method = self.Param("SmoothingMethod", 1)
        self._momentum_length = self.Param("MomentumLength", 1)
        self._first_smooth_length = self.Param("FirstSmoothingLength", 10)
        self._second_smooth_length = self.Param("SecondSmoothingLength", 3)
        self._third_smooth_length = self.Param("ThirdSmoothingLength", 2)
        self._smoothing_phase = self.Param("SmoothingPhase", 15)
        self._first_price = self.Param("FirstPrice", 1)
        self._second_price = self.Param("SecondPrice", 2)
        self._signal_bar = self.Param("SignalBar", 1)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._allow_long_entries = self.Param("AllowLongEntries", True)
        self._allow_short_entries = self.Param("AllowShortEntries", True)
        self._allow_long_exits = self.Param("AllowLongExits", True)
        self._allow_short_exits = self.Param("AllowShortExits", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._indicator_values = []
        self._stop_price = None
        self._take_price = None
        self._csi_calc = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EntryMode(self):
        return self._entry_mode.Value

    @property
    def SmoothingMethod(self):
        return self._smooth_method.Value

    @property
    def MomentumLength(self):
        return self._momentum_length.Value

    @property
    def FirstSmoothingLength(self):
        return self._first_smooth_length.Value

    @property
    def SecondSmoothingLength(self):
        return self._second_smooth_length.Value

    @property
    def ThirdSmoothingLength(self):
        return self._third_smooth_length.Value

    @property
    def SmoothingPhase(self):
        return self._smoothing_phase.Value

    @property
    def FirstPrice(self):
        return self._first_price.Value

    @property
    def SecondPrice(self):
        return self._second_price.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def AllowLongEntries(self):
        return self._allow_long_entries.Value

    @property
    def AllowShortEntries(self):
        return self._allow_short_entries.Value

    @property
    def AllowLongExits(self):
        return self._allow_long_exits.Value

    @property
    def AllowShortExits(self):
        return self._allow_short_exits.Value

    def OnStarted(self, time):
        super(exp_blau_csi_strategy, self).OnStarted(time)

        self._indicator_values = []
        self._stop_price = None
        self._take_price = None

        self._csi_calc = _BlauCsiCalc(
            self.SmoothingMethod, self.MomentumLength,
            self.FirstSmoothingLength, self.SecondSmoothingLength, self.ThirdSmoothingLength,
            self.SmoothingPhase, self.FirstPrice, self.SecondPrice
        )

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        val = self._csi_calc.process(candle)

        if self._handle_stops(candle):
            return

        self._store_value(val)

        open_long, open_short, close_long, close_short = self._evaluate_signals()

        if close_long and self.AllowLongExits and self.Position > 0:
            self.SellMarket()
            self._reset_targets()

        if close_short and self.AllowShortExits and self.Position < 0:
            self.BuyMarket()
            self._reset_targets()

        if open_long and self.AllowLongEntries and self.Position <= 0:
            self.BuyMarket()
            self._set_targets(float(candle.ClosePrice), True)
        elif open_short and self.AllowShortEntries and self.Position >= 0:
            self.SellMarket()
            self._set_targets(float(candle.ClosePrice), False)

    def _evaluate_signals(self):
        required = 3 if self.EntryMode == 1 else 2
        count = len(self._indicator_values)

        if self.SignalBar < 0:
            return False, False, False, False

        signal_index = count - 1 - self.SignalBar
        if signal_index < required - 1:
            return False, False, False, False

        open_long = False
        open_short = False
        close_long = False
        close_short = False

        if self.EntryMode == 0:
            current = self._indicator_values[signal_index]
            previous = self._indicator_values[signal_index - 1]

            if previous > 0:
                if current <= 0:
                    open_long = True
                close_short = True

            if previous < 0:
                if current >= 0:
                    open_short = True
                close_long = True
        else:
            current = self._indicator_values[signal_index]
            previous = self._indicator_values[signal_index - 1]
            older = self._indicator_values[signal_index - 2]

            if previous < older:
                if current >= previous:
                    open_long = True
                close_short = True

            if previous > older:
                if current <= previous:
                    open_short = True
                close_long = True

        return open_long, open_short, close_long, close_short

    def _handle_stops(self, candle):
        triggered = False

        if self.Position > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                triggered = True
            elif self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                triggered = True
        elif self.Position < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                triggered = True
            elif self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                triggered = True

        if triggered:
            self._reset_targets()

        return triggered

    def _set_targets(self, entry_price, is_long):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if step <= 0:
            self._stop_price = None
            self._take_price = None
            return

        if self.StopLossPoints > 0:
            self._stop_price = entry_price - self.StopLossPoints * step if is_long else entry_price + self.StopLossPoints * step
        else:
            self._stop_price = None

        if self.TakeProfitPoints > 0:
            self._take_price = entry_price + self.TakeProfitPoints * step if is_long else entry_price - self.TakeProfitPoints * step
        else:
            self._take_price = None

    def _reset_targets(self):
        self._stop_price = None
        self._take_price = None

    def _store_value(self, value):
        self._indicator_values.append(value)
        keep = self.SignalBar + (3 if self.EntryMode == 1 else 2) + 5
        if keep < 10:
            keep = 10
        if len(self._indicator_values) > keep:
            self._indicator_values = self._indicator_values[-keep:]

    def OnReseted(self):
        super(exp_blau_csi_strategy, self).OnReseted()
        self._indicator_values = []
        self._stop_price = None
        self._take_price = None
        self._csi_calc = None

    def CreateClone(self):
        return exp_blau_csi_strategy()


class _BlauCsiCalc:
    def __init__(self, method, mom_len, s1_len, s2_len, s3_len, phase, price1, price2):
        self._mom_len = max(1, mom_len)
        self._price1 = price1
        self._price2 = price2
        self._window = []
        self._m1 = self._make_ma(method, max(1, s1_len), phase)
        self._m2 = self._make_ma(method, max(1, s2_len), phase)
        self._m3 = self._make_ma(method, max(1, s3_len), phase)
        self._r1 = self._make_ma(method, max(1, s1_len), phase)
        self._r2 = self._make_ma(method, max(1, s2_len), phase)
        self._r3 = self._make_ma(method, max(1, s3_len), phase)

    def process(self, candle):
        current_price = self._get_price(candle, self._price1)
        past_price = self._get_price(candle, self._price2)

        self._window.append(candle)
        while len(self._window) > self._mom_len:
            self._window.pop(0)

        if len(self._window) < self._mom_len:
            return 0.0

        past_candle = self._window[0]
        past_price = self._get_price(past_candle, self._price2)

        lo = float('inf')
        hi = float('-inf')
        for c in self._window:
            lp = float(c.LowPrice)
            hp = float(c.HighPrice)
            if lp < lo:
                lo = lp
            if hp > hi:
                hi = hp

        rng = hi - lo
        momentum = current_price - past_price
        t = candle.OpenTime

        mr1 = self._m1.Process(DecimalIndicatorValue(self._m1, momentum, t))
        rr1 = self._r1.Process(DecimalIndicatorValue(self._r1, rng, t))
        m1v = float(mr1)
        r1v = float(rr1)

        mr2 = self._m2.Process(DecimalIndicatorValue(self._m2, m1v, t))
        rr2 = self._r2.Process(DecimalIndicatorValue(self._r2, r1v, t))
        m2v = float(mr2)
        r2v = float(rr2)

        mr3 = self._m3.Process(DecimalIndicatorValue(self._m3, m2v, t))
        rr3 = self._r3.Process(DecimalIndicatorValue(self._r3, r2v, t))
        m3v = float(mr3)
        r3v = float(rr3)

        if r3v != 0.0:
            return 100.0 * m3v / r3v
        return 0.0

    def _make_ma(self, method, length, phase):
        if method == 0:
            m = SimpleMovingAverage(); m.Length = length; return m
        elif method == 1:
            m = ExponentialMovingAverage(); m.Length = length; return m
        elif method == 2:
            m = SmoothedMovingAverage(); m.Length = length; return m
        elif method == 3:
            m = WeightedMovingAverage(); m.Length = length; return m
        elif method == 4:
            m = JurikMovingAverage(); m.Length = length; m.Phase = phase; return m
        else:
            m = ExponentialMovingAverage(); m.Length = length; return m

    def _get_price(self, c, pt):
        cl = float(c.ClosePrice); op = float(c.OpenPrice)
        hi = float(c.HighPrice); lo = float(c.LowPrice)
        if pt == 1: return cl
        elif pt == 2: return op
        elif pt == 3: return hi
        elif pt == 4: return lo
        elif pt == 5: return (hi + lo) / 2.0
        elif pt == 6: return (hi + lo + cl) / 3.0
        elif pt == 7: return (2.0 * cl + hi + lo) / 4.0
        elif pt == 8: return (op + cl) / 2.0
        elif pt == 9: return (op + cl + hi + lo) / 4.0
        elif pt == 10:
            return hi if cl > op else (lo if cl < op else cl)
        elif pt == 11:
            return (hi + cl) / 2.0 if cl > op else ((lo + cl) / 2.0 if cl < op else cl)
        elif pt == 12:
            r = hi + lo + cl
            if cl < op: r = (r + lo) / 2.0
            elif cl > op: r = (r + hi) / 2.0
            else: r = (r + cl) / 2.0
            return ((r - lo) + (r - hi)) / 2.0
        else: return cl
