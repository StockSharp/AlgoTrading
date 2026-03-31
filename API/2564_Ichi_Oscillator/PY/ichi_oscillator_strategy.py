import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (Ichimoku, SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage, JurikMovingAverage, KaufmanAdaptiveMovingAverage)
from StockSharp.Algo.Strategies import Strategy

SMOOTH_SIMPLE = 0
SMOOTH_EXPONENTIAL = 1
SMOOTH_SMOOTHED = 2
SMOOTH_WEIGHTED = 3
SMOOTH_JURIK = 4
SMOOTH_KAUFMAN = 5


class ichi_oscillator_strategy(Strategy):
    def __init__(self):
        super(ichi_oscillator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._ichimoku_base_period = self.Param("IchimokuBasePeriod", 22)
        self._smoothing_method = self.Param("Smoothing", SMOOTH_JURIK)
        self._smoothing_length = self.Param("SmoothingLength", 5)
        self._smoothing_phase = self.Param("SmoothingPhase", 15)
        self._signal_bar = self.Param("SignalBar", 1)
        self._buy_entries_enabled = self.Param("BuyEntriesEnabled", True)
        self._sell_entries_enabled = self.Param("SellEntriesEnabled", True)
        self._buy_exits_enabled = self.Param("BuyExitsEnabled", True)
        self._sell_exits_enabled = self.Param("SellExitsEnabled", True)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._order_volume = self.Param("OrderVolume", 1.0)

        self._color_history = []
        self._previous_smoothed = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def IchimokuBasePeriod(self):
        return self._ichimoku_base_period.Value

    @IchimokuBasePeriod.setter
    def IchimokuBasePeriod(self, value):
        self._ichimoku_base_period.Value = value

    @property
    def Smoothing(self):
        return self._smoothing_method.Value

    @Smoothing.setter
    def Smoothing(self, value):
        self._smoothing_method.Value = value

    @property
    def SmoothingLength(self):
        return self._smoothing_length.Value

    @SmoothingLength.setter
    def SmoothingLength(self, value):
        self._smoothing_length.Value = value

    @property
    def SmoothingPhase(self):
        return self._smoothing_phase.Value

    @SmoothingPhase.setter
    def SmoothingPhase(self, value):
        self._smoothing_phase.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def BuyEntriesEnabled(self):
        return self._buy_entries_enabled.Value

    @BuyEntriesEnabled.setter
    def BuyEntriesEnabled(self, value):
        self._buy_entries_enabled.Value = value

    @property
    def SellEntriesEnabled(self):
        return self._sell_entries_enabled.Value

    @SellEntriesEnabled.setter
    def SellEntriesEnabled(self, value):
        self._sell_entries_enabled.Value = value

    @property
    def BuyExitsEnabled(self):
        return self._buy_exits_enabled.Value

    @BuyExitsEnabled.setter
    def BuyExitsEnabled(self, value):
        self._buy_exits_enabled.Value = value

    @property
    def SellExitsEnabled(self):
        return self._sell_exits_enabled.Value

    @SellExitsEnabled.setter
    def SellExitsEnabled(self, value):
        self._sell_exits_enabled.Value = value

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
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    def _create_smoother(self, method, length):
        m = int(method)
        if m == SMOOTH_SIMPLE:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_EXPONENTIAL:
            ind = ExponentialMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_SMOOTHED:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_WEIGHTED:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_KAUFMAN:
            ind = KaufmanAdaptiveMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = JurikMovingAverage()
            ind.Length = length
            return ind

    def OnStarted2(self, time):
        super(ichi_oscillator_strategy, self).OnStarted2(time)

        base_period = int(self.IchimokuBasePeriod)
        tenkan_length = max(1, int(base_period * 0.5))
        kijun_length = max(1, int(base_period * 1.5))
        senkou_b_length = max(1, int(base_period * 3))

        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = tenkan_length
        self._ichimoku.Kijun.Length = kijun_length
        self._ichimoku.SenkouB.Length = senkou_b_length

        self._smoother = self._create_smoother(self.Smoothing, int(self.SmoothingLength))

        self._color_history = []
        self._previous_smoothed = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ichimoku, self.ProcessCandle).Start()

        sl = int(self.StopLossPoints)
        tp = int(self.TakeProfitPoints)
        sl_unit = Unit(sl, UnitTypes.Absolute) if sl > 0 else None
        tp_unit = Unit(tp, UnitTypes.Absolute) if tp > 0 else None
        self.StartProtection(sl_unit, tp_unit)

    def ProcessCandle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA

        if tenkan is None or kijun is None or senkou_a is None:
            return

        tenkan_val = float(tenkan)
        kijun_val = float(kijun)
        senkou_a_val = float(senkou_a)
        close = float(candle.ClosePrice)

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step == 0.0:
            step = 1.0

        markt = close - senkou_a_val
        trend = tenkan_val - kijun_val
        raw_oscillator = (markt - trend) / step

        smooth_result = self._smoother.Process(self._smoother.CreateValue(candle.OpenTime, raw_oscillator))
        if not smooth_result.IsFinal:
            return

        smoothed = float(smooth_result)
        self._update_color_history(smoothed)

        sb = int(self.SignalBar)
        if len(self._color_history) <= sb + 1:
            return

        current_index = len(self._color_history) - 1 - sb
        previous_index = current_index - 1
        if previous_index < 0:
            return

        current_color = self._color_history[current_index]
        previous_color = self._color_history[previous_index]

        buy_open = False
        sell_open = False
        buy_close = False
        sell_close = False

        if previous_color == 0 or previous_color == 3:
            sell_close = self.SellExitsEnabled
            if self.BuyEntriesEnabled and (current_color == 2 or current_color == 1 or current_color == 4):
                buy_open = True

        if previous_color == 4 or previous_color == 1:
            buy_close = self.BuyExitsEnabled
            if self.SellEntriesEnabled and (current_color == 0 or current_color == 1 or current_color == 3):
                sell_open = True

        if buy_close and self.Position > 0:
            self.SellMarket()

        if sell_close and self.Position < 0:
            self.BuyMarket()

        if buy_open and self.Position <= 0:
            self.BuyMarket()

        if sell_open and self.Position >= 0:
            self.SellMarket()

    def _update_color_history(self, smoothed):
        color = 2

        if self._previous_smoothed is not None:
            prev = self._previous_smoothed
            if smoothed > 0.0:
                if prev < smoothed:
                    color = 0
                elif prev > smoothed:
                    color = 1
            elif smoothed < 0.0:
                if prev < smoothed:
                    color = 4
                elif prev > smoothed:
                    color = 3
        else:
            if smoothed > 0.0:
                color = 0
            elif smoothed < 0.0:
                color = 3

        self._color_history.append(color)
        self._previous_smoothed = smoothed

    def OnReseted(self):
        super(ichi_oscillator_strategy, self).OnReseted()
        self._color_history = []
        self._previous_smoothed = None

    def CreateClone(self):
        return ichi_oscillator_strategy()
