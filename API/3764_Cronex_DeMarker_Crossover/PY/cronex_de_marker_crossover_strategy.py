import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cronex_de_marker_crossover_strategy(Strategy):
    def __init__(self):
        super(cronex_de_marker_crossover_strategy, self).__init__()

        self._de_marker_period = self.Param("DeMarkerPeriod", 25) \
            .SetDisplay("DeMarker Period", "Length of the DeMarker oscillator", "Indicators")
        self._fast_ma_period = self.Param("FastMaPeriod", 14) \
            .SetDisplay("DeMarker Period", "Length of the DeMarker oscillator", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 25) \
            .SetDisplay("DeMarker Period", "Length of the DeMarker oscillator", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("DeMarker Period", "Length of the DeMarker oscillator", "Indicators")

        self._de_marker = None
        self._fast_ma = None
        self._slow_ma = None
        self._previous_fast = None
        self._previous_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cronex_de_marker_crossover_strategy, self).OnReseted()
        self._de_marker = None
        self._fast_ma = None
        self._slow_ma = None
        self._previous_fast = None
        self._previous_slow = None

    def OnStarted(self, time):
        super(cronex_de_marker_crossover_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__de_marker = DeMarker()
        self.__de_marker.Length = self.de_marker_period
        self.__fast_ma = WeightedMovingAverage()
        self.__fast_ma.Length = self.fast_ma_period
        self.__slow_ma = WeightedMovingAverage()
        self.__slow_ma.Length = self.slow_ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cronex_de_marker_crossover_strategy()
