import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class mutual_fund_momentum_strategy(Strategy):
    """Quarterly momentum rotation among mutual funds."""

    def __init__(self):
        super().__init__()
        self._funds = self.Param("Funds", list()) \
            .SetDisplay("Funds", "List of mutual funds", "Universe")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min USD", "Minimum trade value", "Risk")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._latest = {}
        self._last_day = None

    @property
    def funds(self):
        return self._funds.Value

    @funds.setter
    def funds(self, value):
        self._funds.Value = value

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
        return [(f, self.candle_type) for f in self.funds]

    def OnReseted(self):
        super().OnReseted()
        self._latest.clear()
        self._last_day = None

    def OnStarted(self, time):
        super().OnStarted(time)
        if not self.funds:
            raise Exception("Funds cannot be empty")
        trig = self.funds[0]
        self.SubscribeCandles(self.candle_type, True, trig) \
            .Bind(lambda c, s=trig: self._process(c, s)) \
            .Start()

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        d = candle.OpenTime.Date
        if self._last_day == d:
            return
        self._last_day = d
        if self._is_quarter_day(d):
            self._rebalance()

    def _is_quarter_day(self, d):
        return d.month % 3 == 0 and d.day <= 3

    def _rebalance(self):
        perf = {}
        for f in self.funds:
            nav6, nav0 = self._nav6m(f)
            if nav6 and nav0:
                perf[f] = (nav0 - nav6) / nav6
        if len(perf) < 10:
            return
        dec = len(perf) // 10
        longs = sorted(perf.items(), key=lambda kv: kv[1], reverse=True)[:dec]
        longs = [s for s, _ in longs]
        for pos in list(self.Positions):
            if pos.Security not in longs:
                self._move(pos.Security, 0)
        w = 1.0 / len(longs)
        port = self.Portfolio.CurrentValue or 0.0
        for s in longs:
            price = self._latest.get(s, 0)
            if price > 0:
                self._move(s, w * port / price)

    def _nav6m(self, fund):
        # placeholder for NAV lookup
        return (0, 0)

    def _move(self, sec, tgt):
        diff = tgt - self._pos(sec)
        price = self._latest.get(sec, 0)
        if price <= 0 or abs(diff) * price < self.min_trade_usd:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order, Security
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="MutualMom"))

    def _pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return mutual_fund_momentum_strategy()
