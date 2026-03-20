import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class eugene_strategy(Strategy):
    def __init__(self):
        super(eugene_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 200) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")

        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eugene_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    def OnStarted(self, time):
        super(eugene_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return eugene_strategy()
