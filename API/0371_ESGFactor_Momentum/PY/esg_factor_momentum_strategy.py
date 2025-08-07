import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order
from datatype_extensions import *


class esg_factor_momentum_strategy(Strategy):
    """Momentum rotation strategy based on ESG factors."""

    def __init__(self):
        super(esg_factor_momentum_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "ESG ETFs list", "Universe")

        self._lookback = self.Param("LookbackDays", 252) \
            .SetDisplay("Lookback Days", "Momentum lookback period", "General")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle TF", "Time-frame", "General")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetDisplay("Min Trade USD", "Minimum trade size in USD", "Risk Management")

        self._windows = {}
        self._latest_prices = {}
        self._held = set()
        self._last_proc = DateTime.MinValue

    # region properties
    @property
    def Universe(self):
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def LookbackDays(self):
        return self._lookback.Value

    @LookbackDays.setter
    def LookbackDays(self, value):
        self._lookback.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(esg_factor_momentum_strategy, self).OnReseted()
        self._windows.clear()
        self._latest_prices.clear()
        self._held.clear()
        self._last_proc = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")

        super(esg_factor_momentum_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            self._windows[sec] = RollingWindow(self.LookbackDays + 1)
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        win = self._windows.get(security)
        if win is None:
            return
        win.Add(candle.ClosePrice)

        d = candle.OpenTime.Date
        if d == self._last_proc:
            return
        self._last_proc = d

        if d.Day == 1:
            self.TryRebalance()

    def TryRebalance(self):
        if any(not w.IsFull() for w in self._windows.values()):
            return

        mom = {s: (w.Last() - w[0]) / w[0] for s, w in self._windows.items()}
        best = max(mom.values())
        winners = [s for s, v in mom.items() if v == best]
        w = 1.0 / len(winners)

        for s in list(self._held):
            if s not in winners:
                self.Move(s, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0
        for s in winners:
            price = self.GetLatestPrice(s)
            if price > 0:
                self.Move(s, w * portfolio_value / price)

        self._held = set(winners)
        self.LogInfo("Rebalanced to: {0}".format(",".join([x.Code for x in winners])))

    def Move(self, security, tgt):
        diff = tgt - self.PositionBy(security)
        price = self.GetLatestPrice(security)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return

        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "ESGMom"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def CreateClone(self):
        return esg_factor_momentum_strategy()


class RollingWindow:
    def __init__(self, size):
        self._size = size
        self._q = []

    def Add(self, v):
        if len(self._q) == self._size:
            self._q.pop(0)
        self._q.append(v)

    def IsFull(self):
        return len(self._q) == self._size

    def Last(self):
        return self._q[-1]

    def __getitem__(self, idx):
        return self._q[idx]
