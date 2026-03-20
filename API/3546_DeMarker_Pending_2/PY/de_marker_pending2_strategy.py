import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class de_marker_pending2_strategy(Strategy):
    def __init__(self):
        super(de_marker_pending2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._demarker_period = self.Param("DemarkerPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._demarker_upper_level = self.Param("DemarkerUpperLevel", 0.7) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._demarker_lower_level = self.Param("DemarkerLowerLevel", 0.3) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._rsi = None
        self._prev_oscillator = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(de_marker_pending2_strategy, self).OnReseted()
        self._rsi = None
        self._prev_oscillator = None

    def OnStarted(self, time):
        super(de_marker_pending2_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.demarker_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return de_marker_pending2_strategy()
