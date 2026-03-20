import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mean_reversion_momentum_strategy(Strategy):
    def __init__(self):
        super(mean_reversion_momentum_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._bars_to_count = self.Param("BarsToCount", 5) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._rsi = None
        self._close_history = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mean_reversion_momentum_strategy, self).OnReseted()
        self._rsi = None
        self._close_history = new()

    def OnStarted(self, time):
        super(mean_reversion_momentum_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mean_reversion_momentum_strategy()
