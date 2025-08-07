import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class pairs_trading_stocks_strategy(Strategy):
    """Simplified pairs trading strategy for stocks based on price ratio z-score."""

    def __init__(self):
        super().__init__()
        self._pairs = self.Param("Pairs", list()) \
            .SetDisplay("Pairs", "Pairs of securities", "General")
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
        for a, b in self.pairs:
            yield a, self.candle_type
            yield b, self.candle_type

    def OnReseted(self):
        super().OnReseted()
        self._hist.clear()
        self._latest.clear()

    def OnStarted(self, time):
        if not self.pairs:
            raise Exception("Pairs must be set")
        super().OnStarted(time)
        for a, b in self.pairs:
            self._hist[(a, b)] = []
            self.SubscribeCandles(self.candle_type, True, a).Bind(lambda c, s=a: self._process(c, s)).Start()
            self.SubscribeCandles(self.candle_type, True, b).Bind(lambda c, s=b: self._process(c, s)).Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_daily()

    def _on_daily(self):
        for pair in self.pairs:
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
        from StockSharp.BusinessEntities import Order, Security
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="Pairs"))

    def _pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return pairs_trading_stocks_strategy()
