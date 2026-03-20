import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class symbol_swap_strategy(Strategy):
    def __init__(self):
        super(symbol_swap_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Candle series for signals", "General")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("Candle Type", "Candle series for signals", "General")
        self._spread_threshold = self.Param("SpreadThreshold", 3) \
            .SetDisplay("Candle Type", "Candle series for signals", "General")

        self._sma = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(symbol_swap_strategy, self).OnReseted()
        self._sma = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(symbol_swap_strategy, self).OnStarted(time)

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
        return symbol_swap_strategy()
