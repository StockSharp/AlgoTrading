import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order

class low_volatility_stocks_strategy(Strategy):
    """Long lowest-volatility stocks and short highest-volatility stocks."""

    def __init__(self):
        super(low_volatility_stocks_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._window = self.Param("VolWindowDays", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Vol window", "Days in volatility window", "Parameters")

        self._deciles = self.Param("Deciles", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Deciles", "Number of deciles", "Parameters")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum order value in USD", "Parameters")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._ret = {}
        self._weights = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region properties
    @property
    def Universe(self):
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def VolWindowDays(self):
        return self._window.Value

    @VolWindowDays.setter
    def VolWindowDays(self, value):
        self._window.Value = value

    @property
    def Deciles(self):
        return self._deciles.Value

    @Deciles.setter
    def Deciles(self, value):
        self._deciles.Value = value

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
        super(low_volatility_stocks_strategy, self).OnReseted()
        self._ret.clear()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        super(low_volatility_stocks_strategy, self).OnStarted(time)

        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")

        for s, dt in self.GetWorkingSecurities():
            self._ret[s] = RollingWin(self.VolWindowDays + 1)
            self.SubscribeCandles(dt, True, s) \
                .Bind(lambda c, sec=s: self.ProcessCandle(c, sec)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        self.OnDaily(security, candle)

    def OnDaily(self, sec, candle):
        self._ret[sec].Add(candle.ClosePrice)
        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d
        if d.Day != 1:
            return
        self.Rebalance()

    def Rebalance(self):
        vol = {}
        for s, win in self._ret.items():
            if not win.Full:
                continue
            r = win.ReturnSeries()
            vol[s] = (decimal(Math.Sqrt(sum([float(x*x) for x in r]) / len(r))))
        if len(vol) < self.Deciles * 2:
            return
        bucket = len(vol) // self.Deciles
        low_vol = [kv[0] for kv in sorted(vol.items(), key=lambda kv: kv[1])[:bucket]]
        high_vol = [kv[0] for kv in sorted(vol.items(), key=lambda kv: kv[1], reverse=True)[:bucket]]
        self._weights.clear()
        wl = 1.0 / len(low_vol)
        ws = -1.0 / len(high_vol)
        for s in low_vol:
            self._weights[s] = wl
        for s in high_vol:
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
        order.Comment = "LowVol"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, sec):
        return self._latest_prices.get(sec, 0)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return low_volatility_stocks_strategy()

class RollingWin:
    def __init__(self, n):
        from collections import deque
        self._n = n
        self._q = deque()

    @property
    def Full(self):
        return len(self._q) == self._n

    def Add(self, v):
        if len(self._q) == self._n:
            self._q.popleft()
        self._q.append(v)

    def ReturnSeries(self):
        arr = list(self._q)
        res = []
        for i in range(1, len(arr)):
            prev = arr[i-1]
            if prev != 0:
                res.append((arr[i] - prev) / prev)
        return res
