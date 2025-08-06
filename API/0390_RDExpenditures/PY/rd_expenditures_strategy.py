import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class rd_expenditures_strategy(Strategy):
    """Long stocks with high R&D-to-market-value ratio and short low ones."""

    def __init__(self):
        super().__init__()
        self._univ = self.Param("Universe", list()) \
            .SetDisplay("Universe", "Securities to trade", "General")
        self._quint = self.Param("Quintile", 5) \
            .SetDisplay("Quintile", "Number of quintiles", "General")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "General")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._weights = {}
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

    @property
    def min_trade_usd(self):
        return self._min_usd.Value

    @min_trade_usd.setter
    def min_trade_usd(self, value):
        self._min_usd.Value = value

    def GetWorkingSecurities(self):
        for s in self.universe:
            yield s, self.candle_type

    def OnReseted(self):
        super().OnReseted()
        self._weights.clear()
        self._latest.clear()
        self._last = None

    def OnStarted(self, time):
        if not self.universe:
            raise Exception("Universe must not be empty")
        super().OnStarted(time)
        trig = self.universe[0]
        self.SubscribeCandles(self.candle_type, True, trig).Bind(lambda c, s=trig: self._process(c, s)).Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_daily(candle.OpenTime.Date)

    def _on_daily(self, d):
        if self._last == d:
            return
        self._last = d
        if d.Day != 1:
            return
        self._rebalance()

    def _rebalance(self):
        ratio = {}
        for s in self.universe:
            ok, r = self._try_get_ratio(s)
            if ok:
                ratio[s] = r
        if len(ratio) < self._quint.Value * 2:
            return
        q = len(ratio) // self._quint.Value
        longs = sorted(ratio.items(), key=lambda kv: kv[1], reverse=True)[:q]
        shorts = sorted(ratio.items(), key=lambda kv: kv[1])[:q]
        self._weights.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s, _ in longs:
            self._weights[s] = wl
        for s, _ in shorts:
            self._weights[s] = ws
        for pos in list(self.Positions):
            if pos.Security not in self._weights:
                self._move(pos.Security, 0)
        port = self.Portfolio.CurrentValue or 0.0
        for sec, w in self._weights.items():
            price = self._latest.get(sec, 0)
            if price > 0:
                self._move(sec, w * port / price)

    def _move(self, sec, tgt):
        diff = tgt - self._pos(sec)
        price = self._latest.get(sec, 0)
        if price <= 0 or abs(diff) * price < self.min_trade_usd:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="RDmom"))

    def _pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def _try_get_ratio(self, sec):
        # placeholder
        return False, 0

    def CreateClone(self):
        return rd_expenditures_strategy()
