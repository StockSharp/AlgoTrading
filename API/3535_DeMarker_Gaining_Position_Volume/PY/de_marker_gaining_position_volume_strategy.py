import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class de_marker_gaining_position_volume_strategy(Strategy):
    def __init__(self):
        super(de_marker_gaining_position_volume_strategy, self).__init__()

        self._de_marker_period = self.Param("DeMarkerPeriod", 14) \
            .SetDisplay("DeMarker Period", "Number of bars used by the oscillator.", "Indicator")
        self._upper_level = self.Param("UpperLevel", 0.7) \
            .SetDisplay("DeMarker Period", "Number of bars used by the oscillator.", "Indicator")
        self._lower_level = self.Param("LowerLevel", 0.3) \
            .SetDisplay("DeMarker Period", "Number of bars used by the oscillator.", "Indicator")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("DeMarker Period", "Number of bars used by the oscillator.", "Indicator")

        self._rsi = None
        self._prev_oscillator = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(de_marker_gaining_position_volume_strategy, self).OnReseted()
        self._rsi = None
        self._prev_oscillator = None

    def OnStarted(self, time):
        super(de_marker_gaining_position_volume_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.de_marker_period

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
        return de_marker_gaining_position_volume_strategy()
