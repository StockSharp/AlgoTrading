import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Array
from StockSharp.Messages import DataType
from StockSharp.BusinessEntities import Order, Security
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class book_to_market_value_strategy(Strategy):
    """Book-to-market value strategy placeholder."""

    def __init__(self):
        super(book_to_market_value_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Securities to process", "General")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "General")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    # region Properties
    @property
    def Universe(self):
        """Securities universe to analyze."""
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def MinTradeUsd(self):
        """Minimum trade value in USD."""
        return self._min_trade_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_trade_usd.Value = value

    @property
    def CandleType(self):
        """The type of candles to use for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(book_to_market_value_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(book_to_market_value_strategy, self).OnStarted(time)

        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")

        trigger = self.Universe[0]

        self.SubscribeCandles(self.CandleType, True, trigger) \
            .Bind(lambda candle: self.OnDay(candle.OpenTime.Date)) \
            .Start()

    def OnDay(self, date):
        """Placeholder for factor logic executed each day."""
        pass

    def CreateClone(self):
        """Creates a new instance of the strategy."""
        return book_to_market_value_strategy()
