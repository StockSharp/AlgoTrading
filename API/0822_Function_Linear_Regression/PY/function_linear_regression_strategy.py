import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class function_linear_regression_strategy(Strategy):
    """
    FunctionLinearRegression: EMA crossover strategy.
    Buys when fast EMA crosses above slow EMA, sells on reverse.
    """

    def __init__(self):
        super(function_linear_regression_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 120)             .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 450)             .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))             .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(function_linear_regression_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(function_linear_regression_strategy, self).OnStarted(time)

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast_v = float(fast_val)
        slow_v = float(slow_val)

        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:


            self.BuyMarket()


        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:


            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return function_linear_regression_strategy()
