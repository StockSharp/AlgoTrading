import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class wpr_custom_cloud_simple_strategy(Strategy):
    def __init__(self):
        super(wpr_custom_cloud_simple_strategy, self).__init__()

        self._wpr_period = self.Param("WprPeriod", 20) \
            .SetDisplay("WPR Period", "Williams %R lookback length", "Williams %R")
        self._overbought_level = self.Param("OverboughtLevel", -10) \
            .SetDisplay("WPR Period", "Williams %R lookback length", "Williams %R")
        self._oversold_level = self.Param("OversoldLevel", -90) \
            .SetDisplay("WPR Period", "Williams %R lookback length", "Williams %R")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("WPR Period", "Williams %R lookback length", "Williams %R")

        self._williams_r = None
        self._previous_wpr = None
        self._older_wpr = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wpr_custom_cloud_simple_strategy, self).OnReseted()
        self._williams_r = None
        self._previous_wpr = None
        self._older_wpr = None

    def OnStarted(self, time):
        super(wpr_custom_cloud_simple_strategy, self).OnStarted(time)

        self.__williams_r = WilliamsR()
        self.__williams_r.Length = self.wpr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__williams_r, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return wpr_custom_cloud_simple_strategy()
