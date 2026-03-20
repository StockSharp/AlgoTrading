import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_ma_on_rsi_dual_strategy(Strategy):
    def __init__(self):
        super(rsi_ma_on_rsi_dual_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle type", "Candles processed by the strategy.", "General")
        self._fast_rsi_period = self.Param("FastRsiPeriod", 14) \
            .SetDisplay("Candle type", "Candles processed by the strategy.", "General")
        self._slow_rsi_period = self.Param("SlowRsiPeriod", 28) \
            .SetDisplay("Candle type", "Candles processed by the strategy.", "General")
        self._ma_period = self.Param("MaPeriod", 12) \
            .SetDisplay("Candle type", "Candles processed by the strategy.", "General")

        self._fast_rsi = None
        self._slow_rsi = None
        self._fast_rsi_history = new()
        self._slow_rsi_history = new()
        self._previous_fast_ma = None
        self._previous_slow_ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_ma_on_rsi_dual_strategy, self).OnReseted()
        self._fast_rsi = None
        self._slow_rsi = None
        self._fast_rsi_history = new()
        self._slow_rsi_history = new()
        self._previous_fast_ma = None
        self._previous_slow_ma = None

    def OnStarted(self, time):
        super(rsi_ma_on_rsi_dual_strategy, self).OnStarted(time)

        self.__fast_rsi = RelativeStrengthIndex()
        self.__fast_rsi.Length = self.fast_rsi_period
        self.__slow_rsi = RelativeStrengthIndex()
        self.__slow_rsi.Length = self.slow_rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_rsi, self.__slow_rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rsi_ma_on_rsi_dual_strategy()
