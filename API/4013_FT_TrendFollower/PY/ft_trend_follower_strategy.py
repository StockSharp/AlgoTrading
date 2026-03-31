import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy

class ft_trend_follower_strategy(Strategy):
    """
    FT Trend Follower: EMA crossover with MACD confirmation.
    Buys when fast EMA crosses above slow EMA and MACD > 0.
    Sells when fast EMA crosses below slow EMA and MACD < 0.
    """

    def __init__(self):
        super(ft_trend_follower_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_macd = 0.0
        self._is_ready = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ft_trend_follower_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_macd = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(ft_trend_follower_strategy, self).OnStarted2(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_macd = 0.0
        self._is_ready = False

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        macd = MovingAverageConvergenceDivergence()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, macd_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)
        macd_val = float(macd_val)

        if not self._is_ready:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._prev_macd = macd_val
            self._is_ready = True
            return

        if self._prev_fast <= self._prev_slow and fast_val > slow_val and macd_val > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and macd_val < 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._prev_macd = macd_val

    def CreateClone(self):
        return ft_trend_follower_strategy()
