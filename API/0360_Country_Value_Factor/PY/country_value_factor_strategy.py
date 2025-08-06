import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class country_value_factor_strategy(Strategy):
    """Country value factor strategy based on CAPE ratio."""

    def __init__(self):
        super(country_value_factor_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Trading securities collection", "General")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimal trade size in USD", "General")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    # region Properties
    @property
    def Universe(self):
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_trade_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_trade_usd.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe else []

    def OnReseted(self):
        super(country_value_factor_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(country_value_factor_strategy, self).OnStarted(time)

        if not self.Universe:
            raise Exception("Universe is empty.")

        trigger = self.Universe[0]
        self.SubscribeCandles(self.CandleType, True, trigger) \
            .Bind(lambda c: self.OnDay(c.OpenTime.Date)) \
            .Start()

    def OnDay(self, date):
        # TODO: implement factor logic
        pass

    def CreateClone(self):
        return country_value_factor_strategy()
