import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, InvalidOperationException

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class spreader2_strategy(Strategy):
    """Pair trading spread strategy. Requires two securities."""

    def __init__(self):
        super(spreader2_strategy, self).__init__()

        self._primary_volume_param = self.Param("PrimaryVolume", 1.0)
        self._target_profit_param = self.Param("TargetProfit", 100.0)
        self._shift_param = self.Param("ShiftLength", 6)
        self._candle_type_param = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._day_bars_param = self.Param("DayBars", 288)

    @property
    def PrimaryVolume(self):
        return self._primary_volume_param.Value

    @property
    def TargetProfit(self):
        return self._target_profit_param.Value

    @property
    def ShiftLength(self):
        return self._shift_param.Value

    @property
    def DayBars(self):
        return self._day_bars_param.Value

    @property
    def CandleType(self):
        return self._candle_type_param.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type_param.Value = value

    def OnStarted2(self, time):
        super(spreader2_strategy, self).OnStarted2(time)

        raise InvalidOperationException("Second security is not specified.")

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # Pair trading logic requires two securities; cannot run in single-security mode.

    def OnReseted(self):
        super(spreader2_strategy, self).OnReseted()

    def CreateClone(self):
        return spreader2_strategy()
