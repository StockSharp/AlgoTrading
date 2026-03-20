import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class symbol_swap_panel_strategy(Strategy):
    def __init__(self):
        super(symbol_swap_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle series for monitoring and signals", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("Candle Type", "Candle series for monitoring and signals", "General")

        self._sma = None
        self._entry_price = 0.0
        self._prev_close = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(symbol_swap_panel_strategy, self).OnReseted()
        self._sma = None
        self._entry_price = 0.0
        self._prev_close = 0.0

    def OnStarted(self, time):
        super(symbol_swap_panel_strategy, self).OnStarted(time)

        self.__sma = SimpleMovingAverage()
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
        return symbol_swap_panel_strategy()
