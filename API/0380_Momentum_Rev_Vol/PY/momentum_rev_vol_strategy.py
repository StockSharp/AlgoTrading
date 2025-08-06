import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order

class momentum_rev_vol_strategy(Strategy):
    """Momentum / reversal / volatility composite strategy."""

    def __init__(self):
        super(momentum_rev_vol_strategy, self).__init__()

        self._univ = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "Universe")

        self._look12 = self.Param("Lookback12", 252) \
            .SetDisplay("Momentum Lookback", "Days for 12M momentum", "Parameters")

        self._look1 = self.Param("Lookback1", 21) \
            .SetDisplay("Reversal Lookback", "Days for 1M reversal", "Parameters")

        self._volWindow = self.Param("VolWindow", 60) \
            .SetDisplay("Vol window", "Days for volatility", "Parameters")

        self._wM = self.Param("WM", 1.0) \
            .SetDisplay("Momentum weight", "Weight for momentum", "Parameters")

        self._wR = self.Param("WR", 1.0) \
            .SetDisplay("Reversal weight", "Weight for reversal", "Parameters")

        self._wV = self.Param("WV", 1.0) \
            .SetDisplay("Volatility weight", "Weight for volatility", "Parameters")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetDisplay("Min USD", "Minimum trade value", "Risk")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles used", "General")

        self._map = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue
        self._weights = {}

    # region properties
    @property
    def Universe(self):
        return self._univ.Value

    @Universe.setter
    def Universe(self, value):
        self._univ.Value = value

    @property
    def Lookback12(self):
        return self._look12.Value

    @Lookback12.setter
    def Lookback12(self, value):
        self._look12.Value = value

    @property
    def Lookback1(self):
        return self._look1.Value

    @Lookback1.setter
    def Lookback1(self, value):
        self._look1.Value = value

    @property
    def VolWindow(self):
        return self._volWindow.Value

    @VolWindow.setter
    def VolWindow(self, value):
        self._volWindow.Value = value

    @property
    def WM(self):
        return self._wM.Value

    @WM.setter
    def WM(self, value):
        self._wM.Value = value

    @property
    def WR(self):
        return self._wR.Value

    @WR.setter
    def WR(self, value):
        self._wR.Value = value

    @property
    def WV(self):
        return self._wV.Value

    @WV.setter
    def WV(self, value):
        self._wV.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_trade_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_trade_usd.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(momentum_rev_vol_strategy, self).OnReseted()
        self._map.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue
        self._weights.clear()

    def OnStarted(self, time):
        super(momentum_rev_vol_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe cannot be empty.")
        for s, dt in self.GetWorkingSecurities():
            self._map[s] = Win(self.Lookback12 + 1, self.VolWindow + 1)
            self.SubscribeCandles(dt, True, s) \
                .Bind(lambda c, sec=s: self.ProcessCandle(c, sec)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        self.OnDaily(security, candle)

    def OnDaily(self, sec, candle):
        w = self._map[sec]
        w.Px.Add(candle.ClosePrice)
        w.Ret.Add(candle.ClosePrice)
        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d
        if d.Day != 1:
            return
        self.Rebalance()

    def Rebalance(self):
        score = {}
        for s, w in self._map.items():
            px = w.Px.Data
            if len(px) < self.Lookback12 + 1:
                continue
            mom = (px[-1] - px[0]) / px[0]
            rev = (px[-1] - px[-self.Lookback1 - 1]) / px[-self.Lookback1 - 1]
            ret = w.Ret.ReturnSeries()
            if len(ret) < self.VolWindow:
                continue
            vol = (decimal(Math.Sqrt(sum([float(x*x) for x in ret]) / len(ret))))
            score[s] = self.WM * mom - self.WR * rev - self.WV * vol
        if len(score) < 20:
            return
        dec = len(score) // 10
        longs = [kv[0] for kv in sorted(score.items(), key=lambda kv: kv[1], reverse=True)[:dec]]
        shorts = [kv[0] for kv in sorted(score.items(), key=lambda kv: kv[1])[:dec]]
        self._weights.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s in longs:
            self._weights[s] = wl
        for s in shorts:
            self._weights[s] = ws
        for p in self.Positions:
            if p.Security not in self._weights:
                self.Move(p.Security, 0)
        portfolio_value = self.Portfolio.CurrentValue or 0
        for s, w in self._weights.items():
            price = self.GetLatestPrice(s)
            if price > 0:
                self.Move(s, w * portfolio_value / price)

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
        order.Comment = "MomRevVol"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, sec):
        return self._latest_prices.get(sec, 0)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return momentum_rev_vol_strategy()

class Win:
    def __init__(self, pxN, volN):
        self.Px = RollingWin(pxN)
        self.Ret = RollingWin(volN)

class RollingWin:
    def __init__(self, n):
        from collections import deque
        self._n = n
        self._q = deque()

    def Add(self, v):
        if len(self._q) == self._n:
            self._q.popleft()
        self._q.append(v)

    @property
    def Data(self):
        return list(self._q)

    def ReturnSeries(self):
        arr = list(self._q)
        res = []
        for i in range(1, len(arr)):
            prev = arr[i-1]
            if prev != 0:
                res.append((arr[i] - prev) / prev)
        return res
