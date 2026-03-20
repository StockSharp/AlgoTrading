import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class moving_average_crossover_spread_strategy(Strategy):
    def __init__(self):
        super(moving_average_crossover_spread_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Candle Type", "Candle type", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_crossover_spread_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(moving_average_crossover_spread_strategy, self).OnStarted(time)

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.fast_period
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return moving_average_crossover_spread_strategy()
