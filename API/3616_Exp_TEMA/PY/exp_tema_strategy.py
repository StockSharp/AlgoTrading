import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_tema_strategy(Strategy):
    def __init__(self):
        super(exp_tema_strategy, self).__init__()

        self._tema_period = self.Param("TemaPeriod", 40) \
            .SetDisplay("TEMA Period", "Length of Triple Exponential Moving Average", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("TEMA Period", "Length of Triple Exponential Moving Average", "Indicators")

        self._tema = None
        self._prev1 = None
        self._prev2 = None
        self._prev3 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_tema_strategy, self).OnReseted()
        self._tema = None
        self._prev1 = None
        self._prev2 = None
        self._prev3 = None

    def OnStarted(self, time):
        super(exp_tema_strategy, self).OnStarted(time)

        self.__tema = ExponentialMovingAverage()
        self.__tema.Length = self.tema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__tema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return exp_tema_strategy()
