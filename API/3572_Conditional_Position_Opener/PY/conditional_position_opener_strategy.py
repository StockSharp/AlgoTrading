import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class conditional_position_opener_strategy(Strategy):
    def __init__(self):
        super(conditional_position_opener_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for signal generation", "General")
        self._momentum_period = self.Param("MomentumPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe for signal generation", "General")

        self._momentum = None
        self._prev_momentum = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(conditional_position_opener_strategy, self).OnReseted()
        self._momentum = None
        self._prev_momentum = None

    def OnStarted(self, time):
        super(conditional_position_opener_strategy, self).OnStarted(time)

        self.__momentum = Momentum()
        self.__momentum.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__momentum, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return conditional_position_opener_strategy()
