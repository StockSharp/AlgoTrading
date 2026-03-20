import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lazy_bot_v1_strategy(Strategy):
    def __init__(self):
        super(lazy_bot_v1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for breakout detection", "General")
        self._lookback = self.Param("Lookback", 30) \
            .SetDisplay("Candle Type", "Timeframe for breakout detection", "General")

        self._highs = new()
        self._lows = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lazy_bot_v1_strategy, self).OnReseted()
        self._highs = new()
        self._lows = new()

    def OnStarted(self, time):
        super(lazy_bot_v1_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return lazy_bot_v1_strategy()
