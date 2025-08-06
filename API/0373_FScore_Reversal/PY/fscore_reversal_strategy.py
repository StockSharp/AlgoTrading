import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order
from datatype_extensions import *


class fscore_reversal_strategy(Strategy):
    """Piotroski F-Score reversal strategy."""

    def __init__(self):
        super(fscore_reversal_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities universe", "General")

        self._lookback = self.Param("Lookback", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Lookback period in days", "General")

        self._hi = self.Param("FHi", 7) \
            .SetDisplay("High F-Score", "Minimum F-Score for longs", "General")

        self._lo = self.Param("FLo", 3) \
            .SetDisplay("Low F-Score", "Maximum F-Score for shorts", "General")

        self._min_usd = self.Param("MinTradeUsd", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min trade USD", "Minimum order value", "Risk")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prices = {}
        self._ret = {}
        self._fscore = {}
        self._w = {}
        self._latest_prices = {}
        self._last_rebalance = DateTime.MinValue

    # region properties
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
    def FHi(self):
        return self._hi.Value

    @FHi.setter
    def FHi(self, value):
        self._hi.Value = value

    @property
    def FLo(self):
        return self._lo.Value

    @FLo.setter
    def FLo(self, value):
        self._lo.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe empty")
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(fscore_reversal_strategy, self).OnReseted()
        self._prices.clear()
        self._ret.clear()
        self._fscore.clear()
        self._w.clear()
        self._latest_prices.clear()
        self._last_rebalance = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe empty")

        super(fscore_reversal_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()
            self._prices[sec] = RollingWindow(self.Lookback + 1)

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        win = self._prices.get(security)
        if win is None:
            return
        win.Add(candle.ClosePrice)
        if win.IsFull():
            self._ret[security] = (win.Last() - win[0]) / win[0]

        d = candle.OpenTime.Date
        if d.Day == 1 and self._last_rebalance != d:
            self._last_rebalance = d
            self.Rebalance()

    def Rebalance(self):
        self._fscore.clear()
        for s in self.Universe:
            ok, fs = self.TryGetFScore(s)
            if ok:
                self._fscore[s] = fs

        eligible = [s for s in self._ret.keys() if s in self._fscore]
        if len(eligible) < 20:
            return

        longs = [s for s in eligible if self._ret[s] < 0 and self._fscore[s] >= self.FHi]
        shorts = [s for s in eligible if self._ret[s] > 0 and self._fscore[s] <= self.FLo]
        if not longs or not shorts:
            return

        self._w.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s in longs:
            self._w[s] = wl
        for s in shorts:
            self._w[s] = ws

        for pos in list(self.Positions):
            if pos.Security not in self._w:
                self.Order(pos.Security, -self.PositionBy(pos.Security))

        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec, weight in self._w.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                tgt = weight * portfolio_value / price
                diff = tgt - self.PositionBy(sec)
                if Math.Abs(diff) * price >= self.MinTradeUsd:
                    self.Order(sec, diff)

    def Order(self, security, qty):
        if qty == 0:
            return
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if qty > 0 else Sides.Sell
        order.Volume = Math.Abs(qty)
        order.Type = OrderTypes.Market
        order.Comment = "FScoreRev"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def TryGetFScore(self, s):
        return False, 0

    def CreateClone(self):
        return fscore_reversal_strategy()


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
