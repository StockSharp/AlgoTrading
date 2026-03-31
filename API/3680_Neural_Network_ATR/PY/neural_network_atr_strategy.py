import clr
import math
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class neural_network_atr_strategy(Strategy):
    def __init__(self):
        super(neural_network_atr_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._atr_period = self.Param("AtrPeriod", 14)
        self._buy_threshold = self.Param("BuyThreshold", 0.502)
        self._sell_threshold = self.Param("SellThreshold", 0.498)
        self._hidden_layer_size = self.Param("HiddenLayerSize", 5)
        self._input_size = self.Param("InputSize", 5)
        self._feature_clamp = self.Param("FeatureClamp", 1.0)
        self._initial_learning_rate = self.Param("InitialLearningRate", 0.01)

        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_open = 0.0
        self._prev_volume = 0.0
        self._has_prev = False
        self._weights_ih = []
        self._bias_h = []
        self._weights_ho = []
        self._bias_o = 0.0
        self._learning_rate = 0.01

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def BuyThreshold(self):
        return self._buy_threshold.Value

    @BuyThreshold.setter
    def BuyThreshold(self, value):
        self._buy_threshold.Value = value

    @property
    def SellThreshold(self):
        return self._sell_threshold.Value

    @SellThreshold.setter
    def SellThreshold(self, value):
        self._sell_threshold.Value = value

    @property
    def HiddenLayerSize(self):
        return self._hidden_layer_size.Value

    @HiddenLayerSize.setter
    def HiddenLayerSize(self, value):
        self._hidden_layer_size.Value = value

    @property
    def InputSize(self):
        return self._input_size.Value

    @InputSize.setter
    def InputSize(self, value):
        self._input_size.Value = value

    @property
    def FeatureClamp(self):
        return self._feature_clamp.Value

    @FeatureClamp.setter
    def FeatureClamp(self, value):
        self._feature_clamp.Value = value

    @property
    def InitialLearningRate(self):
        return self._initial_learning_rate.Value

    @InitialLearningRate.setter
    def InitialLearningRate(self, value):
        self._initial_learning_rate.Value = value

    def _init_network(self):
        rng = random.Random(42)
        inp = self.InputSize
        hid = max(1, self.HiddenLayerSize)
        self._weights_ih = [rng.uniform(-0.05, 0.05) for _ in range(inp * hid)]
        self._bias_h = [rng.uniform(-0.05, 0.05) for _ in range(hid)]
        self._weights_ho = [rng.uniform(-0.05, 0.05) for _ in range(hid)]
        self._bias_o = rng.uniform(-0.05, 0.05)
        self._learning_rate = float(self.InitialLearningRate)

    def _normalize(self, value):
        clamp = float(self.FeatureClamp)
        if value > clamp:
            value = clamp
        elif value < -clamp:
            value = -clamp
        return (value + clamp) / (2.0 * clamp)

    def _sigmoid(self, x):
        return 1.0 / (1.0 + math.exp(-x))

    def _predict(self, inputs):
        hid = max(1, self.HiddenLayerSize)
        inp_size = self.InputSize
        hidden = [0.0] * hid
        for j in range(hid):
            act = self._bias_h[j]
            for i in range(inp_size):
                act += inputs[i] * self._weights_ih[i * hid + j]
            hidden[j] = max(0.0, act)
        output = self._bias_o
        for j in range(hid):
            output += hidden[j] * self._weights_ho[j]
        pred = self._sigmoid(output)
        adjusted = pred * (1.0 + self._learning_rate)
        return max(0.0, min(1.0, adjusted))

    def OnReseted(self):
        super(neural_network_atr_strategy, self).OnReseted()
        self._has_prev = False
        self._init_network()

    def OnStarted2(self, time):
        super(neural_network_atr_strategy, self).OnStarted2(time)
        self._has_prev = False
        self._init_network()

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        vol = float(candle.TotalVolume)
        atr_val = float(atr_value)

        if not self._has_prev:
            self._prev_close = close
            self._prev_open = open_p
            self._prev_high = high
            self._prev_low = low
            self._prev_volume = vol
            self._has_prev = True
            return

        # Build inputs
        price_change = (close - self._prev_close) / self._prev_close if self._prev_close != 0 else 0.0
        candle_range = (high - low) / close if close != 0 else 0.0
        body = (close - open_p) / (high - low) if high != low else 0.0
        vol_change = (vol - self._prev_volume) / self._prev_volume if self._prev_volume > 0 else 0.0
        atr_norm = atr_val / close if close != 0 else 0.0

        inputs = [
            self._normalize(price_change),
            self._normalize(candle_range),
            self._normalize(body),
            self._normalize(vol_change),
            self._normalize(atr_norm)
        ]

        prediction = self._predict(inputs)
        buy_thresh = float(self.BuyThreshold)
        sell_thresh = float(self.SellThreshold)

        if prediction >= buy_thresh and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif prediction <= sell_thresh and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_open = open_p
        self._prev_high = high
        self._prev_low = low
        self._prev_volume = vol

    def CreateClone(self):
        return neural_network_atr_strategy()
