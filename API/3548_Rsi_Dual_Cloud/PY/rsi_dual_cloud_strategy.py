import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_dual_cloud_strategy(Strategy):
    def __init__(self):
        super(rsi_dual_cloud_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for RSI calculations", "General")
        self._fast_length = self.Param("FastLength", 14) \
            .SetDisplay("Candle Type", "Timeframe for RSI calculations", "General")
        self._slow_length = self.Param("SlowLength", 42) \
            .SetDisplay("Candle Type", "Timeframe for RSI calculations", "General")

        self._fast_rsi = None
        self._slow_rsi = None
        self._prev_fast = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_dual_cloud_strategy, self).OnReseted()
        self._fast_rsi = None
        self._slow_rsi = None
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(rsi_dual_cloud_strategy, self).OnStarted(time)

        self.__fast_rsi = RelativeStrengthIndex()
        self.__fast_rsi.Length = self.fast_length
        self.__slow_rsi = RelativeStrengthIndex()
        self.__slow_rsi.Length = self.slow_length

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
        return rsi_dual_cloud_strategy()
