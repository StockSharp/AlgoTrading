import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class mnist_pattern_classifier_strategy(Strategy):
    def __init__(self):
        super(mnist_pattern_classifier_strategy, self).__init__()

        self._lookback_period = self.Param("LookbackPeriod", 14) \
            .SetDisplay("Lookback", "Number of candles converted into the pattern grid", "Pattern")
        self._target_class = self.Param("TargetClass", 1) \
            .SetDisplay("Lookback", "Number of candles converted into the pattern grid", "Pattern")
        self._confidence_threshold = self.Param("ConfidenceThreshold", 0.2) \
            .SetDisplay("Lookback", "Number of candles converted into the pattern grid", "Pattern")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Lookback", "Number of candles converted into the pattern grid", "Pattern")

        self._rsi = null!
        self._atr = null!
        self._close_window = new()
        self._first_close = 0.0
        self._previous_close = 0.0
        self._last_class = -1
        self._last_confidence = 0.0
        self._cooldown = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mnist_pattern_classifier_strategy, self).OnReseted()
        self._rsi = null!
        self._atr = null!
        self._close_window = new()
        self._first_close = 0.0
        self._previous_close = 0.0
        self._last_class = -1
        self._last_confidence = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(mnist_pattern_classifier_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.lookback_period
        self.__atr = AverageTrueRange()
        self.__atr.Length = self.lookback_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self.__atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mnist_pattern_classifier_strategy()
