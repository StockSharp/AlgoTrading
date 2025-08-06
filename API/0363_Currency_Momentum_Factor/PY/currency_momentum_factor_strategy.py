import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *

class currency_momentum_factor_strategy(Strategy):
    """Long top-K momentum currencies, short bottom-K; monthly rebalance."""

    def __init__(self):
        super(currency_momentum_factor_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._lookback = self.Param("Lookback", 252) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Momentum lookback period", "Parameters")

        self._k = self.Param("K", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Top/Bottom K", "Number of currencies long/short", "Parameters")

        self._tf = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "Risk Management")

        self._wins = {}
        self._weights = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region Properties
    @property
    def Universe(self):
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def K(self):
        return self._k.Value

    @K.setter
    def K(self, value):
        self._k.Value = value

    @property
    def CandleType(self):
        return self._tf.Value

    @CandleType.setter
    def CandleType(self, value):
        self._tf.Value = value

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
        super(currency_momentum_factor_strategy, self).OnReseted()
        self._wins.clear()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        super(currency_momentum_factor_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe must not be empty.")
        for security, dt in self.GetWorkingSecurities():
            self._wins[security] = RollingWindow(self.Lookback + 1)
            self.SubscribeCandles(dt, True, security) \
                .Bind(lambda candle, sec=security: self.ProcessCandle(candle, sec)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        self._wins[security].add(candle.ClosePrice)
        day = candle.OpenTime.Date
        if day == self._last_day:
            return
        self._last_day = day
        if day.Day == 1:
            self.Rebalance()

    def Rebalance(self):
        if any(not w.is_full() for w in self._wins.values()):
            return
        mom = {s: (w.last() - w[0]) / w[0] for s, w in self._wins.items()}
        top = sorted(mom.items(), key=lambda kv: kv[1], reverse=True)[:self.K]
        bot = sorted(mom.items(), key=lambda kv: kv[1])[:self.K]
        self._weights.clear()
        wl = 1.0 / len(top) if top else 0
        ws = -1.0 / len(bot) if bot else 0
        for s, _ in top:
            self._weights[s] = wl
        for s, _ in bot:
            self._weights[s] = ws
        for position in self.Positions:
            if position.Security not in self._weights:
                self.Move(position.Security, 0)
        portfolio_value = self.Portfolio.CurrentValue or 0
        for s, w in self._weights.items():
            price = self.GetLatestPrice(s)
            if price > 0:
                self.Move(s, w * portfolio_value / price)

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, security, target):
        diff = target - self.PositionBy(security)
        price = self.GetLatestPrice(security)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "CurrMom"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    class RollingWindow:
        def __init__(self, n):
            self._n = n
            self._q = []
        def add(self, v):
            if len(self._q) == self._n:
                self._q.pop(0)
            self._q.append(v)
        def is_full(self):
            return len(self._q) == self._n
        def last(self):
            return self._q[-1]
        def __getitem__(self, idx):
            return self._q[idx]

    def CreateClone(self):
        return currency_momentum_factor_strategy()
