import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class nrtr_reversal_strategy(Strategy):
    def __init__(self):
        super(nrtr_reversal_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for trailing", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2) \
            .SetDisplay("ATR Period", "ATR period for trailing", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1) \
            .SetDisplay("ATR Period", "ATR period for trailing", "Indicators")

        self._trail_line = 0.0
        self._extreme = 0.0
        self._trend = 0.0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(nrtr_reversal_strategy, self).OnReseted()
        self._trail_line = 0.0
        self._extreme = 0.0
        self._trend = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(nrtr_reversal_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return nrtr_reversal_strategy()
