import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class tst_strategy(Strategy):
    def __init__(self):
        super(tst_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._pullback_multiplier = self.Param("PullbackMultiplier", 0.5) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._stop_multiplier = self.Param("StopMultiplier", 2.0) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._take_multiplier = self.Param("TakeMultiplier", 1.0) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tst_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted(self, time):
        super(tst_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length

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
        return tst_strategy()
