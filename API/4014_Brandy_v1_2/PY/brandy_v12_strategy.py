import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy


class brandy_v12_strategy(Strategy):
    def __init__(self):
        super(brandy_v12_strategy, self).__init__()

        self._long_period = self.Param("LongPeriod", 70) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")
        self._long_shift = self.Param("LongShift", 5) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")
        self._short_period = self.Param("ShortPeriod", 20) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")
        self._short_shift = self.Param("ShortShift", 5) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 150) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")

        self._long_sma = None
        self._short_sma = None
        self._long_history = new()
        self._short_history = new()
        self._entry_price = None
        self._stop_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(brandy_v12_strategy, self).OnReseted()
        self._long_sma = None
        self._short_sma = None
        self._long_history = new()
        self._short_history = new()
        self._entry_price = None
        self._stop_price = None

    def OnStarted(self, time):
        super(brandy_v12_strategy, self).OnStarted(time)

        self.__long_sma = SMA()
        self.__long_sma.Length = self.long_period
        self.__short_sma = SMA()
        self.__short_sma.Length = self.short_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__long_sma, self.__short_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return brandy_v12_strategy()
