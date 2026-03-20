import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class secwenta_multi_bar_signals_strategy(Strategy):
    def __init__(self):
        super(secwenta_multi_bar_signals_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 15) \
            .SetDisplay("Fast Period", "Fast SmoothedMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 60) \
            .SetDisplay("Fast Period", "Fast SmoothedMA period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Fast Period", "Fast SmoothedMA period", "Indicator")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Fast Period", "Fast SmoothedMA period", "Indicator")

        self._fast = None
        self._slow = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnReseted(self):
        super(secwenta_multi_bar_signals_strategy, self).OnReseted()
        self._fast = None
        self._slow = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(secwenta_multi_bar_signals_strategy, self).OnStarted(time)

        self.__fast = SmoothedMovingAverage()
        self.__fast.Length = self.fast_period
        self.__slow = SmoothedMovingAverage()
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
        return secwenta_multi_bar_signals_strategy()
