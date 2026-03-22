import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class linear_regression_channel_strategy(Strategy):
    """
    Trades pullbacks using a linear regression channel.
    Buys in uptrend when price dips below lower band, sells in downtrend above upper.
    """

    def __init__(self):
        super(linear_regression_channel_strategy, self).__init__()
        self._length = self.Param("Length", 50) \
            .SetDisplay("Length", "Bars for regression", "Parameters")
        self._deviation = self.Param("Deviation", 1.5) \
            .SetDisplay("Deviation", "Channel width multiplier", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._closes = []
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(linear_regression_channel_strategy, self).OnReseted()
        self._closes = []
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(linear_regression_channel_strategy, self).OnStarted(time)
        self._bars_from_signal = self._cooldown_bars.Value
        self._closes = []

        dummy_ema1 = ExponentialMovingAverage()
        dummy_ema1.Length = 10
        dummy_ema2 = ExponentialMovingAverage()
        dummy_ema2.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(dummy_ema1, dummy_ema2, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return

        length = self._length.Value
        close = float(candle.ClosePrice)

        self._closes.append(close)
        if len(self._closes) > length:
            self._closes.pop(0)

        if len(self._closes) < length:
            return

        n = len(self._closes)
        sum_x = 0.0
        sum_y = 0.0
        sum_xy = 0.0
        sum_x2 = 0.0

        for i in range(n):
            x = float(i)
            y = self._closes[i]
            sum_x += x
            sum_y += y
            sum_xy += x * y
            sum_x2 += x * x

        denom = n * sum_x2 - sum_x * sum_x
        if denom == 0:
            return

        slope = (n * sum_xy - sum_x * sum_y) / denom
        intercept = (sum_y - slope * sum_x) / n

        last_x = n - 1
        line = intercept + slope * last_x

        dev_sum = 0.0
        for i in range(n):
            fitted = intercept + slope * float(i)
            diff = self._closes[i] - fitted
            dev_sum += diff * diff
        std_dev = math.sqrt(dev_sum / n)

        mult = self._deviation.Value
        upper = line + std_dev * mult
        lower = line - std_dev * mult
        self._bars_from_signal += 1

        if self._bars_from_signal < self._cooldown_bars.Value:
            return

        if slope > 0 and close < lower and self.Position == 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif slope < 0 and close > upper and self.Position == 0:
            self.SellMarket()
            self._bars_from_signal = 0
        elif self.Position > 0 and close >= line:
            self.SellMarket()
            self._bars_from_signal = 0
        elif self.Position < 0 and close <= line:
            self.BuyMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return linear_regression_channel_strategy()
