import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex, SimpleMovingAverage,
    ExponentialMovingAverage, SmoothedMovingAverage, WeightedMovingAverage,
    Highest, Lowest)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

IBS_MA_SIMPLE = 0
IBS_MA_EXPONENTIAL = 1
IBS_MA_SMOOTHED = 2
IBS_MA_WEIGHTED = 3

APPLIED_CLOSE = 0
APPLIED_OPEN = 1
APPLIED_HIGH = 2
APPLIED_LOW = 3
APPLIED_MEDIAN = 4
APPLIED_TYPICAL = 5
APPLIED_WEIGHTED = 6

class IbsRsiCciCalculator(object):
    def __init__(self, ibs_period, ibs_type, rsi_period, rsi_price, cci_period, cci_price,
                 threshold, range_period, smooth_period, price_step,
                 koef_ibs, koef_rsi, koef_cci, kibs, kcci, krsi, posit):
        self._rsi_price = rsi_price
        self._cci_price = cci_price
        self._threshold = Decimal(float(threshold))
        self._price_step = Decimal(float(price_step))
        self._koef_ibs = Decimal(float(koef_ibs))
        self._koef_rsi = Decimal(float(koef_rsi))
        self._koef_cci = Decimal(float(koef_cci))
        self._kibs = Decimal(float(kibs))
        self._kcci = Decimal(float(kcci))
        self._krsi = Decimal(float(krsi))
        self._posit = Decimal(float(posit))
        self._previous_up = None

        self._ibs_ma = self._create_ma(ibs_type, ibs_period)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = rsi_period
        self._cci_sma = SimpleMovingAverage()
        self._cci_sma.Length = cci_period
        self._cci_period = cci_period
        self._cci_buffer = []
        self._highest = Highest()
        self._highest.Length = range_period
        self._lowest = Lowest()
        self._lowest.Length = range_period
        self._range_high_ma = self._create_ma(IBS_MA_SMOOTHED, smooth_period)
        self._range_low_ma = self._create_ma(IBS_MA_SMOOTHED, smooth_period)

    def _create_ma(self, ma_type, length):
        t = int(ma_type)
        if t == IBS_MA_EXPONENTIAL:
            ind = ExponentialMovingAverage()
        elif t == IBS_MA_SMOOTHED:
            ind = SmoothedMovingAverage()
        elif t == IBS_MA_WEIGHTED:
            ind = WeightedMovingAverage()
        else:
            ind = SimpleMovingAverage()
        ind.Length = length
        return ind

    def _get_price(self, candle, price_type):
        h = candle.HighPrice
        l = candle.LowPrice
        c = candle.ClosePrice
        o = candle.OpenPrice
        t = int(price_type)
        if t == APPLIED_OPEN:
            return o
        elif t == APPLIED_HIGH:
            return h
        elif t == APPLIED_LOW:
            return l
        elif t == APPLIED_MEDIAN:
            return Decimal.Divide(Decimal.Add(h, l), Decimal(2))
        elif t == APPLIED_TYPICAL:
            return Decimal.Divide(Decimal.Add(Decimal.Add(h, l), c), Decimal(3))
        elif t == APPLIED_WEIGHTED:
            return Decimal.Divide(Decimal.Add(Decimal.Add(Decimal.Add(h, l), c), c), Decimal(4))
        else:
            return c

    def process(self, candle):
        h = candle.HighPrice
        l = candle.LowPrice
        c = candle.ClosePrice
        open_time = candle.OpenTime

        bar_range = Math.Abs(Decimal.Subtract(h, l))
        if bar_range == Decimal(0):
            bar_range = self._price_step
        if bar_range == Decimal(0):
            return None

        ibs_raw = Decimal.Divide(Decimal.Subtract(c, l), bar_range)

        ibs_result = process_float(self._ibs_ma, ibs_raw, open_time, True)
        if not ibs_result.IsFinal:
            return None

        rsi_input = self._get_price(candle, self._rsi_price)
        rsi_result = process_float(self._rsi, rsi_input, open_time, True)
        if not rsi_result.IsFinal:
            return None

        cci_input = self._get_price(candle, self._cci_price)
        cci_value = self._process_cci(cci_input, open_time)
        if cci_value is None:
            return None

        ibs = Decimal(float(ibs_result))
        rsi = Decimal(float(rsi_result))
        cci = cci_value

        total = Decimal(0)
        # sum += _kibs * (ibs - 0.5) * 100 * _koefIbs
        total = Decimal.Add(total, Decimal.Multiply(Decimal.Multiply(Decimal.Multiply(self._kibs, Decimal.Subtract(ibs, Decimal(0.5))), Decimal(100)), self._koef_ibs))
        # sum += _kcci * cci * _koefCci
        total = Decimal.Add(total, Decimal.Multiply(Decimal.Multiply(self._kcci, cci), self._koef_cci))
        # sum += _krsi * (rsi - 50) * _koefRsi
        total = Decimal.Add(total, Decimal.Multiply(Decimal.Multiply(self._krsi, Decimal.Subtract(rsi, Decimal(50))), self._koef_rsi))
        # sum /= 3
        total = Decimal.Divide(total, Decimal(3))

        target = Decimal.Multiply(self._posit, total)
        up = self._previous_up if self._previous_up is not None else target
        diff = Decimal.Subtract(target, up)

        if Math.Abs(diff) > self._threshold:
            if diff > Decimal(0):
                up = Decimal.Subtract(target, self._threshold)
            else:
                up = Decimal.Add(target, self._threshold)
        else:
            up = target

        self._previous_up = up

        highest_result = process_float(self._highest, up, open_time, True)
        lowest_result = process_float(self._lowest, up, open_time, True)
        if not highest_result.IsFinal or not lowest_result.IsFinal:
            return None

        highest_val = Decimal(float(highest_result))
        lowest_val = Decimal(float(lowest_result))

        high_smooth = process_float(self._range_high_ma, highest_val, open_time, True)
        low_smooth = process_float(self._range_low_ma, lowest_val, open_time, True)
        if not high_smooth.IsFinal or not low_smooth.IsFinal:
            return None

        up_band = Decimal(float(high_smooth))
        low_band = Decimal(float(low_smooth))
        signal = Decimal.Divide(Decimal.Add(up_band, low_band), Decimal(2))

        return (up, signal)

    def _process_cci(self, price, open_time):
        ma_result = process_float(self._cci_sma, price, open_time, True)
        self._cci_buffer.append(price)
        if len(self._cci_buffer) > self._cci_period:
            self._cci_buffer.pop(0)

        if not ma_result.IsFinal or len(self._cci_buffer) < self._cci_period:
            return None

        ma = Decimal(float(ma_result))
        total = Decimal(0)
        for v in self._cci_buffer:
            total = Decimal.Add(total, Math.Abs(Decimal.Subtract(v, ma)))

        if total == Decimal(0):
            return Decimal(0)

        mean_deviation = Decimal.Divide(total, Decimal(self._cci_period))
        if mean_deviation == Decimal(0):
            return Decimal(0)

        return Decimal.Divide(Decimal.Subtract(price, ma), Decimal.Multiply(Decimal(0.015), mean_deviation))

    def reset(self):
        self._previous_up = None
        self._ibs_ma.Reset()
        self._rsi.Reset()
        self._cci_sma.Reset()
        self._cci_buffer = []
        self._highest.Reset()
        self._lowest.Reset()
        self._range_high_ma.Reset()
        self._range_low_ma.Reset()

