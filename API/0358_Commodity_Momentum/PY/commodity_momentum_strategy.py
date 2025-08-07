import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *
from collections import deque

class commodity_momentum_strategy(Strategy):
    """Long commodities with highest 12-month momentum (skip last month).
    Rebalanced monthly on the first trading day."""

    def __init__(self):
        super(commodity_momentum_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Commodities to trade", "General")

        self._top_n = self.Param("TopN", 5) \
            .SetDisplay("Top N", "Number of top momentum commodities to hold", "General")

        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min Trade USD", "Minimum USD amount per trade", "General")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Candle type used for calculations", "General")

        self._px = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region properties
    @property
    def Universe(self):
        """Universe of commodities to trade."""
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def TopN(self):
        """Number of top commodities to hold."""
        return self._top_n.Value

    @TopN.setter
    def TopN(self, value):
        self._top_n.Value = value

    @property
    def MinTradeUsd(self):
        """Minimum USD amount for trades."""
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value

    @property
    def CandleType(self):
        """Candle type used for calculations."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(commodity_momentum_strategy, self).OnReseted()
        self._px.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        super(commodity_momentum_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe cannot be empty.")

        for sec, dt in self.GetWorkingSecurities():
            self._px[sec] = RollingWin(252 + 1)
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        self.OnDaily(security, candle)

    def OnDaily(self, sec, candle):
        self._px[sec].Add(candle.ClosePrice)

        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d
        if d.Day != 1:
            return
        self.Rebalance()

    def Rebalance(self):
        mom = {}
        for s, win in self._px.items():
            if not win.Full:
                continue
            arr = win.Data
            r = (arr[21] - arr[252]) / arr[252]
            mom[s] = r

        if len(mom) < self.TopN:
            return

        winners = sorted(mom.items(), key=lambda kv: kv[1], reverse=True)[:self.TopN]
        winners = [kv[0] for kv in winners]

        for position in list(self.Positions):
            if position.Security not in winners:
                self.Move(position.Security, 0)

        w = 1.0 / len(winners)
        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec in winners:
            price = self.GetLatestPrice(sec)
            if price > 0:
                self.Move(sec, w * portfolio_value / price)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, sec, tgt):
        diff = tgt - self.PositionBy(sec)
        price = self.GetLatestPrice(sec)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return

        order = Order()
        order.Security = sec
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "ComMom"
        self.RegisterOrder(order)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return commodity_momentum_strategy()

class RollingWin:
    def __init__(self, n):
        self._n = n
        self._q = deque()

    @property
    def Full(self):
        return len(self._q) == self._n

    def Add(self, p):
        if len(self._q) == self._n:
            self._q.popleft()
        self._q.append(p)

    @property
    def Data(self):
        return list(self._q)
