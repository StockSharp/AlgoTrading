import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class rectangle_test_strategy(Strategy):
    def __init__(self):
        super(rectangle_test_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("Fast EMA Period", "Length of the fast EMA", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("Fast EMA Period", "Length of the fast EMA", "Indicators")
        self._range_candles = self.Param("RangeCandles", 10) \
            .SetDisplay("Fast EMA Period", "Length of the fast EMA", "Indicators")
        self._rectangle_size_percent = self.Param("RectangleSizePercent", 10) \
            .SetDisplay("Fast EMA Period", "Length of the fast EMA", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Fast EMA Period", "Length of the fast EMA", "Indicators")

        self._highs = new()
        self._lows = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rectangle_test_strategy, self).OnReseted()
        self._highs = new()
        self._lows = new()

    def OnStarted(self, time):
        super(rectangle_test_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rectangle_test_strategy()
