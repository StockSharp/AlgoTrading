import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class doji_trader_breakout_strategy(Strategy):
    def __init__(self):
        super(doji_trader_breakout_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Indicators")
        self._doji_ratio = self.Param("DojiRatio", 0.25) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Indicators")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(doji_trader_breakout_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._has_prev = False

    def OnStarted(self, time):
        super(doji_trader_breakout_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period

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
        return doji_trader_breakout_strategy()
