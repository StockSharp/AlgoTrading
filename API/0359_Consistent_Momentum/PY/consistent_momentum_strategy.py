import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class consistent_momentum_strategy(Strategy):
    """Consistent momentum strategy.
    Selects securities that show strong momentum over multiple windows
    and holds positions for a fixed number of months.
    """

    def __init__(self):
        super(consistent_momentum_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._lookback = self.Param("LookbackDays", 7 * 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Days", "Days in momentum lookback window", "Parameters")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._holding_months = self.Param("HoldingMonths", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Holding Months", "Months to keep a position", "Parameters")

        self._min_usd = self.Param("MinTradeUsd", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimal trade value in USD", "Parameters")

        self._prices = {}
        self._latest_prices = {}
        self._tranches = []
        self._last_day = DateTime.MinValue

    # region Properties
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
    def HoldingMonths(self):
        return self._holding_months.Value

    @HoldingMonths.setter
    def HoldingMonths(self, value):
        self._holding_months.Value = value

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
        super(consistent_momentum_strategy, self).OnReseted()
        self._prices.clear()
        self._latest_prices.clear()
        self._tranches.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe cannot be empty.")

        super(consistent_momentum_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            self._prices[sec] = RollingWindow(self.LookbackDays + 1)
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Store latest closing price
        self._latest_prices[security] = candle.ClosePrice

        self.ProcessDaily(candle, security)

    def ProcessDaily(self, c, sec):
        self._prices[sec].Add(c.ClosePrice)

        d = c.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d

        # Age tranches
        for tr in list(self._tranches):
            tr.Age += 1
            if tr.Age >= self.HoldingMonths:
                for s, qty in tr.Pos:
                    self.Move(s, 0)
                self._tranches.remove(tr)

        # Rebalance on first day of month
        if d.Day != 1:
            return

        if any(not w.IsFull() for w in self._prices.values()):
            return

        m7 = 7 * 21
        m71 = {s: (w[m7 - 21] - w[0]) / w[0] for s, w in self._prices.items()}
        m60 = {s: (w.Last() - w[21]) / w[21] for s, w in self._prices.items()}

        dec = len(self._prices) // 10
        top71 = set([kv[0] for kv in sorted(m71.items(), key=lambda kv: kv[1], reverse=True)[:dec]])
        top60 = set([kv[0] for kv in sorted(m60.items(), key=lambda kv: kv[1], reverse=True)[:dec]])
        bot71 = set([kv[0] for kv in sorted(m71.items(), key=lambda kv: kv[1])[:dec]])
        bot60 = set([kv[0] for kv in sorted(m60.items(), key=lambda kv: kv[1])[:dec]])

        longs = list(top71 & top60)
        shorts = list(bot71 & bot60)

        if len(longs) == 0 or len(shorts) == 0:
            return

        cap = self.Portfolio.CurrentValue or 0
        wl = cap * 0.5 / len(longs)
        ws = cap * 0.5 / len(shorts)

        tranche = Tranche()

        for s in longs:
            price = self.GetLatestPrice(s)
            if price > 0:
                qty = wl / price
                self.Move(s, qty)
                tranche.Pos.append((s, qty))

        for s in shorts:
            price = self.GetLatestPrice(s)
            if price > 0:
                qty = -ws / price
                self.Move(s, qty)
                tranche.Pos.append((s, qty))

        self._tranches.append(tranche)

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, s, tgt):
        diff = tgt - self.PositionBy(s)
        price = self.GetLatestPrice(s)

        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return

        order = Order()
        order.Security = s
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "ConsMom"
        self.RegisterOrder(order)

    def PositionBy(self, s):
        val = self.GetPositionValue(s, self.Portfolio)
        return val if val is not None else 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return consistent_momentum_strategy()

class Tranche(object):
    def __init__(self):
        self.Pos = []
        self.Age = 0

class RollingWindow(object):
    def __init__(self, n):
        self._n = n
        self._q = []

    def Add(self, v):
        if len(self._q) == self._n:
            self._q.pop(0)
        self._q.append(v)

    def IsFull(self):
        return len(self._q) == self._n

    def Last(self):
        return self._q[-1]

    def __getitem__(self, i):
        return self._q[i]
