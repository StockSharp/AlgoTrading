import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class five_min_rsi_qualified_strategy(Strategy):
    def __init__(self):
        super(five_min_rsi_qualified_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._qualification_length = self.Param("QualificationLength", 3) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._upper_threshold = self.Param("UpperThreshold", 65) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._lower_threshold = self.Param("LowerThreshold", 35) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")

        self._overbought_count = 0.0
        self._oversold_count = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(five_min_rsi_qualified_strategy, self).OnReseted()
        self._overbought_count = 0.0
        self._oversold_count = 0.0

    def OnStarted(self, time):
        super(five_min_rsi_qualified_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return five_min_rsi_qualified_strategy()
