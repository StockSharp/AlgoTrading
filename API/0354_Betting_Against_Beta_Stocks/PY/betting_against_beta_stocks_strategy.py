import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *
from collections import deque


class betting_against_beta_stocks_strategy(Strategy):
    """Betting Against Beta strategy for stocks."""

    def __init__(self):
        super(betting_against_beta_stocks_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Securities universe for strategy", "General")

        self._window = self.Param("WindowDays", 252) \
            .SetGreaterThanZero() \
            .SetDisplay("Window (days)", "Rolling window length for beta", "Parameters")

        self._deciles = self.Param("Deciles", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Deciles", "Number of buckets for sorting", "Parameters")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade $", "Minimum trade value", "Risk Management")

        self._wins = {}
        self._weights = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region Properties
    @property
    def Universe(self):
        """Trading universe."""
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def WindowDays(self):
        return self._window.Value

    @WindowDays.setter
    def WindowDays(self, value):
        self._window.Value = value

    @property
    def Deciles(self):
        return self._deciles.Value

    @Deciles.setter
    def Deciles(self, value):
        self._deciles.Value = value

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
        if self.Security is None:
            raise Exception("Benchmark not set")
        return [(s, self.CandleType) for s in list(self.Universe) + [self.Security]]

    def OnReseted(self):
        super(betting_against_beta_stocks_strategy, self).OnReseted()
        self._wins.clear()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty")
        if self.Security is None:
            raise Exception("Benchmark not set")

        super(betting_against_beta_stocks_strategy, self).OnStarted(time)

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

        portfolio_value = self.Portfolio.CurrentValue or 0.0
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
            return 0.0
        mean_x = sum(x[:n]) / n
        mean_y = sum(y[:n]) / n
        cov = 0.0
        var_m = 0.0
        for i in range(n):
            cov += (x[i] - mean_x) * (y[i] - mean_y)
            var_m += (y[i] - mean_y) * (y[i] - mean_y)
        return cov / var_m if var_m != 0 else 0.0

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
        order.Comment = "BABStocks"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0.0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0.0)

    def CreateClone(self):
        return betting_against_beta_stocks_strategy()


class RollingWindow:
    def __init__(self, n):
        self._n = n
        self._q = deque()

    def Add(self, v):
        if len(self._q) == self._n:
            self._q.popleft()
        self._q.append(v)

    def IsFull(self):
        return len(self._q) == self._n

    def Last(self):
        return self._q[-1]

    def __getitem__(self, i):
        return list(self._q)[i]

    def ToArray(self):
        return list(self._q)
