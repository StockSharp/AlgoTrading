import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math
from StockSharp.Messages import DataType
from StockSharp.Algo.Strategies import Strategy


class zakryvator_strategy(Strategy):
    def __init__(self):
        super(zakryvator_strategy, self).__init__()
        self._min001002 = self.Param("Min001002", 4.0) \
            .SetDisplay("Loss <=0.02", "Max loss for volume <=0.02 lots", "Risk")
        self._min002005 = self.Param("Min002005", 8.0) \
            .SetDisplay("Loss 0.02-0.05", "Max loss for volume 0.02-0.05 lots", "Risk")
        self._min00501 = self.Param("Min00501", 10.0) \
            .SetDisplay("Loss 0.05-0.10", "Max loss for volume 0.05-0.10 lots", "Risk")
        self._min0103 = self.Param("Min0103", 15.0) \
            .SetDisplay("Loss 0.10-0.30", "Max loss for volume 0.10-0.30 lots", "Risk")
        self._min0305 = self.Param("Min0305", 20.0) \
            .SetDisplay("Loss 0.30-0.50", "Max loss for volume 0.30-0.50 lots", "Risk")
        self._min051 = self.Param("Min051", 25.0) \
            .SetDisplay("Loss 0.50-1", "Max loss for volume 0.50-1 lots", "Risk")
        self._min_from1 = self.Param("MinFrom1", 30.0) \
            .SetDisplay("Loss >1", "Max loss for volume above 1 lot", "Risk")
        self._entry_price = 0.0

    @property
    def min001002(self):
        return self._min001002.Value

    @property
    def min002005(self):
        return self._min002005.Value

    @property
    def min00501(self):
        return self._min00501.Value

    @property
    def min0103(self):
        return self._min0103.Value

    @property
    def min0305(self):
        return self._min0305.Value

    @property
    def min051(self):
        return self._min051.Value

    @property
    def min_from1(self):
        return self._min_from1.Value

    def OnStarted2(self, time):
        super(zakryvator_strategy, self).OnStarted2(time)
        self.SubscribeTicks().Bind(self.process_trade).Start()

    def process_trade(self, trade):
        if self.Position == 0:
            self._entry_price = 0.0
            return

        price = float(trade.Price)

        if self._entry_price == 0.0:
            self._entry_price = price

        open_pnl = float(self.Position) * (price - self._entry_price)

        if open_pnl >= 0.0:
            return

        volume = abs(float(self.Position))

        if volume <= 0.02:
            threshold = float(self.min001002)
        elif volume <= 0.05:
            threshold = float(self.min002005)
        elif volume <= 0.10:
            threshold = float(self.min00501)
        elif volume <= 0.30:
            threshold = float(self.min0103)
        elif volume <= 0.50:
            threshold = float(self.min0305)
        elif volume <= 1.0:
            threshold = float(self.min051)
        else:
            threshold = float(self.min_from1)

        if open_pnl <= -threshold:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

    def CreateClone(self):
        return zakryvator_strategy()
