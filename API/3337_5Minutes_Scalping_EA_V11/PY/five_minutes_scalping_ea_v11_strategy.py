import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class five_minutes_scalping_ea_v11_strategy(Strategy):
    def __init__(self):
        super(five_minutes_scalping_ea_v11_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 6) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(five_minutes_scalping_ea_v11_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(five_minutes_scalping_ea_v11_strategy, self).OnStarted(time)

        self._fast = WeightedMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = WeightedMovingAverage()
        self._slow.Length = self.slow_period

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
        return five_minutes_scalping_ea_v11_strategy()
