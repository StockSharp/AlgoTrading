import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class over_hedge_v2_grid_strategy(Strategy):
    def __init__(self):
        super(over_hedge_v2_grid_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._oversold = self.Param("Oversold", 30) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._overbought = self.Param("Overbought", 70) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(over_hedge_v2_grid_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(over_hedge_v2_grid_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return over_hedge_v2_grid_strategy()
