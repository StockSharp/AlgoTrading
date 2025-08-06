import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class paired_switching_strategy(Strategy):
    """Each quarter holds the ETF with higher previous-quarter return."""

    def __init__(self):
        super().__init__()
        self._second = self.Param("SecondETF", None) \
            .SetDisplay("Second ETF", "Second exchange-traded fund", "General")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum trade value", "General")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Candles time frame", "General")
        self._p1 = RollingWin(64)
        self._p2 = RollingWin(64)
        self._latest = {}
        self._last = None

    @property
    def second_etf(self):
        return self._second.Value

    @second_etf.setter
    def second_etf(self, value):
        self._second.Value = value

    @property
    def min_trade_usd(self):
        return self._min_usd.Value

    @min_trade_usd.setter
    def min_trade_usd(self, value):
        self._min_usd.Value = value

    @property
    def candle_type(self):
        return self._tf.Value

    @candle_type.setter
    def candle_type(self, value):
        self._tf.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.second_etf, self.candle_type)]

    def OnReseted(self):
        super().OnReseted()
        self._p1.clear()
        self._p2.clear()
        self._latest.clear()
        self._last = None

    def OnStarted(self, time):
        if self.Security is None or self.second_etf is None:
            raise Exception("FirstETF and SecondETF must be set")
        super().OnStarted(time)
        self.SubscribeCandles(self.candle_type, True, self.Security) \
            .Bind(lambda c, s=True: self._process(c, self.Security, True)) \
            .Start()
        self.SubscribeCandles(self.candle_type, True, self.second_etf) \
            .Bind(lambda c, s=False: self._process(c, self.second_etf, False)) \
            .Start()

    def _process(self, candle, sec, first):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_daily(first, candle)

    def _on_daily(self, first, candle):
        (self._p1 if first else self._p2).add(candle.ClosePrice)
        d = candle.OpenTime.Date
        if self._last == d:
            return
        self._last = d
        if not (d.Month % 3 == 1 and d.Day == 1):
            return
        self._rebalance()

    def _rebalance(self):
        if not self._p1.full or not self._p2.full:
            return
        r1 = (self._p1.data[0] - self._p1.data[-1]) / self._p1.data[-1]
        r2 = (self._p2.data[0] - self._p2.data[-1]) / self._p2.data[-1]
        long_etf = self.Security if r1 > r2 else self.second_etf
        other = self.second_etf if r1 > r2 else self.Security
        port = self.Portfolio.CurrentValue or 0.0
        price_long = self._latest.get(long_etf, 0)
        if price_long > 0:
            self._move(long_etf, port / price_long)
        self._move(other, 0)

    def _move(self, sec, tgt):
        diff = tgt - self._pos(sec)
        price = self._latest.get(sec, 0)
        if price <= 0 or abs(diff) * price < self.min_trade_usd:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="PairSwitch"))

    def _pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return paired_switching_strategy()


class RollingWin:
    def __init__(self, n):
        from collections import deque
        self._n = n
        self._q = deque()

    def add(self, v):
        if len(self._q) == self._n:
            self._q.popleft()
        self._q.append(v)

    @property
    def full(self):
        return len(self._q) == self._n

    @property
    def data(self):
        return list(self._q)

    def clear(self):
        self._q.clear()
