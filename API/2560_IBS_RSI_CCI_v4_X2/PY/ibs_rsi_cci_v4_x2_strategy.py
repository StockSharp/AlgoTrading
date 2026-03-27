import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal, Array, Object
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (RelativeStrengthIndex, SimpleMovingAverage,
    ExponentialMovingAverage, SmoothedMovingAverage, WeightedMovingAverage,
    Highest, Lowest)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
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
        self._threshold = float(threshold)
        self._price_step = float(price_step)
        self._koef_ibs = float(koef_ibs)
        self._koef_rsi = float(koef_rsi)
        self._koef_cci = float(koef_cci)
        self._kibs = float(kibs)
        self._kcci = float(kcci)
        self._krsi = float(krsi)
        self._posit = float(posit)
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
            ind.Length = length
            return ind
        elif t == IBS_MA_SMOOTHED:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif t == IBS_MA_WEIGHTED:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind

    def _get_price(self, candle, price_type):
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        t = int(price_type)
        if t == APPLIED_OPEN:
            return o
        elif t == APPLIED_HIGH:
            return h
        elif t == APPLIED_LOW:
            return l
        elif t == APPLIED_MEDIAN:
            return (h + l) / 2.0
        elif t == APPLIED_TYPICAL:
            return (h + l + c) / 3.0
        elif t == APPLIED_WEIGHTED:
            return (h + l + c + c) / 4.0
        else:
            return c

    def process(self, candle):
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        open_time = candle.OpenTime

        bar_range = abs(h - l)
        if bar_range == 0.0:
            bar_range = self._price_step
        if bar_range == 0.0:
            return None

        ibs_raw = (c - l) / bar_range
        ibs_result = self._ibs_ma.Process(self._ibs_ma.CreateValue(open_time, Array[object]([Decimal(ibs_raw)])))
        if not ibs_result.IsFinal:
            return None

        rsi_input = self._get_price(candle, self._rsi_price)
        rsi_result = self._rsi.Process(self._rsi.CreateValue(open_time, Array[object]([Decimal(rsi_input)])))
        if not rsi_result.IsFinal:
            return None

        cci_input = Decimal(self._get_price(candle, self._cci_price))
        cci_value = self._process_cci(cci_input, open_time)
        if cci_value is None:
            return None

        ibs = float(ibs_result)
        rsi = float(rsi_result)
        cci = cci_value

        total = 0.0
        total += self._kibs * (ibs - 0.5) * 100.0 * self._koef_ibs
        total += self._kcci * cci * self._koef_cci
        total += self._krsi * (rsi - 50.0) * self._koef_rsi
        total /= 3.0

        target = self._posit * total
        up = self._previous_up if self._previous_up is not None else target
        diff = target - up

        if abs(diff) > self._threshold:
            if diff > 0.0:
                up = target - self._threshold
            else:
                up = target + self._threshold
        else:
            up = target

        self._previous_up = up

        highest_result = self._highest.Process(self._highest.CreateValue(open_time, Array[object]([Decimal(up)])))
        lowest_result = self._lowest.Process(self._lowest.CreateValue(open_time, Array[object]([Decimal(up)])))
        if not highest_result.IsFinal or not lowest_result.IsFinal:
            return None

        highest_val = float(highest_result)
        lowest_val = float(lowest_result)

        high_smooth = self._range_high_ma.Process(self._range_high_ma.CreateValue(open_time, Array[object]([Decimal(highest_val)])))
        low_smooth = self._range_low_ma.Process(self._range_low_ma.CreateValue(open_time, Array[object]([Decimal(lowest_val)])))
        if not high_smooth.IsFinal or not low_smooth.IsFinal:
            return None

        up_band = float(high_smooth)
        low_band = float(low_smooth)
        signal = (up_band + low_band) / 2.0

        return (up, signal)

    def _process_cci(self, price, open_time):
        ma_result = self._cci_sma.Process(self._cci_sma.CreateValue(open_time, Array[object]([Decimal(float(price))])))
        self._cci_buffer.append(price)
        if len(self._cci_buffer) > self._cci_period:
            self._cci_buffer.pop(0)

        if not ma_result.IsFinal or len(self._cci_buffer) < self._cci_period:
            return None

        ma = float(ma_result)
        total = 0.0
        for v in self._cci_buffer:
            total += abs(v - ma)

        if total == 0.0:
            return 0.0

        mean_deviation = total / self._cci_period
        if mean_deviation == 0.0:
            return 0.0

        return (price - ma) / (0.015 * mean_deviation)

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

        self._order_volume = self.Param("OrderVolume", 1.0)
        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._trend_ibs_period = self.Param("TrendIbsPeriod", 5)
        self._trend_ibs_ma_type = self.Param("TrendIbsMaType", IBS_MA_SIMPLE)
        self._trend_rsi_period = self.Param("TrendRsiPeriod", 14)
        self._trend_rsi_price = self.Param("TrendRsiPrice", APPLIED_CLOSE)
        self._trend_cci_period = self.Param("TrendCciPeriod", 14)
        self._trend_cci_price = self.Param("TrendCciPrice", APPLIED_MEDIAN)
        self._trend_threshold = self.Param("TrendThreshold", 50.0)
        self._trend_range_period = self.Param("TrendRangePeriod", 25)
        self._trend_smooth_period = self.Param("TrendSmoothPeriod", 3)
        self._trend_signal_bar = self.Param("TrendSignalBar", 1)
        self._allow_long_entries = self.Param("AllowLongEntries", True)
        self._allow_short_entries = self.Param("AllowShortEntries", True)
        self._close_long_on_trend_flip = self.Param("CloseLongOnTrendFlip", True)
        self._close_short_on_trend_flip = self.Param("CloseShortOnTrendFlip", True)
        self._koef_ibs = self.Param("KoefIbs", 7.0)
        self._koef_rsi = self.Param("KoefRsi", 9.0)
        self._koef_cci = self.Param("KoefCci", 1.0)
        self._kibs = self.Param("Kibs", -1.0)
        self._kcci = self.Param("Kcci", -1.0)
        self._krsi = self.Param("Krsi", -1.0)
        self._posit = self.Param("Posit", -1.0)

        self._signal_candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))
        self._signal_ibs_period = self.Param("SignalIbsPeriod", 5)
        self._signal_ibs_ma_type = self.Param("SignalIbsMaType", IBS_MA_SIMPLE)
        self._signal_rsi_period = self.Param("SignalRsiPeriod", 14)
        self._signal_rsi_price = self.Param("SignalRsiPrice", APPLIED_CLOSE)
        self._signal_cci_period = self.Param("SignalCciPeriod", 14)
        self._signal_cci_price = self.Param("SignalCciPrice", APPLIED_MEDIAN)
        self._signal_threshold = self.Param("SignalThreshold", 50.0)
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

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def TrendCandleType(self):
        return self._trend_candle_type.Value

    @TrendCandleType.setter
    def TrendCandleType(self, value):
        self._trend_candle_type.Value = value

    @property
    def TrendIbsPeriod(self):
        return self._trend_ibs_period.Value

    @TrendIbsPeriod.setter
    def TrendIbsPeriod(self, value):
        self._trend_ibs_period.Value = value

    @property
    def TrendIbsMaType(self):
        return self._trend_ibs_ma_type.Value

    @TrendIbsMaType.setter
    def TrendIbsMaType(self, value):
        self._trend_ibs_ma_type.Value = value

    @property
    def TrendRsiPeriod(self):
        return self._trend_rsi_period.Value

    @TrendRsiPeriod.setter
    def TrendRsiPeriod(self, value):
        self._trend_rsi_period.Value = value

    @property
    def TrendRsiPrice(self):
        return self._trend_rsi_price.Value

    @TrendRsiPrice.setter
    def TrendRsiPrice(self, value):
        self._trend_rsi_price.Value = value

    @property
    def TrendCciPeriod(self):
        return self._trend_cci_period.Value

    @TrendCciPeriod.setter
    def TrendCciPeriod(self, value):
        self._trend_cci_period.Value = value

    @property
    def TrendCciPrice(self):
        return self._trend_cci_price.Value

    @TrendCciPrice.setter
    def TrendCciPrice(self, value):
        self._trend_cci_price.Value = value

    @property
    def TrendThreshold(self):
        return self._trend_threshold.Value

    @TrendThreshold.setter
    def TrendThreshold(self, value):
        self._trend_threshold.Value = value

    @property
    def TrendRangePeriod(self):
        return self._trend_range_period.Value

    @TrendRangePeriod.setter
    def TrendRangePeriod(self, value):
        self._trend_range_period.Value = value

    @property
    def TrendSmoothPeriod(self):
        return self._trend_smooth_period.Value

    @TrendSmoothPeriod.setter
    def TrendSmoothPeriod(self, value):
        self._trend_smooth_period.Value = value

    @property
    def TrendSignalBar(self):
        return self._trend_signal_bar.Value

    @TrendSignalBar.setter
    def TrendSignalBar(self, value):
        self._trend_signal_bar.Value = value

    @property
    def AllowLongEntries(self):
        return self._allow_long_entries.Value

    @AllowLongEntries.setter
    def AllowLongEntries(self, value):
        self._allow_long_entries.Value = value

    @property
    def AllowShortEntries(self):
        return self._allow_short_entries.Value

    @AllowShortEntries.setter
    def AllowShortEntries(self, value):
        self._allow_short_entries.Value = value

    @property
    def CloseLongOnTrendFlip(self):
        return self._close_long_on_trend_flip.Value

    @CloseLongOnTrendFlip.setter
    def CloseLongOnTrendFlip(self, value):
        self._close_long_on_trend_flip.Value = value

    @property
    def CloseShortOnTrendFlip(self):
        return self._close_short_on_trend_flip.Value

    @CloseShortOnTrendFlip.setter
    def CloseShortOnTrendFlip(self, value):
        self._close_short_on_trend_flip.Value = value

    @property
    def KoefIbs(self):
        return self._koef_ibs.Value

    @KoefIbs.setter
    def KoefIbs(self, value):
        self._koef_ibs.Value = value

    @property
    def KoefRsi(self):
        return self._koef_rsi.Value

    @KoefRsi.setter
    def KoefRsi(self, value):
        self._koef_rsi.Value = value

    @property
    def KoefCci(self):
        return self._koef_cci.Value

    @KoefCci.setter
    def KoefCci(self, value):
        self._koef_cci.Value = value

    @property
    def Kibs(self):
        return self._kibs.Value

    @Kibs.setter
    def Kibs(self, value):
        self._kibs.Value = value

    @property
    def Kcci(self):
        return self._kcci.Value

    @Kcci.setter
    def Kcci(self, value):
        self._kcci.Value = value

    @property
    def Krsi(self):
        return self._krsi.Value

    @Krsi.setter
    def Krsi(self, value):
        self._krsi.Value = value

    @property
    def Posit(self):
        return self._posit.Value

    @Posit.setter
    def Posit(self, value):
        self._posit.Value = value

    @property
    def SignalCandleType(self):
        return self._signal_candle_type.Value

    @SignalCandleType.setter
    def SignalCandleType(self, value):
        self._signal_candle_type.Value = value

    @property
    def SignalIbsPeriod(self):
        return self._signal_ibs_period.Value

    @SignalIbsPeriod.setter
    def SignalIbsPeriod(self, value):
        self._signal_ibs_period.Value = value

    @property
    def SignalIbsMaType(self):
        return self._signal_ibs_ma_type.Value

    @SignalIbsMaType.setter
    def SignalIbsMaType(self, value):
        self._signal_ibs_ma_type.Value = value

    @property
    def SignalRsiPeriod(self):
        return self._signal_rsi_period.Value

    @SignalRsiPeriod.setter
    def SignalRsiPeriod(self, value):
        self._signal_rsi_period.Value = value

    @property
    def SignalRsiPrice(self):
        return self._signal_rsi_price.Value

    @SignalRsiPrice.setter
    def SignalRsiPrice(self, value):
        self._signal_rsi_price.Value = value

    @property
    def SignalCciPeriod(self):
        return self._signal_cci_period.Value

    @SignalCciPeriod.setter
    def SignalCciPeriod(self, value):
        self._signal_cci_period.Value = value

    @property
    def SignalCciPrice(self):
        return self._signal_cci_price.Value

    @SignalCciPrice.setter
    def SignalCciPrice(self, value):
        self._signal_cci_price.Value = value

    @property
    def SignalThreshold(self):
        return self._signal_threshold.Value

    @SignalThreshold.setter
    def SignalThreshold(self, value):
        self._signal_threshold.Value = value

    @property
    def SignalRangePeriod(self):
        return self._signal_range_period.Value

    @SignalRangePeriod.setter
    def SignalRangePeriod(self, value):
        self._signal_range_period.Value = value

    @property
    def SignalSmoothPeriod(self):
        return self._signal_smooth_period.Value

    @SignalSmoothPeriod.setter
    def SignalSmoothPeriod(self, value):
        self._signal_smooth_period.Value = value

    @property
    def SignalSignalBar(self):
        return self._signal_signal_bar.Value

    @SignalSignalBar.setter
    def SignalSignalBar(self, value):
        self._signal_signal_bar.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    @property
    def CloseLongOnSignalCross(self):
        return self._close_long_on_signal_cross.Value

    @CloseLongOnSignalCross.setter
    def CloseLongOnSignalCross(self, value):
        self._close_long_on_signal_cross.Value = value

    @property
    def CloseShortOnSignalCross(self):
        return self._close_short_on_signal_cross.Value

    @CloseShortOnSignalCross.setter
    def CloseShortOnSignalCross(self, value):
        self._close_short_on_signal_cross.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    def OnStarted(self, time):
        super(ibs_rsi_cci_v4_x2_strategy, self).OnStarted(time)

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001

        self._trend_calculator = IbsRsiCciCalculator(
            int(self.TrendIbsPeriod), int(self.TrendIbsMaType),
            int(self.TrendRsiPeriod), int(self.TrendRsiPrice),
            int(self.TrendCciPeriod), int(self.TrendCciPrice),
            float(self.TrendThreshold), int(self.TrendRangePeriod), int(self.TrendSmoothPeriod),
            price_step,
            float(self.KoefIbs), float(self.KoefRsi), float(self.KoefCci),
            float(self.Kibs), float(self.Kcci), float(self.Krsi), float(self.Posit))

        self._signal_calculator = IbsRsiCciCalculator(
            int(self.SignalIbsPeriod), int(self.SignalIbsMaType),
            int(self.SignalRsiPeriod), int(self.SignalRsiPrice),
            int(self.SignalCciPeriod), int(self.SignalCciPrice),
            float(self.SignalThreshold), int(self.SignalRangePeriod), int(self.SignalSmoothPeriod),
            price_step,
            float(self.KoefIbs), float(self.KoefRsi), float(self.KoefCci),
            float(self.Kibs), float(self.Kcci), float(self.Krsi), float(self.Posit))

        self._trend_values = []
        self._signal_values = []
        self._trend_direction = 0
        self._cooldown_remaining = 0

        trend_sub = self.SubscribeCandles(self.TrendCandleType)
        trend_sub.Bind(self._process_trend).Start()

        signal_sub = self.SubscribeCandles(self.SignalCandleType)
        signal_sub.Bind(self._process_signal).Start()

        tp = int(self.TakeProfitPoints)
        sl = int(self.StopLossPoints)
        if tp > 0 or sl > 0:
            take_unit = Unit(float(tp) * price_step, UnitTypes.Absolute)
            stop_unit = Unit(float(sl) * price_step, UnitTypes.Absolute)
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

        tsb = int(self.TrendSignalBar)
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

        ssb = int(self.SignalSignalBar)
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

        close_long = self.CloseLongOnSignalCross and previous[0] < previous[1]
        close_short = self.CloseShortOnSignalCross and previous[0] > previous[1]
        open_long = False
        open_short = False

        if self._trend_direction < 0:
            if self.CloseLongOnTrendFlip:
                close_long = True
            if self._cooldown_remaining == 0 and self.AllowShortEntries and current[0] >= current[1] and previous[0] < previous[1]:
                open_short = True
        elif self._trend_direction > 0:
            if self.CloseShortOnTrendFlip:
                close_short = True
            if self._cooldown_remaining == 0 and self.AllowLongEntries and current[0] <= current[1] and previous[0] > previous[1]:
                open_long = True

        submitted = False

        if close_long and self.Position > 0:
            self.SellMarket()
            submitted = True

        if close_short and self.Position < 0:
            self.BuyMarket()
            submitted = True

        if open_long and self.Position <= 0 and self.AllowLongEntries:
            self.BuyMarket()
            submitted = True
        elif open_short and self.Position >= 0 and self.AllowShortEntries:
            self.SellMarket()
            submitted = True

        if submitted:
            self._cooldown_remaining = int(self.SignalCooldownBars)

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
