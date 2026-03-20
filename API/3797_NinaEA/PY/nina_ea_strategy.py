import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class nina_ea_strategy(Strategy):
    def __init__(self):
        super(nina_ea_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetDisplay("ATR Period", "ATR length for SuperTrend", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 1) \
            .SetDisplay("ATR Period", "ATR length for SuperTrend", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("ATR Period", "ATR length for SuperTrend", "Indicators")

        self._previous_trend_up = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(nina_ea_strategy, self).OnReseted()
        self._previous_trend_up = None

    def OnStarted(self, time):
        super(nina_ea_strategy, self).OnStarted(time)

        self._super_trend = SuperTrend()
        self._super_trend.Length = self.atr_period
        self._super_trend.Multiplier = self.atr_multiplier

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._super_trend, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return nina_ea_strategy()
