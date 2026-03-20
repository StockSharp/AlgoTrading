import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class outlier_detector_with_n_sigma_confidence_intervals_strategy(Strategy):
    def __init__(self):
        super(outlier_detector_with_n_sigma_confidence_intervals_strategy, self).__init__()
        self._sample_size = self.Param("SampleSize", 50) \
            .SetGreaterThanZero()
        self._n_sigma = self.Param("NSigma", 2.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(outlier_detector_with_n_sigma_confidence_intervals_strategy, self).OnReseted()
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(outlier_detector_with_n_sigma_confidence_intervals_strategy, self).OnStarted(time)
        self._last_signal_ticks = 0
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._sample_size.Value
        self._std = StandardDeviation()
        self._std.Length = self._sample_size.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._std, self.OnProcess).Start()

    def OnProcess(self, candle, avg, std):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed or not self._std.IsFormed:
            return
        av = float(avg)
        sd = float(std)
        close = float(candle.ClosePrice)
        if sd <= 0:
            return
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks < cooldown_ticks:
            return
        ns = float(self._n_sigma.Value)
        upper = av + ns * sd
        lower = av - ns * sd
        if close > upper and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_ticks = current_ticks
        elif close < lower and self.Position >= 0:
            self.SellMarket()
            self._last_signal_ticks = current_ticks

    def CreateClone(self):
        return outlier_detector_with_n_sigma_confidence_intervals_strategy()
