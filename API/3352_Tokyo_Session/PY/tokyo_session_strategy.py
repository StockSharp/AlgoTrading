import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class tokyo_session_strategy(Strategy):
    def __init__(self):
        super(tokyo_session_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tokyo_session_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(tokyo_session_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return tokyo_session_strategy()
