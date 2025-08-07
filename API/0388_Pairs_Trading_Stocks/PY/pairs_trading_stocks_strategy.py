import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *


class pairs_trading_stocks_strategy(Strategy):
    """Simplified pairs trading strategy for stocks based on price ratio z-score."""

    def __init__(self):
        super().__init__()
        self._pairs = self.Param("Pairs", Array.Empty[Security]()) \
            .SetDisplay("Pairs", "Securities for pairs (even indices paired with odd)", "General")
        self._window = self.Param("WindowDays", 60) \
            .SetDisplay("Window Days", "Rolling window size in days", "General")
        self._entryZ = self.Param("EntryZ", 2.0) \
            .SetDisplay("Entry Z", "Entry z-score threshold", "General")
        self._exitZ = self.Param("ExitZ", 0.5) \
            .SetDisplay("Exit Z", "Exit z-score threshold", "General")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "General")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._hist = {}
        self._latest = {}

    @property
    def pairs(self):
        return self._pairs.Value

    @pairs.setter
    def pairs(self, value):
        self._pairs.Value = value

    @property
    def candle_type(self):
        return self._tf.Value

    @candle_type.setter
    def candle_type(self, value):
        self._tf.Value = value

    def GetWorkingSecurities(self):
        # Convert .NET Array to Python list
        securities_list = list(self.pairs) if self.pairs is not None else []
        
        # Return all unique securities
        for sec in securities_list:
            yield sec, self.candle_type

    def get_pairs_tuples(self):
        """Create pairs from securities list: (0,1), (2,3), (4,5), ..."""
        securities_list = list(self.pairs) if self.pairs is not None else []
        
        # Create pairs from adjacent elements
        pairs = []
        for i in range(0, len(securities_list) - 1, 2):
            if i + 1 < len(securities_list):
                pairs.append((securities_list[i], securities_list[i + 1]))
        
        return pairs

    def OnReseted(self):
        super().OnReseted()
        self._hist.clear()
        self._latest.clear()

    def OnStarted(self, time):
        if not self.pairs or len(self.pairs) < 2:
            raise Exception("At least 2 securities must be set for pairs")
        super().OnStarted(time)
        
        # Initialize history for each pair
        for pair in self.get_pairs_tuples():
            a, b = pair
            self._hist[pair] = []
        
        # Subscribe to data for all securities
        for sec, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, sec).Bind(lambda c, s=sec: self._process(c, s)).Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_daily()

    def _on_daily(self):
        for pair in self.get_pairs_tuples():
            a, b = pair
            priceA = self._latest.get(a, 0)
            priceB = self._latest.get(b, 0)
            if priceA == 0 or priceB == 0:
                continue
            r = priceA / priceB
            w = self._hist[pair]
            if len(w) == self._window.Value:
                w.pop(0)
            w.append(r)
            if len(w) < self._window.Value:
                continue
            mean = sum(w) / len(w)
            sigma = Math.Sqrt(sum((x - mean) ** 2 for x in w) / len(w))
            if sigma == 0:
                continue
            z = (r - mean) / sigma
            if abs(z) < self._exitZ.Value:
                self._move(a, 0)
                self._move(b, 0)
                continue
            port = self.Portfolio.CurrentValue or 0.0
            notional = port / 2
            if z > self._entryZ.Value:
                self._move(a, -notional / priceA)
                self._move(b, notional / priceB)
            elif z < -self._entryZ.Value:
                self._move(a, notional / priceA)
                self._move(b, -notional / priceB)

    def _move(self, sec, tgt):
        diff = tgt - self._pos(sec)
        price = self._latest.get(sec, 0)
        if price <= 0 or abs(diff) * price < self._min_usd.Value:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="Pairs"))

    def _pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return pairs_trading_stocks_strategy()
