import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class close_agent_strategy(Strategy):
    def __init__(self):
        super(close_agent_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe used for indicators", "General")
        self._rsi_length = self.Param("RsiLength", 13) \
            .SetDisplay("Candle Type", "Timeframe used for indicators", "General")
        self._bollinger_length = self.Param("BollingerLength", 21) \
            .SetDisplay("Candle Type", "Timeframe used for indicators", "General")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("Candle Type", "Timeframe used for indicators", "General")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("Candle Type", "Timeframe used for indicators", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(close_agent_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(close_agent_strategy, self).OnStarted(time)

        self._sma_fast = SimpleMovingAverage()
        self._sma_fast.Length = 10
        self._sma_slow = SimpleMovingAverage()
        self._sma_slow.Length = 30

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma_fast, self._sma_slow, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return close_agent_strategy()
