import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class pairs_trading_country_etfs_strategy(Strategy):
    """Mean-reversion pairs trade between two country ETFs."""

    def __init__(self):
        super().__init__()
        self._univ = self.Param("Universe", list()) \
            .SetDisplay("Universe", "Pair of ETFs", "General")
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
        self._a = None
        self._b = None
        self._ratio = []
        self._latest = {}
        self._last = None

    @property
    def universe(self):
        return self._univ.Value

    @universe.setter
    def universe(self, value):
        self._univ.Value = value

    @property
    def candle_type(self):
        return self._tf.Value

    @candle_type.setter
    def candle_type(self, value):
        self._tf.Value = value

    def GetWorkingSecurities(self):
        if len(self.universe) != 2:
            raise Exception("Universe must contain exactly two ETFs")
        self._a, self._b = self.universe
        yield self._a, self.candle_type
        yield self._b, self.candle_type

    def OnReseted(self):
        super().OnReseted()
        self._a = self._b = None
        self._ratio = []
        self._latest.clear()
        self._last = None

    def OnStarted(self, time):
        if self.universe is None or len(self.universe) != 2:
            raise Exception("Universe must contain exactly two ETFs")
        super().OnStarted(time)
        self._a, self._b = self.universe
        self.SubscribeCandles(self.candle_type, True, self._a).Bind(lambda c, s=self._a: self._process(c, s)).Start()
        self.SubscribeCandles(self.candle_type, True, self._b).Bind(lambda c, s=self._b: self._process(c, s)).Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        d = candle.OpenTime.Date
        if self._last == d:
            return
        self._last = d
        self._on_daily()

    def _on_daily(self):
        pxA = self._latest.get(self._a, 0)
        pxB = self._latest.get(self._b, 0)
        if pxA == 0 or pxB == 0:
            return
        r = pxA / pxB
        if len(self._ratio) == self._window.Value:
            self._ratio.pop(0)
        self._ratio.append(r)
        if len(self._ratio) < self._window.Value:
            return
        mean = sum(self._ratio) / len(self._ratio)
        sigma = Math.Sqrt(sum((x - mean) ** 2 for x in self._ratio) / len(self._ratio))
        if sigma == 0:
            return
        z = (r - mean) / sigma
        if abs(z) < self._exitZ.Value:
            self._move(self._a, 0)
            self._move(self._b, 0)
            return
        if z > self._entryZ.Value:
            self._hedge(-1)
        elif z < -self._entryZ.Value:
            self._hedge(1)

    def _hedge(self, dir):
        equity = self.Portfolio.CurrentValue or 0.0
        priceA = self._latest.get(self._a, 0)
        priceB = self._latest.get(self._b, 0)
        if priceA <= 0 or priceB <= 0:
            return
        qtyA = equity / 2 / priceA * dir
        qtyB = equity / 2 / priceB * -dir
        self._move(self._a, qtyA)
        self._move(self._b, qtyB)

    def _move(self, sec, tgt):
        diff = tgt - self._pos(sec)
        price = self._latest.get(sec, 0)
        if price <= 0 or abs(diff) * price < self._min_usd.Value:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order, Security
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="PairsETF"))

    def _pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return pairs_trading_country_etfs_strategy()
