import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, CommodityChannelIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class ibs_rsi_cci_v4_strategy(Strategy):
    def __init__(self):
        super(ibs_rsi_cci_v4_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._ibs_period = self.Param("IbsPeriod", 5)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._cci_period = self.Param("CciPeriod", 14)
        self._range_period = self.Param("RangePeriod", 25)
        self._smooth_period = self.Param("SmoothPeriod", 3)
        self._step_threshold = self.Param("StepThreshold", 50.0)
        self._ibs_weight = self.Param("IbsWeight", 700.0)
        self._rsi_weight = self.Param("RsiWeight", 9.0)
        self._cci_weight = self.Param("CciWeight", 1.0)
        self._signal_bar = self.Param("SignalBar", 1)
        self._enable_long_open = self.Param("EnableLongOpen", True)
        self._enable_short_open = self.Param("EnableShortOpen", True)
        self._enable_long_close = self.Param("EnableLongClose", True)
        self._enable_short_close = self.Param("EnableShortClose", True)
        self._order_volume = self.Param("OrderVolume", 1.0)

        self._has_signal = False
        self._last_signal = 0.0
        self._signal_history = []
        self._baseline_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def IbsPeriod(self):
        return self._ibs_period.Value

    @IbsPeriod.setter
    def IbsPeriod(self, value):
        self._ibs_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def RangePeriod(self):
        return self._range_period.Value

    @RangePeriod.setter
    def RangePeriod(self, value):
        self._range_period.Value = value

    @property
    def SmoothPeriod(self):
        return self._smooth_period.Value

    @SmoothPeriod.setter
    def SmoothPeriod(self, value):
        self._smooth_period.Value = value

    @property
    def StepThreshold(self):
        return self._step_threshold.Value

    @StepThreshold.setter
    def StepThreshold(self, value):
        self._step_threshold.Value = value

    @property
    def IbsWeight(self):
        return self._ibs_weight.Value

    @IbsWeight.setter
    def IbsWeight(self, value):
        self._ibs_weight.Value = value

    @property
    def RsiWeight(self):
        return self._rsi_weight.Value

    @RsiWeight.setter
    def RsiWeight(self, value):
        self._rsi_weight.Value = value

    @property
    def CciWeight(self):
        return self._cci_weight.Value

    @CciWeight.setter
    def CciWeight(self, value):
        self._cci_weight.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def EnableLongOpen(self):
        return self._enable_long_open.Value

    @EnableLongOpen.setter
    def EnableLongOpen(self, value):
        self._enable_long_open.Value = value

    @property
    def EnableShortOpen(self):
        return self._enable_short_open.Value

    @EnableShortOpen.setter
    def EnableShortOpen(self, value):
        self._enable_short_open.Value = value

    @property
    def EnableLongClose(self):
        return self._enable_long_close.Value

    @EnableLongClose.setter
    def EnableLongClose(self, value):
        self._enable_long_close.Value = value

    @property
    def EnableShortClose(self):
        return self._enable_short_close.Value

    @EnableShortClose.setter
    def EnableShortClose(self, value):
        self._enable_short_close.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    def OnStarted2(self, time):
        super(ibs_rsi_cci_v4_strategy, self).OnStarted2(time)

        self.Volume = float(self.OrderVolume)
        self._has_signal = False
        self._last_signal = 0.0
        self._signal_history = []
        self._baseline_history = []

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._ibs_avg = SimpleMovingAverage()
        self._ibs_avg.Length = max(1, int(self.IbsPeriod))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._cci, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        cci_val = float(cci_value)

        if not self._rsi.IsFormed or not self._cci.IsFormed:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        candle_range = high - low
        if candle_range == 0.0:
            step = 0.0001
            if self.Security is not None and self.Security.PriceStep is not None:
                step = float(self.Security.PriceStep)
            if step == 0.0:
                step = 0.0001
            candle_range = step

        ibs_raw = (close - low) / candle_range
        ibs_result = process_float(self._ibs_avg, Decimal(ibs_raw), candle.OpenTime, True)
        if not ibs_result.IsFinal:
            return

        ibs_smoothed = float(ibs_result)

        ibs_w = float(self.IbsWeight)
        cci_w = float(self.CciWeight)
        rsi_w = float(self.RsiWeight)

        composite = ((ibs_smoothed - 0.5) * ibs_w + cci_val * cci_w + (rsi_val - 50.0) * rsi_w) / 3.0
        adjusted = self._apply_step_constraint(composite)

        self._signal_history.append(adjusted)
        range_period = max(1, int(self.RangePeriod))
        signal_bar = int(self.SignalBar)
        smooth_period = max(1, int(self.SmoothPeriod))
        max_hist = max(2, max(range_period, signal_bar + 2) + smooth_period)
        if len(self._signal_history) > max_hist:
            self._signal_history.pop(0)

        if len(self._signal_history) < range_period:
            return

        start_idx = max(0, len(self._signal_history) - range_period)
        highest = max(self._signal_history[start_idx:])
        lowest = min(self._signal_history[start_idx:])
        baseline = (highest + lowest) / 2.0

        self._baseline_history.append(baseline)
        if len(self._baseline_history) > max_hist:
            self._baseline_history.pop(0)

        hist_len = min(len(self._signal_history), len(self._baseline_history))
        if hist_len <= signal_bar:
            return

        prev_idx = hist_len - 1 - max(0, signal_bar)
        prev_signal = self._signal_history[prev_idx]
        prev_baseline = self._baseline_history[prev_idx]
        cur_signal = self._signal_history[hist_len - 1]
        cur_baseline = self._baseline_history[hist_len - 1]

        position = self.Position

        if position > 0 and self.EnableLongClose and prev_signal < prev_baseline:
            self.SellMarket()
            position = 0
        elif position < 0 and self.EnableShortClose and prev_signal > prev_baseline:
            self.BuyMarket()
            position = 0

        if self.EnableLongOpen and position <= 0 and prev_signal > prev_baseline and cur_signal <= cur_baseline:
            self.BuyMarket()
        elif self.EnableShortOpen and position >= 0 and prev_signal < prev_baseline and cur_signal >= cur_baseline:
            self.SellMarket()

    def _apply_step_constraint(self, target):
        if not self._has_signal:
            self._last_signal = target
            self._has_signal = True
            return self._last_signal

        threshold = abs(float(self.StepThreshold))
        if threshold <= 0.0:
            self._last_signal = target
            return self._last_signal

        diff = target - self._last_signal
        if abs(diff) > threshold:
            direction = 1.0 if diff > 0.0 else -1.0
            self._last_signal = target - direction * threshold
        else:
            self._last_signal = target

        return self._last_signal

    def OnReseted(self):
        super(ibs_rsi_cci_v4_strategy, self).OnReseted()
        self._has_signal = False
        self._last_signal = 0.0
        self._signal_history = []
        self._baseline_history = []

    def CreateClone(self):
        return ibs_rsi_cci_v4_strategy()
