import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *

class betting_against_beta_strategy(Strategy):
    """Betting Against Beta strategy.

    Longs the lowest beta decile and shorts the highest beta decile.
    Betas are estimated against the benchmark over a rolling window and
    the portfolio is rebalanced monthly on the first trading day.
    """

    def __init__(self):
        super(betting_against_beta_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities universe", "General")

        self._window = self.Param("WindowDays", 252) \
            .SetDisplay("Window Days", "Lookback window length", "General") \
            .SetGreaterThanZero()

        self._deciles = self.Param("Deciles", 10) \
            .SetDisplay("Deciles", "Number of deciles", "General") \
            .SetGreaterThanZero()

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Candle time frame", "General")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "General") \
            .SetGreaterThanZero()

        self._wins = {}
        self._weights = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region Properties
    @property
    def Universe(self):
        """Securities universe."""
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def WindowDays(self):
        """Lookback window in days."""
        return self._window.Value

    @WindowDays.setter
    def WindowDays(self, value):
        self._window.Value = value

    @property
    def Deciles(self):
        """Number of decile buckets."""
        return self._deciles.Value

    @Deciles.setter
    def Deciles(self, value):
        self._deciles.Value = value

    @property
    def CandleType(self):
        """Candle time frame used by the strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MinTradeUsd(self):
        """Minimum trade size in USD."""
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value
    # endregion

    def GetWorkingSecurities(self):
        if self.Security is None:
            raise Exception("Benchmark not set")
        return [(s, self.CandleType) for s in self.Universe + [self.Security]]

    def OnReseted(self):
        super(betting_against_beta_strategy, self).OnReseted()
        self._wins.clear()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty")
        if self.Security is None:
            raise Exception("Benchmark not set")

        super(betting_against_beta_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            self._wins[sec] = RollingWindow(self.WindowDays + 1)
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        self._wins[security].Add(candle.ClosePrice)

        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d

        if d.Day == 1:
            self.TryRebalance()

    def TryRebalance(self):
        if any(not w.IsFull() for w in self._wins.values()):
            return

        bench_ret = self.GetReturns(self._wins[self.Security])
        betas = {}
        for s in self.Universe:
            r = self.GetReturns(self._wins[s])
            betas[s] = self.Beta(r, bench_ret)

        bucket = len(betas) // self.Deciles
        if bucket == 0:
            return

        sorted_items = sorted(betas.items(), key=lambda kv: kv[1])
        longs = [kv[0] for kv in sorted_items[:bucket]]
        shorts = [kv[0] for kv in sorted_items[-bucket:]]

        self._weights.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s in longs:
            self._weights[s] = wl
        for s in shorts:
            self._weights[s] = ws

        for position in self.Positions:
            if position.Security not in self._weights:
                self.Move(position.Security, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec, weight in self._weights.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                self.Move(sec, weight * portfolio_value / price)

    def GetReturns(self, win):
        arr = win.ToArray()
        return [(arr[i] - arr[i - 1]) / arr[i - 1] for i in range(1, len(arr))]

    def Beta(self, x, y):
        n = min(len(x), len(y))
        if n == 0:
            return 0
        mean_x = sum(x[:n]) / n
        mean_y = sum(y[:n]) / n
        cov = 0.0
        var_m = 0.0
        for i in range(n):
            cov += (x[i] - mean_x) * (y[i] - mean_y)
            var_m += (y[i] - mean_y) * (y[i] - mean_y)
        return cov / var_m if var_m != 0 else 0

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
        order.Comment = "BAB"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def CreateClone(self):
        """Creates a new copy of the strategy."""
        return betting_against_beta_strategy()

class RollingWindow:
    def __init__(self, n):
        self._n = n
        self._q = []

    def Add(self, v):
        if len(self._q) == self._n:
            self._q.pop(0)
        self._q.append(v)

    def IsFull(self):
        return len(self._q) == self._n

    def ToArray(self):
        return list(self._q)
