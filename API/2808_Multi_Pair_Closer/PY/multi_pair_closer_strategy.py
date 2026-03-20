import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class multi_pair_closer_strategy(Strategy):
    def __init__(self):
        super(multi_pair_closer_strategy, self).__init__()

        self._profit_target = self.Param("ProfitTarget", 5) \
            .SetDisplay("Profit Target", "Close position when floating profit reaches this value", "Risk Management")
        self._max_loss = self.Param("MaxLoss", 10) \
            .SetDisplay("Profit Target", "Close position when floating profit reaches this value", "Risk Management")
        self._min_age_seconds = self.Param("MinAgeSeconds", 60) \
            .SetDisplay("Profit Target", "Close position when floating profit reaches this value", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Profit Target", "Close position when floating profit reaches this value", "Risk Management")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("Profit Target", "Close position when floating profit reaches this value", "Risk Management")

        self._sma = None
        self._entry_price = 0.0
        self._entry_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_pair_closer_strategy, self).OnReseted()
        self._sma = None
        self._entry_price = 0.0
        self._entry_time = None

    def OnStarted(self, time):
        super(multi_pair_closer_strategy, self).OnStarted(time)

        self.__sma = SimpleMovingAverage()
        self.__sma.Length = self.sma_period

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
        return multi_pair_closer_strategy()
