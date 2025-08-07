import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array
from StockSharp.Messages import DataType
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class small_cap_premium_strategy(Strategy):
    """Placeholder for small-cap premium strategy."""

    def __init__(self):
        super(small_cap_premium_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def CreateClone(self):
        return small_cap_premium_strategy()
