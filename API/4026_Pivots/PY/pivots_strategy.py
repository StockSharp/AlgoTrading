import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class pivots_strategy(Strategy):
    def __init__(self):
        super(pivots_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Timeframe for signal generation", "General")

        self._pivot_level = 0.0
        self._previous_close = None
        self._entry_price = None
        self._pivot_ready = False
        self._daily_highs = new()
        self._daily_lows = new()
        self._daily_closes = new()
        self._current_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pivots_strategy, self).OnReseted()
        self._pivot_level = 0.0
        self._previous_close = None
        self._entry_price = None
        self._pivot_ready = False
        self._daily_highs = new()
        self._daily_lows = new()
        self._daily_closes = new()
        self._current_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._day_close = 0.0

    def OnStarted(self, time):
        super(pivots_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = 2

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return pivots_strategy()
