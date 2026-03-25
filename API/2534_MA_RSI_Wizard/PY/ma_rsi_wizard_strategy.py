import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from System import Decimal
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage, WeightedMovingAverage, RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


# MA method constants
MA_SIMPLE = 0
MA_EXPONENTIAL = 1
MA_SMOOTHED = 2
MA_LINEAR_WEIGHTED = 3

# Applied price constants
PRICE_CLOSE = 0
PRICE_OPEN = 1
PRICE_HIGH = 2
PRICE_LOW = 3
PRICE_MEDIAN = 4
PRICE_TYPICAL = 5
PRICE_WEIGHTED = 6


class ma_rsi_wizard_strategy(Strategy):
    def __init__(self):
        super(ma_rsi_wizard_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._threshold_open = self.Param("ThresholdOpen", 75)
        self._threshold_close = self.Param("ThresholdClose", 100)
        self._price_level_points = self.Param("PriceLevelPoints", 0.0)
        self._stop_level_points = self.Param("StopLevelPoints", 50)
        self._take_level_points = self.Param("TakeLevelPoints", 50)
        self._expiration_bars = self.Param("ExpirationBars", 24)
        self._ma_period = self.Param("MaPeriod", 20)
        self._ma_shift = self.Param("MaShift", 3)
        self._ma_method = self.Param("MaMethod", MA_SIMPLE)
        self._ma_applied_price = self.Param("MaAppliedPrice", PRICE_CLOSE)
        self._ma_weight = self.Param("MaWeight", 0.8)
        self._rsi_period = self.Param("RsiPeriod", 3)
        self._rsi_applied_price = self.Param("RsiAppliedPrice", PRICE_CLOSE)
        self._rsi_weight = self.Param("RsiWeight", 0.5)

        self._bar_index = 0
        self._last_long_entry_bar = None
        self._last_short_entry_bar = None
        self._ma_shift_buffer = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ThresholdOpen(self):
        return self._threshold_open.Value

    @ThresholdOpen.setter
    def ThresholdOpen(self, value):
        self._threshold_open.Value = value

    @property
    def ThresholdClose(self):
        return self._threshold_close.Value

    @ThresholdClose.setter
    def ThresholdClose(self, value):
        self._threshold_close.Value = value

    @property
    def PriceLevelPoints(self):
        return self._price_level_points.Value

    @PriceLevelPoints.setter
    def PriceLevelPoints(self, value):
        self._price_level_points.Value = value

    @property
    def StopLevelPoints(self):
        return self._stop_level_points.Value

    @StopLevelPoints.setter
    def StopLevelPoints(self, value):
        self._stop_level_points.Value = value

    @property
    def TakeLevelPoints(self):
        return self._take_level_points.Value

    @TakeLevelPoints.setter
    def TakeLevelPoints(self, value):
        self._take_level_points.Value = value

    @property
    def ExpirationBars(self):
        return self._expiration_bars.Value

    @ExpirationBars.setter
    def ExpirationBars(self, value):
        self._expiration_bars.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def MaShift(self):
        return self._ma_shift.Value

    @MaShift.setter
    def MaShift(self, value):
        self._ma_shift.Value = value

    @property
    def MaMethod(self):
        return self._ma_method.Value

    @MaMethod.setter
    def MaMethod(self, value):
        self._ma_method.Value = value

    @property
    def MaAppliedPrice(self):
        return self._ma_applied_price.Value

    @MaAppliedPrice.setter
    def MaAppliedPrice(self, value):
        self._ma_applied_price.Value = value

    @property
    def MaWeight(self):
        return self._ma_weight.Value

    @MaWeight.setter
    def MaWeight(self, value):
        self._ma_weight.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiAppliedPrice(self):
        return self._rsi_applied_price.Value

    @RsiAppliedPrice.setter
    def RsiAppliedPrice(self, value):
        self._rsi_applied_price.Value = value

    @property
    def RsiWeight(self):
        return self._rsi_weight.Value

    @RsiWeight.setter
    def RsiWeight(self, value):
        self._rsi_weight.Value = value

    def OnStarted(self, time):
        super(ma_rsi_wizard_strategy, self).OnStarted(time)

        self._bar_index = 0
        self._last_long_entry_bar = None
        self._last_short_entry_bar = None
        self._ma_shift_buffer = []

        self._ma_ind = self._create_ma(int(self.MaMethod), int(self.MaPeriod))
        self._rsi_ind = RelativeStrengthIndex()
        self._rsi_ind.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        sl = int(self.StopLevelPoints)
        tp = int(self.TakeLevelPoints)
        sl_unit = Unit(sl * step, UnitTypes.Absolute) if sl > 0 else Unit(0)
        tp_unit = Unit(tp * step, UnitTypes.Absolute) if tp > 0 else Unit(0)
        if sl > 0 or tp > 0:
            self.StartProtection(stopLoss=sl_unit, takeProfit=tp_unit)

    def _create_ma(self, method, period):
        if method == MA_EXPONENTIAL:
            ma = ExponentialMovingAverage()
        elif method == MA_SMOOTHED:
            ma = SmoothedMovingAverage()
        elif method == MA_LINEAR_WEIGHTED:
            ma = WeightedMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = period
        return ma

    def _select_price(self, candle, price_type):
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if price_type == PRICE_OPEN:
            return open_p
        elif price_type == PRICE_HIGH:
            return high
        elif price_type == PRICE_LOW:
            return low
        elif price_type == PRICE_MEDIAN:
            return (high + low) / 2.0
        elif price_type == PRICE_TYPICAL:
            return (high + low + close) / 3.0
        elif price_type == PRICE_WEIGHTED:
            return (high + low + 2.0 * close) / 4.0
        else:
            return close

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        close = float(candle.ClosePrice)
        ma_price = self._select_price(candle, int(self.MaAppliedPrice))
        ma_iv = DecimalIndicatorValue(self._ma_ind, Decimal(ma_price), candle.OpenTime)
        ma_iv.IsFinal = True
        ma_result = self._ma_ind.Process(ma_iv)
        if not ma_result.IsFinal:
            return
        ma_val = float(ma_result)

        rsi_price = self._select_price(candle, int(self.RsiAppliedPrice))
        rsi_iv = DecimalIndicatorValue(self._rsi_ind, Decimal(rsi_price), candle.OpenTime)
        rsi_iv.IsFinal = True
        rsi_result = self._rsi_ind.Process(rsi_iv)
        if not rsi_result.IsFinal:
            return
        rsi_val = float(rsi_result)

        reference_ma = self._update_shifted_ma(ma_val)
        if reference_ma is None:
            return

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        price_offset = float(self.PriceLevelPoints) * step

        if float(self.PriceLevelPoints) > 0.0 and abs(close - reference_ma) < price_offset:
            return

        ma_long_signal = 100.0 if close > reference_ma else 0.0
        ma_short_signal = 100.0 if close < reference_ma else 0.0

        rsi_long_signal = min(100.0, (rsi_val - 50.0) * 2.0) if rsi_val > 50.0 else 0.0
        rsi_short_signal = min(100.0, (50.0 - rsi_val) * 2.0) if rsi_val < 50.0 else 0.0

        ma_w = float(self.MaWeight)
        rsi_w = float(self.RsiWeight)
        weight_sum = ma_w + rsi_w
        if weight_sum <= 0.0:
            return

        long_score = (ma_w * ma_long_signal + rsi_w * rsi_long_signal) / weight_sum
        short_score = (ma_w * ma_short_signal + rsi_w * rsi_short_signal) / weight_sum

        threshold_close = int(self.ThresholdClose)
        threshold_open = int(self.ThresholdOpen)
        expiration = int(self.ExpirationBars)

        if self.Position > 0 and short_score >= threshold_close:
            self.SellMarket(abs(float(self.Position)))
        elif self.Position < 0 and long_score >= threshold_close:
            self.BuyMarket(abs(float(self.Position)))

        allow_long = expiration <= 0 or self._last_long_entry_bar is None or self._bar_index - self._last_long_entry_bar >= expiration
        allow_short = expiration <= 0 or self._last_short_entry_bar is None or self._bar_index - self._last_short_entry_bar >= expiration

        if self.Position <= 0 and long_score >= threshold_open and allow_long:
            volume = float(self.Volume) + abs(float(self.Position))
            if volume > 0:
                self.BuyMarket(volume)
                self._last_long_entry_bar = self._bar_index
            return

        if self.Position >= 0 and short_score >= threshold_open and allow_short:
            volume = float(self.Volume) + abs(float(self.Position))
            if volume > 0:
                self.SellMarket(volume)
                self._last_short_entry_bar = self._bar_index

    def _update_shifted_ma(self, ma_val):
        shift = max(0, int(self.MaShift))
        if shift == 0:
            return ma_val

        self._ma_shift_buffer.append(ma_val)

        if len(self._ma_shift_buffer) <= shift:
            return None

        while len(self._ma_shift_buffer) > shift + 1:
            self._ma_shift_buffer.pop(0)

        if len(self._ma_shift_buffer) == shift + 1:
            return self._ma_shift_buffer[0]

        return None

    def OnReseted(self):
        super(ma_rsi_wizard_strategy, self).OnReseted()
        self._bar_index = 0
        self._last_long_entry_bar = None
        self._last_short_entry_bar = None
        self._ma_shift_buffer = []

    def CreateClone(self):
        return ma_rsi_wizard_strategy()
