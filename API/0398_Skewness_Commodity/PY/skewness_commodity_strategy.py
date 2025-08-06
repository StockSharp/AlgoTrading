import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class skewness_commodity_strategy(Strategy):
    """Placeholder for skewness commodity strategy."""

    def __init__(self):
        super(skewness_commodity_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def CreateClone(self):
        return skewness_commodity_strategy()
