import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class alexav_speed_up_m1_body_break_strategy(Strategy):
    def __init__(self):
        super(alexav_speed_up_m1_body_break_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for body threshold", "Indicators")
        self._body_multiplier = self.Param("BodyMultiplier", 1.0) \
            .SetDisplay("ATR Period", "ATR period for body threshold", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("ATR Period", "ATR period for body threshold", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(alexav_speed_up_m1_body_break_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(alexav_speed_up_m1_body_break_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return alexav_speed_up_m1_body_break_strategy()
