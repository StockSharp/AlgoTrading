import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class month12_cycle_strategy(Strategy):
    """12-Month Cycle strategy. Ranks universe by prior-year monthly return and trades top/bottom deciles."""

    def __init__(self):
        super().__init__()
        self._universe = self.Param("Universe", list()) \
            .SetDisplay("Universe", "List of securities", "Universe")
        self._deciles = self.Param("DecileSize", 10) \
            .SetDisplay("Deciles", "Number of portfolios", "Ranking")
        self._leverage = self.Param("Leverage", 1.0) \
            .SetDisplay("Leverage", "Leverage per leg", "Risk")
        self._years = self.Param("YearsBack", 1) \
            .SetDisplay("Years Back", "Lag in years", "Ranking")
        self._tf = self.Param("CandleType", tf(1 * 1440)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._month_closes = {}
        self._cap = {}
        self._latest = {}
        self._target = {}

    @property
    def universe(self):
        return self._universe.Value

    @universe.setter
    def universe(self, value):
        self._universe.Value = value

    @property
    def decile_size(self):
        return self._deciles.Value

    @decile_size.setter
    def decile_size(self, value):
        self._deciles.Value = value

    @property
    def leverage(self):
        return self._leverage.Value

    @leverage.setter
    def leverage(self, value):
        self._leverage.Value = value

    @property
    def years_back(self):
        return self._years.Value

    @years_back.setter
    def years_back(self, value):
        self._years.Value = value

    @property
    def candle_type(self):
        return self._tf.Value

    @candle_type.setter
    def candle_type(self, value):
        self._tf.Value = value

    def GetWorkingSecurities(self):
        if not self.universe:
            raise Exception("Universe cannot be empty")
        for s in self.universe:
            yield s, self.candle_type

    def OnReseted(self):
        super().OnReseted()
        self._month_closes.clear()
        self._cap.clear()
        self._latest.clear()
        self._target.clear()

    def OnStarted(self, time):
        super().OnStarted(time)
        if not self.universe:
            raise Exception("Universe is empty")
        for sec, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda c, s=sec: self._process(c, s)) \
                .Start()
            self._month_closes[sec] = MonthWindow(13)

    def _process(self, candle, sec):
        if candle.State != CandleStates.Finished:
            return
        self._latest[sec] = candle.ClosePrice
        self._month_closes[sec].add(candle.ClosePrice)
        if candle.OpenTime.Date.Day == 1:
            self._rebalance()

    def _rebalance(self):
        ready = {s: w for s, w in self._month_closes.items() if w.full}
        if len(ready) < self.decile_size * 2:
            return
        perf = {}
        for sec, w in ready.items():
            perf[sec] = w.data[1] / w.data[0] - 1
            price = self._latest.get(sec, 0)
            self._cap[sec] = price * (sec.VolumeStep or 1)
        ranked = sorted(perf.items(), key=lambda kv: kv[1], reverse=True)
        dec_len = len(ranked) // self.decile_size
        if dec_len == 0:
            return
        winners = ranked[:dec_len]
        losers = ranked[-dec_len:]
        self._compute_weights(winners, losers)
        self._execute_trades()

    def _compute_weights(self, winners, losers):
        self._target.clear()
        cap_long = sum(self._cap[s] for s, _ in winners)
        cap_short = sum(self._cap[s] for s, _ in losers)
        for s, _ in winners:
            self._target[s] = self.leverage * (self._cap[s] / cap_long)
        for s, _ in losers:
            self._target[s] = -self.leverage * (self._cap[s] / cap_short)

    def _execute_trades(self):
        for pos in list(self.Positions):
            if pos.Security not in self._target:
                self._move(pos.Security, 0)
        portfolio_value = self.Portfolio.CurrentValue or 0.0
        for sec, w in self._target.items():
            price = self._latest.get(sec, 0)
            if price <= 0:
                continue
            tgt = w * portfolio_value / price
            self._move(sec, tgt)

    def _move(self, sec, tgt):
        diff = tgt - self._pos(sec)
        price = self._latest.get(sec, 0)
        if price <= 0 or abs(diff) * price < 50:
            return
        side = Sides.Buy if diff > 0 else Sides.Sell
        from StockSharp.BusinessEntities import Order, Security
        self.RegisterOrder(Order(Security=sec, Portfolio=self.Portfolio, Side=side,
                                 Volume=abs(diff), Type=OrderTypes.Market,
                                 Comment="12-MonthCycle"))

    def _pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val or 0.0

    def CreateClone(self):
        return month12_cycle_strategy()


class MonthWindow:
    def __init__(self, size):
        from collections import deque
        self._size = size
        self._q = deque()

    def add(self, v):
        if len(self._q) == self._size:
            self._q.popleft()
        self._q.append(v)

    @property
    def full(self):
        return len(self._q) == self._size

    @property
    def data(self):
        return list(self._q)
