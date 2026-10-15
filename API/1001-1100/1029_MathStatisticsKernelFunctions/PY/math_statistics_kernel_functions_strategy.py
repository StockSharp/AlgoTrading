import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class math_statistics_kernel_functions_strategy(Strategy):
    def __init__(self):
        super(math_statistics_kernel_functions_strategy, self).__init__()
        self._kernel = self.Param("Kernel", "uniform") \
            .SetDisplay("Kernel", "Kernel function name", "General")
        self._bandwidth = self.Param("Bandwidth", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Bandwidth", "Kernel bandwidth", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 20) \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._bar_index = 0
        self._last_trade_bar = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(math_statistics_kernel_functions_strategy, self).OnReseted()
        self._bar_index = 0
        self._last_trade_bar = -1

    def OnStarted2(self, time):
        super(math_statistics_kernel_functions_strategy, self).OnStarted2(time)
        self._bar_index = 0
        self._last_trade_bar = -1
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def _uniform(self, d, bw):
        return 0.0 if abs(d) > bw else 0.5

    def _triangular(self, d, bw):
        return 0.0 if abs(d) > bw else 1.0 - abs(d / bw)

    def _epanechnikov(self, d, bw):
        if abs(d) > bw:
            return 0.0
        r = d / bw
        return 0.25 * (1.0 - r * r)

    def _quartic(self, d, bw):
        if abs(d) > bw:
            return 0.0
        r = d / bw
        inner = 1.0 - r * r
        return 0.9375 * inner * inner

    def _triweight(self, d, bw):
        if abs(d) > bw:
            return 0.0
        r = d / bw
        inner = 1.0 - r * r
        return (35.0 / 32.0) * inner * inner * inner

    def _tricubic(self, d, bw):
        if abs(d) > bw:
            return 0.0
        r = abs(d) / bw
        inner = 1.0 - r * r * r
        return (70.0 / 81.0) * inner * inner * inner

    def _gaussian(self, d, bw):
        x = d / bw
        return (1.0 / math.sqrt(2.0 * math.pi)) * math.exp(-0.5 * x * x)

    def _cosine(self, d, bw):
        if abs(d) > bw:
            return 0.0
        x = d / bw
        return (math.pi / 4.0) * math.cos(math.pi / 2.0 * x)

    def _logistic(self, d, bw):
        x = d / bw
        return 1.0 / (math.exp(x) + 2.0 + math.exp(-x))

    def _sigmoid_k(self, d, bw):
        x = d / bw
        return (2.0 / math.pi) * (1.0 / (math.exp(x) + math.exp(-x)))

    def _select(self, kernel, d, bw):
        if kernel == "uniform":
            return self._uniform(d, bw)
        elif kernel == "triangle":
            return self._triangular(d, bw)
        elif kernel == "epanechnikov":
            return self._epanechnikov(d, bw)
        elif kernel == "quartic":
            return self._quartic(d, bw)
        elif kernel == "triweight":
            return self._triweight(d, bw)
        elif kernel == "tricubic":
            return self._tricubic(d, bw)
        elif kernel == "gaussian":
            return self._gaussian(d, bw)
        elif kernel == "cosine":
            return self._cosine(d, bw)
        elif kernel == "logistic":
            return self._logistic(d, bw)
        elif kernel == "sigmoid":
            return self._sigmoid_k(d, bw)
        return 0.0

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        test = -1.0 + (self._bar_index % 400) * 0.005
        self._bar_index += 1
        bw = float(self._bandwidth.Value)
        kernel = str(self._kernel.Value)
        value = self._select(kernel, test, bw)
        cd = self._signal_cooldown_bars.Value
        can_trade = self._last_trade_bar < 0 or self._bar_index - self._last_trade_bar >= cd
        if can_trade and value > 0.5 and self.Position <= 0:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif can_trade and value < 0.5 and self.Position >= 0:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

    def CreateClone(self):
        return math_statistics_kernel_functions_strategy()
