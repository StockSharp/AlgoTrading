import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class disaster_strategy(Strategy):
    def __init__(self):
        super(disaster_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")

        self._prev_mom = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(disaster_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(disaster_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._mom = Momentum()
        self._mom.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._mom, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return disaster_strategy()
