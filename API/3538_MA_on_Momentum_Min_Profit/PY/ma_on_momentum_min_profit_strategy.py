import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class ma_on_momentum_min_profit_strategy(Strategy):
    def __init__(self):
        super(ma_on_momentum_min_profit_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Type of candles used for the momentum calculation", "General")
        self._momentum_period = self.Param("MomentumPeriod", 20) \
            .SetDisplay("Candle Type", "Type of candles used for the momentum calculation", "General")
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetDisplay("Candle Type", "Type of candles used for the momentum calculation", "General")

        self._momentum = None
        self._momentum_history = new()
        self._prev_momentum = None
        self._prev_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_on_momentum_min_profit_strategy, self).OnReseted()
        self._momentum = None
        self._momentum_history = new()
        self._prev_momentum = None
        self._prev_signal = None

    def OnStarted(self, time):
        super(ma_on_momentum_min_profit_strategy, self).OnStarted(time)

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
        return ma_on_momentum_min_profit_strategy()
