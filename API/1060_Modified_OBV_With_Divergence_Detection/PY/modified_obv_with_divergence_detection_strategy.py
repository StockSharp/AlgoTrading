import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import OnBalanceVolume, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class modified_obv_with_divergence_detection_strategy(Strategy):
    def __init__(self):
        super(modified_obv_with_divergence_detection_strategy, self).__init__()
        self._obv_ma_length = self.Param("ObvMaLength", 7) \
            .SetGreaterThanZero()
        self._signal_length = self.Param("SignalLength", 10) \
            .SetGreaterThanZero()
        self._min_cross_gap_percent = self.Param("MinCrossGapPercent", 0.2) \
            .SetGreaterThanZero()
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._was_below_signal = False
        self._is_initialized = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(modified_obv_with_divergence_detection_strategy, self).OnReseted()
        self._was_below_signal = False
        self._is_initialized = False
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(modified_obv_with_divergence_detection_strategy, self).OnStarted(time)
        self._is_initialized = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._obv = OnBalanceVolume()
        self._obv_ma = ExponentialMovingAverage()
        self._obv_ma.Length = self._obv_ma_length.Value
        self._signal_ma = ExponentialMovingAverage()
        self._signal_ma.Length = self._signal_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._obv, self.OnProcess).Start()

    def OnProcess(self, candle, obv_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._obv.IsFormed:
            return
        obvm_result = self._obv_ma.Process(obv_value)
        obvm = float(obvm_result.ToDecimal())
        signal_result = self._signal_ma.Process(obvm_result)
        signal = float(signal_result.ToDecimal())
        if not self._obv_ma.IsFormed or not self._signal_ma.IsFormed:
            return
        if not self._is_initialized:
            self._was_below_signal = obvm < signal
            self._is_initialized = True
            return
        is_below = obvm < signal
        denominator = abs(signal) + 1.0
        gap_percent = abs(obvm - signal) / denominator * 100.0
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        min_gap = float(self._min_cross_gap_percent.Value)
        if self._bars_from_signal >= cd and gap_percent >= min_gap and self._was_below_signal and not is_below and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and gap_percent >= min_gap and not self._was_below_signal and is_below and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._was_below_signal = is_below

    def CreateClone(self):
        return modified_obv_with_divergence_detection_strategy()
