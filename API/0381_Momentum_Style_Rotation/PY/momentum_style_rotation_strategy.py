import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class momentum_style_rotation_strategy(Strategy):
    """Rotates among factor ETFs and a market ETF based on trailing 3-month performance."""

    def __init__(self):
        super().__init__()
        self._factors = self.Param("FactorETFs", list()) \
            .SetDisplay("Factor ETFs", "List of factor ETFs", "Universe")
        self._look = self.Param("LookbackDays", 63) \
            .SetDisplay("Lookback", "Performance lookback in days", "Parameters")
        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min USD", "Minimum trade value", "Risk")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Type of candles used", "General")

        self._px = {}
        self._latest = {}
        self._last_day = None

    @property
    def factor_etfs(self):
        return self._factors.Value

    @factor_etfs.setter
    def factor_etfs(self, value):
        self._factors.Value = value

    @property
    def lookback_days(self):
        return self._look.Value

    @lookback_days.setter
    def lookback_days(self, value):
        self._look.Value = value

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
        if self.factor_etfs is not None:
            for s in self.factor_etfs:
                yield s, self.candle_type
        if self.Security is not None:
            yield self.Security, self.candle_type

    def OnReseted(self):
        super().OnReseted()
        self._px.clear()
        self._latest.clear()
        self._last_day = None

    def OnStarted(self, time):
        super().OnStarted(time)
        if not self.factor_etfs:
            raise Exception("FactorETFs cannot be empty.")
        if self.Security is None:
            raise Exception("MarketETF cannot be null.")
        for sec, tf in self.GetWorkingSecurities():
            self._px[sec] = RollingWin(self.lookback_days + 1)
            self.SubscribeCandles(tf, True, sec) \
                .Bind(lambda c, s=sec: self._process_candle(c, s)) \
                .Start()

    def _process_candle(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._on_daily(sec, candle)

    def _on_daily(self, sec, candle):
        self._px[sec].add(candle.ClosePrice)
        d = candle.OpenTime.Date
        if self._last_day == d:
            return
        self._last_day = d
        if d.Day != 1:
            return
        self._rebalance()

    def _rebalance(self):
        perf = {}
        for sec, win in self._px.items():
            if win.full:
                data = win.data
                perf[sec] = (data[0] - data[-1]) / data[-1]
        if not perf:
            return
        best = max(perf, key=perf.get)
        for pos in list(self.Positions):
            if pos.Security != best:
                self._move(pos.Security, 0)
        portfolio_value = self.Portfolio.CurrentValue or 0.0
        price = self._latest.get(best, 0.0)
        if price > 0:
            self._move(best, portfolio_value / price)

    def _move(self, sec, tgt):
        diff = tgt - self._position_by(sec)
        price = self._latest.get(sec, 0.0)
        if price <= 0 or abs(diff) * price < self.min_trade_usd:
            return
        self.RegisterOrder(self._create_order(sec, diff))

    def _create_order(self, sec, diff):
        from StockSharp.BusinessEntities import Order
        side = Sides.Buy if diff > 0 else Sides.Sell
        return Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                     Volume=abs(diff), Type=self.OrderTypes.Market,
                     Comment="StyleRot")

    def _position_by(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return momentum_style_rotation_strategy()


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
