import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class linear_regression_all_data_strategy(Strategy):
    """
    Linear regression using all available bars. Trades based on
    deviation from regression line as percentage of predicted value.
    """

    def __init__(self):
        super(linear_regression_all_data_strategy, self).__init__()
        self._max_bars_back = self.Param("MaxBarsBack", 5000) \
            .SetDisplay("Max Bars Back", "Maximum number of bars for drawing", "General")
        self._deviation_threshold = self.Param("DeviationThreshold", 0.008) \
            .SetDisplay("Deviation Threshold", "Deviation to trigger trade", "General")
        self._exit_threshold = self.Param("ExitThreshold", 0.002) \
            .SetDisplay("Exit Threshold", "Deviation to close position", "General")
        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._index = 0
        self._sum_x = 0.0
        self._sum_y = 0.0
        self._sum_x2 = 0.0
        self._sum_xy = 0.0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(linear_regression_all_data_strategy, self).OnReseted()
        self._index = 0
        self._sum_x = 0.0
        self._sum_y = 0.0
        self._sum_x2 = 0.0
        self._sum_xy = 0.0
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(linear_regression_all_data_strategy, self).OnStarted(time)

        self._index = 0
        self._sum_x = 0.0
        self._sum_y = 0.0
        self._sum_x2 = 0.0
        self._sum_xy = 0.0
        self._bars_from_signal = self._cooldown_bars.Value

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

        self._index += 1
        x = float(self._index)
        y = float(candle.ClosePrice)

        self._sum_x += x
        self._sum_y += y
        self._sum_x2 += x * x
        self._sum_xy += x * y

        if self._index < 20:
            return

        n = float(self._index)
        denom = n * self._sum_x2 - self._sum_x * self._sum_x
        if denom == 0:
            return

        slope = (n * self._sum_xy - self._sum_x * self._sum_y) / denom
        intercept = (self._sum_y - slope * self._sum_x) / n
        predicted = slope * x + intercept

        if predicted == 0:
            return

        deviation = (y - predicted) / predicted
        self._bars_from_signal += 1

        if self._bars_from_signal < self._cooldown_bars.Value:
            return

        dev_th = self._deviation_threshold.Value
        exit_th = self._exit_threshold.Value

        if self.Position == 0:
            if deviation <= -dev_th:
                self.BuyMarket()
                self._bars_from_signal = 0
            elif deviation >= dev_th:
                self.SellMarket()
                self._bars_from_signal = 0
            return

        if self.Position > 0 and deviation >= -exit_th:
            self.SellMarket()
            self._bars_from_signal = 0
        elif self.Position < 0 and deviation <= exit_th:
            self.BuyMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return linear_regression_all_data_strategy()
