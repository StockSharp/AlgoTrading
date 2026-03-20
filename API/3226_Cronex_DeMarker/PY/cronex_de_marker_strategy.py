import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cronex_de_marker_strategy(Strategy):
    def __init__(self):
        super(cronex_de_marker_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")

        self._fast = None
        self._slow = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnReseted(self):
        super(cronex_de_marker_strategy, self).OnReseted()
        self._fast = None
        self._slow = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(cronex_de_marker_strategy, self).OnStarted(time)

        self.__fast = ExponentialMovingAverage()
        self.__fast.Length = self.fast_period
        self.__slow = ExponentialMovingAverage()
        self.__slow.Length = self.slow_period

        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cronex_de_marker_strategy()
