import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class small_inside_bar_strategy(Strategy):
    def __init__(self):
        super(small_inside_bar_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Time frame used for pattern detection", "General")
        self._range_ratio_threshold = self.Param("RangeRatioThreshold", 2.25) \
            .SetDisplay("Candle Type", "Time frame used for pattern detection", "General")
        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Candle Type", "Time frame used for pattern detection", "General")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Candle Type", "Time frame used for pattern detection", "General")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Candle Type", "Time frame used for pattern detection", "General")
        self._open_mode = self.Param("OpenMode", SmallInsideBarOpenModes.SwingWithRefill) \
            .SetDisplay("Candle Type", "Time frame used for pattern detection", "General")

        self._previous_candle = None
        self._two_back_candle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(small_inside_bar_strategy, self).OnReseted()
        self._previous_candle = None
        self._two_back_candle = None

    def OnStarted(self, time):
        super(small_inside_bar_strategy, self).OnStarted(time)


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
        return small_inside_bar_strategy()
