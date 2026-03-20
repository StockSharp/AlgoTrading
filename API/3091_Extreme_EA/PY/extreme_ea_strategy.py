import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class extreme_ea_strategy(Strategy):
    def __init__(self):
        super(extreme_ea_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 50) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicator")
        self._slow_ma_period = self.Param("SlowMaPeriod", 200) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicator")
        self._cci_period = self.Param("CciPeriod", 12) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicator")
        self._cci_upper_level = self.Param("CciUpperLevel", 50) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicator")
        self._cci_lower_level = self.Param("CciLowerLevel", -50) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicator")

        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_fast2 = 0.0
        self._prev_slow2 = 0.0
        self._has_prev = False

    def OnReseted(self):
        super(extreme_ea_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_fast2 = 0.0
        self._prev_slow2 = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(extreme_ea_strategy, self).OnStarted(time)

        self.__fast_ma = ExponentialMovingAverage()
        self.__fast_ma.Length = self.fast_ma_period
        self.__slow_ma = ExponentialMovingAverage()
        self.__slow_ma.Length = self.slow_ma_period

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
        return extreme_ea_strategy()
