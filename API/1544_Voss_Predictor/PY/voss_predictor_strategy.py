import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class voss_predictor_strategy(Strategy):
    def __init__(self):
        super(voss_predictor_strategy, self).__init__()
        self._period_bandpass = self.Param("PeriodBandpass", 20) \
            .SetDisplay("Bandpass Period", "Period for band-pass filter", "Settings")
        self._band_width = self.Param("BandWidth", 0.25) \
            .SetDisplay("Bandwidth", "Bandwidth coefficient", "Settings")
        self._bars_prediction = self.Param("BarsPrediction", 3.0) \
            .SetDisplay("Bars of Prediction", "Look ahead bars", "Settings")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._price_prev1 = 0.0
        self._price_prev2 = 0.0
        self._band_pass_prev1 = 0.0
        self._band_pass_prev2 = 0.0
        self._prev_vpf = 0.0
        self._prev_bpf = 0.0

    @property
    def period_bandpass(self):
        return self._period_bandpass.Value

    @property
    def band_width(self):
        return self._band_width.Value

    @property
    def bars_prediction(self):
        return self._bars_prediction.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(voss_predictor_strategy, self).OnReseted()
        self._price_prev1 = 0.0
        self._price_prev2 = 0.0
        self._band_pass_prev1 = 0.0
        self._band_pass_prev2 = 0.0
        self._prev_vpf = 0.0
        self._prev_bpf = 0.0

    def OnStarted(self, time):
        super(voss_predictor_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def CreateClone(self):
        return voss_predictor_strategy()
