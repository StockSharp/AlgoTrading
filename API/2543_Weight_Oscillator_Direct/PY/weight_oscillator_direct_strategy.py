import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (RelativeStrengthIndex, MoneyFlowIndex, WilliamsR, DeMarker,
    SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage,
    WeightedMovingAverage, JurikMovingAverage, KaufmanAdaptiveMovingAverage,
    DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy

TREND_DIRECT = 0
TREND_AGAINST = 1

SMOOTH_SIMPLE = 0
SMOOTH_EXPONENTIAL = 1
SMOOTH_SMOOTHED = 2
SMOOTH_WEIGHTED = 3
SMOOTH_JURIK = 4
SMOOTH_KAUFMAN = 5


class weight_oscillator_direct_strategy(Strategy):
    def __init__(self):
        super(weight_oscillator_direct_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._trend_mode = self.Param("TrendMode", TREND_DIRECT)
        self._signal_bar = self.Param("SignalBar", 2)
        self._rsi_weight = self.Param("RsiWeight", 1.0)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._mfi_weight = self.Param("MfiWeight", 1.0)
        self._mfi_period = self.Param("MfiPeriod", 14)
        self._wpr_weight = self.Param("WprWeight", 1.0)
        self._wpr_period = self.Param("WprPeriod", 14)
        self._demarker_weight = self.Param("DeMarkerWeight", 1.0)
        self._demarker_period = self.Param("DeMarkerPeriod", 14)
        self._smoothing_method = self.Param("SmoothingMethod", SMOOTH_JURIK)
        self._smoothing_length = self.Param("SmoothingLength", 10)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._buy_open_enabled = self.Param("BuyOpenEnabled", True)
        self._sell_open_enabled = self.Param("SellOpenEnabled", True)
        self._buy_close_enabled = self.Param("BuyCloseEnabled", True)
        self._sell_close_enabled = self.Param("SellCloseEnabled", True)

        self._oscillator_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TrendMode(self):
        return self._trend_mode.Value

    @TrendMode.setter
    def TrendMode(self, value):
        self._trend_mode.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def RsiWeight(self):
        return self._rsi_weight.Value

    @RsiWeight.setter
    def RsiWeight(self, value):
        self._rsi_weight.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def MfiWeight(self):
        return self._mfi_weight.Value

    @MfiWeight.setter
    def MfiWeight(self, value):
        self._mfi_weight.Value = value

    @property
    def MfiPeriod(self):
        return self._mfi_period.Value

    @MfiPeriod.setter
    def MfiPeriod(self, value):
        self._mfi_period.Value = value

    @property
    def WprWeight(self):
        return self._wpr_weight.Value

    @WprWeight.setter
    def WprWeight(self, value):
        self._wpr_weight.Value = value

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @WprPeriod.setter
    def WprPeriod(self, value):
        self._wpr_period.Value = value

    @property
    def DeMarkerWeight(self):
        return self._demarker_weight.Value

    @DeMarkerWeight.setter
    def DeMarkerWeight(self, value):
        self._demarker_weight.Value = value

    @property
    def DeMarkerPeriod(self):
        return self._demarker_period.Value

    @DeMarkerPeriod.setter
    def DeMarkerPeriod(self, value):
        self._demarker_period.Value = value

    @property
    def SmoothingMethod(self):
        return self._smoothing_method.Value

    @SmoothingMethod.setter
    def SmoothingMethod(self, value):
        self._smoothing_method.Value = value

    @property
    def SmoothingLength(self):
        return self._smoothing_length.Value

    @SmoothingLength.setter
    def SmoothingLength(self, value):
        self._smoothing_length.Value = value

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

    @property
    def BuyOpenEnabled(self):
        return self._buy_open_enabled.Value

    @BuyOpenEnabled.setter
    def BuyOpenEnabled(self, value):
        self._buy_open_enabled.Value = value

    @property
    def SellOpenEnabled(self):
        return self._sell_open_enabled.Value

    @SellOpenEnabled.setter
    def SellOpenEnabled(self, value):
        self._sell_open_enabled.Value = value

    @property
    def BuyCloseEnabled(self):
        return self._buy_close_enabled.Value

    @BuyCloseEnabled.setter
    def BuyCloseEnabled(self, value):
        self._buy_close_enabled.Value = value

    @property
    def SellCloseEnabled(self):
        return self._sell_close_enabled.Value

    @SellCloseEnabled.setter
    def SellCloseEnabled(self, value):
        self._sell_close_enabled.Value = value

    def _create_smoothing_indicator(self):
        method = int(self.SmoothingMethod)
        length = int(self.SmoothingLength)
        if method == SMOOTH_SIMPLE:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind
        elif method == SMOOTH_EXPONENTIAL:
            ind = ExponentialMovingAverage()
            ind.Length = length
            return ind
        elif method == SMOOTH_SMOOTHED:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif method == SMOOTH_WEIGHTED:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        elif method == SMOOTH_KAUFMAN:
            ind = KaufmanAdaptiveMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = JurikMovingAverage()
            ind.Length = length
            return ind

    def OnStarted2(self, time):
        super(weight_oscillator_direct_strategy, self).OnStarted2(time)

        self._oscillator_history = []

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        self._mfi = MoneyFlowIndex()
        self._mfi.Length = self.MfiPeriod
        self._wpr = WilliamsR()
        self._wpr.Length = self.WprPeriod
        self._demarker = DeMarker()
        self._demarker.Length = self.DeMarkerPeriod
        self._smoothing = self._create_smoothing_indicator()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._mfi, self._wpr, self._demarker, self.ProcessCandle).Start()

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        if step <= Decimal(0):
            step = Decimal(1)

        tp = int(self.TakeProfitPoints)
        sl = int(self.StopLossPoints)
        tp_unit = Unit(Decimal(tp) * step, UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(Decimal(sl) * step, UnitTypes.Absolute) if sl > 0 else None
        self.StartProtection(sl_unit, tp_unit)

    def ProcessCandle(self, candle, rsi_value, mfi_value, wpr_value, demarker_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_w = float(self.RsiWeight)
        mfi_w = float(self.MfiWeight)
        wpr_w = float(self.WprWeight)
        dm_w = float(self.DeMarkerWeight)
        total_weight = rsi_w + mfi_w + wpr_w + dm_w
        if total_weight <= 0.0:
            return

        rsi_val = float(rsi_value)
        mfi_val = float(mfi_value)
        wpr_val = float(wpr_value)
        dm_val = float(demarker_value)

        normalized_wpr = wpr_val + 100.0
        normalized_dm = dm_val * 100.0

        blended = (rsi_w * rsi_val + mfi_w * mfi_val + wpr_w * normalized_wpr + dm_w * normalized_dm) / total_weight

        input_val = DecimalIndicatorValue(self._smoothing, Decimal(blended), candle.OpenTime)
        input_val.IsFinal = True
        smoothed_result = self._smoothing.Process(input_val)
        if not smoothed_result.IsFinal:
            return

        oscillator = float(smoothed_result)

        self._oscillator_history.append(oscillator)
        if len(self._oscillator_history) > 512:
            self._oscillator_history.pop(0)

        signal_bar = int(self.SignalBar)
        required_count = signal_bar + 2
        if len(self._oscillator_history) < required_count:
            return

        current = self._oscillator_history[len(self._oscillator_history) - signal_bar]
        previous = self._oscillator_history[len(self._oscillator_history) - (signal_bar + 1)]
        prior = self._oscillator_history[len(self._oscillator_history) - (signal_bar + 2)]

        rising = previous < prior and current > previous
        falling = previous > prior and current < previous

        trend_mode = int(self.TrendMode)
        if trend_mode == TREND_DIRECT:
            long_signal = rising
            short_signal = falling
        else:
            long_signal = falling
            short_signal = rising

        if long_signal:
            if self.BuyCloseEnabled and self.Position < 0:
                self.BuyMarket()
            if self.BuyOpenEnabled and self.Position <= 0:
                self.BuyMarket()

        if short_signal:
            if self.SellCloseEnabled and self.Position > 0:
                self.SellMarket()
            if self.SellOpenEnabled and self.Position >= 0:
                self.SellMarket()

    def OnReseted(self):
        super(weight_oscillator_direct_strategy, self).OnReseted()
        self._oscillator_history = []

    def CreateClone(self):
        return weight_oscillator_direct_strategy()
