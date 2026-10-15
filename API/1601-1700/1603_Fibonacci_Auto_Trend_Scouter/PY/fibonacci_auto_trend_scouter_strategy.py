import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fibonacci_auto_trend_scouter_strategy(Strategy):
    def __init__(self):
        super(fibonacci_auto_trend_scouter_strategy, self).__init__()
        self._small_period = self.Param("SmallPeriod", 8) \
            .SetDisplay("Small Period", "Small EMA period", "General")
        self._medium_period = self.Param("MediumPeriod", 21) \
            .SetDisplay("Medium Period", "Medium EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_small = 0.0
        self._prev_medium = 0.0
        self._is_ready = False

    @property
    def small_period(self):
        return self._small_period.Value

    @property
    def medium_period(self):
        return self._medium_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibonacci_auto_trend_scouter_strategy, self).OnReseted()
        self._prev_small = 0.0
        self._prev_medium = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(fibonacci_auto_trend_scouter_strategy, self).OnStarted2(time)
        ema_small = ExponentialMovingAverage()
        ema_small.Length = self.small_period
        ema_medium = ExponentialMovingAverage()
        ema_medium.Length = self.medium_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_small, ema_medium, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_small)
            self.DrawIndicator(area, ema_medium)
            self.DrawOwnTrades(area)

    def on_process(self, candle, small, medium):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_ready:
            self._prev_small = small
            self._prev_medium = medium
            self._is_ready = True
            return
        # Crossover detection
        cross_up = self._prev_small <= self._prev_medium and small > medium
        cross_down = self._prev_small >= self._prev_medium and small < medium
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()
        self._prev_small = small
        self._prev_medium = medium

    def CreateClone(self):
        return fibonacci_auto_trend_scouter_strategy()
