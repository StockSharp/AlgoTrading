import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
import math
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
        self._price_prev1 = None
        self._price_prev2 = None
        self._band_pass_prev1 = 0.0
        self._band_pass_prev2 = 0.0
        self._prev_vpf = 0.0
        self._prev_bpf = 0.0
        self._voss_buffer = [0.0] * 9

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
        self._price_prev1 = None
        self._price_prev2 = None
        self._band_pass_prev1 = 0.0
        self._band_pass_prev2 = 0.0
        self._prev_vpf = 0.0
        self._prev_bpf = 0.0
        self._voss_buffer = [0.0] * 9

    def OnStarted2(self, time):
        super(voss_predictor_strategy, self).OnStarted2(time)
        alpha = 2.0 * math.pi / float(self.period_bandpass)
        self._cos_alpha = math.cos(alpha)
        gamma = math.cos(alpha * float(self.band_width))
        delta = 1.0 / gamma - math.sqrt(1.0 / (gamma * gamma) - 1.0)
        self._delta_dec = delta
        self._order = int(3.0 * min(3.0, float(self.bars_prediction)))
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        prev2 = self._price_prev2 if self._price_prev2 is not None else (self._price_prev1 if self._price_prev1 is not None else price)
        whiten = 0.5 * (price - prev2)
        self._price_prev2 = self._price_prev1
        self._price_prev1 = price
        band_pass = (1.0 - self._delta_dec) * whiten \
            + self._cos_alpha * (1.0 + self._delta_dec) * self._band_pass_prev1 \
            - self._delta_dec * self._band_pass_prev2
        self._band_pass_prev2 = self._band_pass_prev1
        self._band_pass_prev1 = band_pass
        e = 0.0
        for i in range(self._order):
            e += self._voss_buffer[self._order - i - 1] * (1.0 + i) / self._order
        vpf = 0.5 * (3.0 + self._order) * band_pass - e
        i = self._order - 1
        while i > 0:
            self._voss_buffer[i] = self._voss_buffer[i - 1]
            i -= 1
        self._voss_buffer[0] = vpf
        cross_up = self._prev_vpf <= self._prev_bpf and vpf > band_pass
        cross_down = self._prev_vpf >= self._prev_bpf and vpf < band_pass
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()
        self._prev_vpf = vpf
        self._prev_bpf = band_pass

    def CreateClone(self):
        return voss_predictor_strategy()
