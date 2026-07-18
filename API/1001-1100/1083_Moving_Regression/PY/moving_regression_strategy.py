import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class moving_regression_strategy(Strategy):
    """
    Moving regression: linear regression slope on close prices for trend direction.
    """

    def __init__(self):
        super(moving_regression_strategy, self).__init__()
        self._degree = self.Param("Degree", 2).SetDisplay("Degree", "Sensitivity multiplier", "General")
        self._window = self.Param("Window", 20).SetDisplay("Window", "Regression window", "General")
        self._cooldown_bars = self.Param("CooldownBars", 8).SetDisplay("Cooldown", "Min bars between entries", "General")
        self._slope_threshold = self.Param("SlopeThresholdPercent", 0.005).SetDisplay("Slope Threshold %", "Min slope pct", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prices = []
        self._bar_index = 0
        self._last_signal_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_regression_strategy, self).OnReseted()
        self._prices = []
        self._bar_index = 0
        self._last_signal_bar = -1000000

    def OnStarted2(self, time):
        super(moving_regression_strategy, self).OnStarted2(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = 10
        ema2 = ExponentialMovingAverage()
        ema2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        close = float(candle.ClosePrice)
        self._prices.append(close)
        win = self._window.Value
        if len(self._prices) > win:
            self._prices = self._prices[-win:]
        if len(self._prices) < win:
            return
        slope = self._calc_slope(self._prices)
        slope_pct = slope / close * 100.0 if close != 0 else 0.0
        threshold = float(self._slope_threshold.Value) * (1.0 + self._degree.Value * 0.05)
        can_signal = self._bar_index - self._last_signal_bar >= self._cooldown_bars.Value
        if can_signal and slope_pct > threshold and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_bar = self._bar_index
        elif can_signal and slope_pct < -threshold and self.Position >= 0:
            self.SellMarket()
            self._last_signal_bar = self._bar_index

    @staticmethod
    def _calc_slope(prices):
        n = len(prices)
        if n < 2:
            return 0.0
        sum_x = 0.0
        sum_y = 0.0
        sum_xx = 0.0
        sum_xy = 0.0
        for i in range(n):
            x = float(i)
            y = prices[i]
            sum_x += x
            sum_y += y
            sum_xx += x * x
            sum_xy += x * y
        denom = n * sum_xx - sum_x * sum_x
        if denom == 0:
            return 0.0
        return (n * sum_xy - sum_x * sum_y) / denom

    def CreateClone(self):
        return moving_regression_strategy()