class ibs_rsi_cci_v4_x2_strategy(Strategy):
    def __init__(self):
        super(ibs_rsi_cci_v4_x2_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", Decimal(1))
        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._trend_ibs_period = self.Param("TrendIbsPeriod", 5)
        self._trend_ibs_ma_type = self.Param("TrendIbsMaType", IBS_MA_SIMPLE)
        self._trend_rsi_period = self.Param("TrendRsiPeriod", 14)
        self._trend_rsi_price = self.Param("TrendRsiPrice", APPLIED_CLOSE)
        self._trend_cci_period = self.Param("TrendCciPeriod", 14)
        self._trend_cci_price = self.Param("TrendCciPrice", APPLIED_MEDIAN)
        self._trend_threshold = self.Param("TrendThreshold", Decimal(50))
        self._trend_range_period = self.Param("TrendRangePeriod", 25)
        self._trend_smooth_period = self.Param("TrendSmoothPeriod", 3)
        self._trend_signal_bar = self.Param("TrendSignalBar", 1)
        self._allow_long_entries = self.Param("AllowLongEntries", True)
        self._allow_short_entries = self.Param("AllowShortEntries", True)
        self._close_long_on_trend_flip = self.Param("CloseLongOnTrendFlip", True)
        self._close_short_on_trend_flip = self.Param("CloseShortOnTrendFlip", True)
        self._koef_ibs = self.Param("KoefIbs", Decimal(7))
        self._koef_rsi = self.Param("KoefRsi", Decimal(9))
        self._koef_cci = self.Param("KoefCci", Decimal(1))
        self._kibs = self.Param("Kibs", Decimal(-1))
        self._kcci = self.Param("Kcci", Decimal(-1))
        self._krsi = self.Param("Krsi", Decimal(-1))
        self._posit = self.Param("Posit", Decimal(-1))

        self._signal_candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))
        self._signal_ibs_period = self.Param("SignalIbsPeriod", 5)
        self._signal_ibs_ma_type = self.Param("SignalIbsMaType", IBS_MA_SIMPLE)
        self._signal_rsi_period = self.Param("SignalRsiPeriod", 14)
        self._signal_rsi_price = self.Param("SignalRsiPrice", APPLIED_CLOSE)
        self._signal_cci_period = self.Param("SignalCciPeriod", 14)
        self._signal_cci_price = self.Param("SignalCciPrice", APPLIED_MEDIAN)
        self._signal_threshold = self.Param("SignalThreshold", Decimal(50))
        self._signal_range_period = self.Param("SignalRangePeriod", 25)
        self._signal_smooth_period = self.Param("SignalSmoothPeriod", 3)
        self._signal_signal_bar = self.Param("SignalSignalBar", 1)
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10)
        self._close_long_on_signal_cross = self.Param("CloseLongOnSignalCross", False)
        self._close_short_on_signal_cross = self.Param("CloseShortOnSignalCross", False)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)

        self._trend_values = []
        self._signal_values = []
        self._trend_calculator = None
        self._signal_calculator = None
        self._trend_direction = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ibs_rsi_cci_v4_x2_strategy, self).OnStarted2(time)

        price_step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(0.0001)

        self._trend_calculator = IbsRsiCciCalculator(
            int(self._trend_ibs_period.Value), int(self._trend_ibs_ma_type.Value),
            int(self._trend_rsi_period.Value), int(self._trend_rsi_price.Value),
            int(self._trend_cci_period.Value), int(self._trend_cci_price.Value),
            self._trend_threshold.Value, int(self._trend_range_period.Value), int(self._trend_smooth_period.Value),
            price_step,
            self._koef_ibs.Value, self._koef_rsi.Value, self._koef_cci.Value,
            self._kibs.Value, self._kcci.Value, self._krsi.Value, self._posit.Value)

        self._signal_calculator = IbsRsiCciCalculator(
            int(self._signal_ibs_period.Value), int(self._signal_ibs_ma_type.Value),
            int(self._signal_rsi_period.Value), int(self._signal_rsi_price.Value),
            int(self._signal_cci_period.Value), int(self._signal_cci_price.Value),
            self._signal_threshold.Value, int(self._signal_range_period.Value), int(self._signal_smooth_period.Value),
            price_step,
            self._koef_ibs.Value, self._koef_rsi.Value, self._koef_cci.Value,
            self._kibs.Value, self._kcci.Value, self._krsi.Value, self._posit.Value)

        self._trend_values = []
        self._signal_values = []
        self._trend_direction = 0
        self._cooldown_remaining = 0

        trend_sub = self.SubscribeCandles(self._trend_candle_type.Value)
        trend_sub.Bind(self._process_trend).Start()

        signal_sub = self.SubscribeCandles(self._signal_candle_type.Value)
        signal_sub.Bind(self._process_signal).Start()

        tp = int(self._take_profit_points.Value)
        sl = int(self._stop_loss_points.Value)
        if tp > 0 or sl > 0:
            take_unit = Unit(Decimal.Multiply(Decimal(tp), price_step), UnitTypes.Absolute)
            stop_unit = Unit(Decimal.Multiply(Decimal(sl), price_step), UnitTypes.Absolute)
            self.StartProtection(stop_unit, take_unit)

    def _process_trend(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._trend_calculator is None:
            return

        value = self._trend_calculator.process(candle)
        if value is None:
            return

        self._trend_values.append(value)

        tsb = int(self._trend_signal_bar.Value)
        max_count = max(tsb + 5, 32)
        if len(self._trend_values) > max_count:
            self._trend_values.pop(0)

        if len(self._trend_values) <= tsb:
            return

        index = len(self._trend_values) - (tsb + 1)
        if index < 0:
            return

        selected = self._trend_values[index]
        up_val = selected[0]
        down_val = selected[1]

        if up_val > down_val:
            self._trend_direction = 1
        elif up_val < down_val:
            self._trend_direction = -1
        else:
            self._trend_direction = 0

    def _process_signal(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._signal_calculator is None:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        value = self._signal_calculator.process(candle)
        if value is None:
            return

        self._signal_values.append(value)

        ssb = int(self._signal_signal_bar.Value)
        max_count = max(ssb + 10, 48)
        if len(self._signal_values) > max_count:
            self._signal_values.pop(0)

        if len(self._signal_values) <= ssb + 1:
            return

        current_index = len(self._signal_values) - (ssb + 1)
        previous_index = current_index - 1
        if current_index < 0 or previous_index < 0:
            return

        current = self._signal_values[current_index]
        previous = self._signal_values[previous_index]

        close_long = bool(self._close_long_on_signal_cross.Value) and previous[0] < previous[1]
        close_short = bool(self._close_short_on_signal_cross.Value) and previous[0] > previous[1]
        open_long = False
        open_short = False

        if self._trend_direction < 0:
            if bool(self._close_long_on_trend_flip.Value):
                close_long = True
            if self._cooldown_remaining == 0 and bool(self._allow_short_entries.Value) and current[0] >= current[1] and previous[0] < previous[1]:
                open_short = True
        elif self._trend_direction > 0:
            if bool(self._close_short_on_trend_flip.Value):
                close_short = True
            if self._cooldown_remaining == 0 and bool(self._allow_long_entries.Value) and current[0] <= current[1] and previous[0] > previous[1]:
                open_long = True

        submitted = False

        if close_long and self.Position > 0:
            self.SellMarket()
            submitted = True

        if close_short and self.Position < 0:
            self.BuyMarket()
            submitted = True

        if open_long and self.Position <= 0 and bool(self._allow_long_entries.Value):
            self.BuyMarket()
            submitted = True
        elif open_short and self.Position >= 0 and bool(self._allow_short_entries.Value):
            self.SellMarket()
            submitted = True

        if submitted:
            self._cooldown_remaining = int(self._signal_cooldown_bars.Value)

    def OnReseted(self):
        super(ibs_rsi_cci_v4_x2_strategy, self).OnReseted()
        self._trend_values = []
        self._signal_values = []
        self._trend_direction = 0
        self._cooldown_remaining = 0
        if self._trend_calculator is not None:
            self._trend_calculator.reset()
        if self._signal_calculator is not None:
            self._signal_calculator.reset()
        self._trend_calculator = None
        self._signal_calculator = None

    def CreateClone(self):
        return ibs_rsi_cci_v4_x2_strategy()
