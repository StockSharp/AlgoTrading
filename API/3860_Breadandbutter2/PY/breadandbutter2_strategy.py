import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class breadandbutter2_strategy(Strategy):
    def __init__(self):
        super(breadandbutter2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_wma5 = 0.0
        self._prev_wma10 = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(breadandbutter2_strategy, self).OnReseted()
        self._prev_wma5 = 0.0
        self._prev_wma10 = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(breadandbutter2_strategy, self).OnStarted(time)

        self._wma5 = WeightedMovingAverage()
        self._wma5.Length = 5
        self._wma10 = WeightedMovingAverage()
        self._wma10.Length = 10
        self._wma15 = WeightedMovingAverage()
        self._wma15.Length = 15

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._wma5, self._wma10, self._wma15, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return breadandbutter2_strategy()
