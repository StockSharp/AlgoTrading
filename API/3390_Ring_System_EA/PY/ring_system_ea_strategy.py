import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ring_system_ea_strategy(Strategy):
    def __init__(self):
        super(ring_system_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._sma_period = self.Param("SmaPeriod", 30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._roc_period = self.Param("RocPeriod", 10) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._was_bullish = False
        self._has_prev_signal = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ring_system_ea_strategy, self).OnReseted()
        self._was_bullish = False
        self._has_prev_signal = False

    def OnStarted(self, time):
        super(ring_system_ea_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._roc = RateOfChange()
        self._roc.Length = self.roc_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._roc, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ring_system_ea_strategy()
