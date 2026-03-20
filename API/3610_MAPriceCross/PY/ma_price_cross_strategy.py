import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_price_cross_strategy(Strategy):
    def __init__(self):
        super(ma_price_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for MA cross detection", "General")
        self._ma_period = self.Param("MaPeriod", 100) \
            .SetDisplay("Candle Type", "Timeframe for MA cross detection", "General")

        self._sma = None
        self._prev_average = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_price_cross_strategy, self).OnReseted()
        self._sma = None
        self._prev_average = None
        self._prev_close = None

    def OnStarted(self, time):
        super(ma_price_cross_strategy, self).OnStarted(time)

        self.__sma = ExponentialMovingAverage()
        self.__sma.Length = self.ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ma_price_cross_strategy()
