import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage,
    WeightedMovingAverage, JurikMovingAverage, TripleExponentialMovingAverage,
    KaufmanAdaptiveMovingAverage, DecimalIndicatorValue
)
from StockSharp.Algo.Strategies import Strategy


class blau_c_momentum_strategy(Strategy):
    def __init__(self):
        super(blau_c_momentum_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._enable_long_entry = self.Param("EnableLongEntry", True)
        self._enable_short_entry = self.Param("EnableShortEntry", True)
        self._enable_long_exit = self.Param("EnableLongExit", True)
        self._enable_short_exit = self.Param("EnableShortExit", True)
        self._entry_mode = self.Param("EntryMode", 1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._smoothing_method = self.Param("SmoothingMethod", 1)
        self._momentum_length = self.Param("MomentumLength", 1)
        self._first_smooth_length = self.Param("FirstSmoothLength", 20)
        self._second_smooth_length = self.Param("SecondSmoothLength", 5)
        self._third_smooth_length = self.Param("ThirdSmoothLength", 3)
        self._phase = self.Param("Phase", 15)
        self._price_for_close = self.Param("PriceForClose", 1)
        self._price_for_open = self.Param("PriceForOpen", 2)
        self._signal_bar = self.Param("SignalBar", 1)

        self._indicator_history = []
        self._momentum_calc = None
        self._candle_span = 0
        self._long_trade_block_until = None
        self._short_trade_block_until = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def EnableLongEntry(self):
        return self._enable_long_entry.Value

    @property
    def EnableShortEntry(self):
        return self._enable_short_entry.Value

    @property
    def EnableLongExit(self):
        return self._enable_long_exit.Value

    @property
    def EnableShortExit(self):
        return self._enable_short_exit.Value

    @property
    def EntryMode(self):
        return self._entry_mode.Value

    @property
    def SmoothingMethod(self):
        return self._smoothing_method.Value

    @property
    def MomentumLength(self):
        return self._momentum_length.Value

    @property
    def FirstSmoothLength(self):
        return self._first_smooth_length.Value

    @property
    def SecondSmoothLength(self):
        return self._second_smooth_length.Value

    @property
    def ThirdSmoothLength(self):
        return self._third_smooth_length.Value

    @property
    def Phase(self):
        return self._phase.Value

    @property
    def PriceForClose(self):
        return self._price_for_close.Value

    @property
    def PriceForOpen(self):
        return self._price_for_open.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    def OnStarted(self, time):
        super(blau_c_momentum_strategy, self).OnStarted(time)

        self._indicator_history = []
        self._momentum_calc = _BlauMomentumCalc(
            self.SmoothingMethod, self.MomentumLength, self.FirstSmoothLength,
            self.SecondSmoothLength, self.ThirdSmoothLength, self.Phase,
            self.PriceForClose, self.PriceForOpen
        )

        try:
            ct = self.CandleType
            self._candle_span = ct.Arg.TotalSeconds if ct.Arg is not None else 0
        except:
            self._candle_span = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0
        tp_unit = Unit(self.TakeProfitPoints * step, UnitTypes.Absolute) if self.TakeProfitPoints > 0 and step > 0 else None
        sl_unit = Unit(self.StopLossPoints * step, UnitTypes.Absolute) if self.StopLossPoints > 0 and step > 0 else None
        self.StartProtection(tp_unit, sl_unit)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished or self._momentum_calc is None:
            return

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        val = self._momentum_calc.process(candle, step)
        if val is None:
            return

        self._indicator_history.append(val)
        required = max(self.SignalBar + 3, 5)
        if len(self._indicator_history) > required:
            self._indicator_history = self._indicator_history[-required:]

        current = self._get_hist(self.SignalBar)
        previous = self._get_hist(self.SignalBar + 1)
        if current is None or previous is None:
            return

        close_short = False
        close_long = False
        open_long = False
        open_short = False

        if self.EntryMode == 0:  # Breakdown
            if previous > 0:
                if self.EnableLongEntry and current <= 0:
                    open_long = True
                if self.EnableShortExit:
                    close_short = True
            if previous < 0:
                if self.EnableShortEntry and current >= 0:
                    open_short = True
                if self.EnableLongExit:
                    close_long = True
        else:  # Twist
            older = self._get_hist(self.SignalBar + 2)
            if older is None:
                return
            if previous < older:
                if self.EnableLongEntry and current >= previous:
                    open_long = True
                if self.EnableShortExit:
                    close_short = True
            if previous > older:
                if self.EnableShortEntry and current <= previous:
                    open_short = True
                if self.EnableLongExit:
                    close_long = True

        if close_long and self.Position > 0:
            self.SellMarket()
        if close_short and self.Position < 0:
            self.BuyMarket()
        if open_long and self.Position <= 0 and self._can_enter_long(candle.OpenTime):
            self.BuyMarket()
            self._set_block(True, candle.OpenTime)
        if open_short and self.Position >= 0 and self._can_enter_short(candle.OpenTime):
            self.SellMarket()
            self._set_block(False, candle.OpenTime)

    def _get_hist(self, shift):
        if shift < 0:
            return None
        idx = len(self._indicator_history) - shift - 1
        if idx < 0 or idx >= len(self._indicator_history):
            return None
        return self._indicator_history[idx]

    def _can_enter_long(self, t):
        return self._long_trade_block_until is None or t >= self._long_trade_block_until

    def _can_enter_short(self, t):
        return self._short_trade_block_until is None or t >= self._short_trade_block_until

    def _set_block(self, is_long, t):
        blocked = t.AddSeconds(self._candle_span) if self._candle_span > 0 else t
        if is_long:
            self._long_trade_block_until = blocked
        else:
            self._short_trade_block_until = blocked

    def OnReseted(self):
        super(blau_c_momentum_strategy, self).OnReseted()
        self._indicator_history = []
        self._momentum_calc = None
        self._long_trade_block_until = None
        self._short_trade_block_until = None
        self._candle_span = 0

    def CreateClone(self):
        return blau_c_momentum_strategy()


class _BlauMomentumCalc:
    def __init__(self, method, mom_len, s1_len, s2_len, s3_len, phase, price1, price2):
        self._mom_len = max(1, mom_len)
        self._price1 = price1
        self._price2 = price2
        self._buf = []
        self._ma1 = self._make_ma(method, max(1, s1_len), phase)
        self._ma2 = self._make_ma(method, max(1, s2_len), phase)
        self._ma3 = self._make_ma(method, max(1, s3_len), phase)

    def process(self, candle, point):
        v1 = self._price(self._price1, candle)
        v2 = self._price(self._price2, candle)
        self._buf.append(v2)
        if len(self._buf) > self._mom_len:
            self._buf.pop(0)
        if len(self._buf) < self._mom_len:
            return None

        momentum = v1 - self._buf[0]
        t = candle.OpenTime

        r1 = self._ma1.Process(DecimalIndicatorValue(self._ma1, momentum, t))
        if not self._ma1.IsFormed:
            return None
        s1 = float(r1.ToDecimal())

        r2 = self._ma2.Process(DecimalIndicatorValue(self._ma2, s1, t))
        if not self._ma2.IsFormed:
            return None
        s2 = float(r2.ToDecimal())

        r3 = self._ma3.Process(DecimalIndicatorValue(self._ma3, s2, t))
        if not self._ma3.IsFormed:
            return None
        s3 = float(r3.ToDecimal())

        return s3 * 100.0 / point if point > 0 else s3

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
        elif method == 5:
            m = TripleExponentialMovingAverage(); m.Length = length; return m
        elif method == 6:
            m = KaufmanAdaptiveMovingAverage(); m.Length = length; return m
        else:
            m = ExponentialMovingAverage(); m.Length = length; return m

    def _price(self, pt, c):
        cl = float(c.ClosePrice); op = float(c.OpenPrice)
        hi = float(c.HighPrice); lo = float(c.LowPrice)
        if pt == 1: return cl
        elif pt == 2: return op
        elif pt == 3: return hi
        elif pt == 4: return lo
        elif pt == 5: return (hi + lo) / 2.0
        elif pt == 6: return (cl + hi + lo) / 3.0
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
