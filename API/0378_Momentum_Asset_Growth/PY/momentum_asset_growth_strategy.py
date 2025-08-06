import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order

class momentum_asset_growth_strategy(Strategy):
    """Combines momentum with the asset-growth effect."""

    def __init__(self):
        super(momentum_asset_growth_strategy, self).__init__()

        self._univ = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._momLook = self.Param("MomLook", 252) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum lookback", "Trading days for momentum", "Parameters")

        self._skip = self.Param("SkipMonths", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Skip months", "Months skipped from recent data", "Parameters")

        self._decile = self.Param("AssetDecile", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Asset decile", "Decile for high asset growth", "Parameters")

        self._quint = self.Param("Quintile", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Quintile", "Quintile for momentum ranking", "Parameters")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum order value in USD", "Parameters")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._px = {}
        self._weights = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region properties
    @property
    def Universe(self):
        return self._univ.Value

    @Universe.setter
    def Universe(self, value):
        self._univ.Value = value

    @property
    def MomLook(self):
        return self._momLook.Value

    @MomLook.setter
    def MomLook(self, value):
        self._momLook.Value = value

    @property
    def SkipMonths(self):
        return self._skip.Value

    @SkipMonths.setter
    def SkipMonths(self, value):
        self._skip.Value = value

    @property
    def AssetDecile(self):
        return self._decile.Value

    @AssetDecile.setter
    def AssetDecile(self, value):
        self._decile.Value = value

    @property
    def Quintile(self):
        return self._quint.Value

    @Quintile.setter
    def Quintile(self, value):
        self._quint.Value = value

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
        super(momentum_asset_growth_strategy, self).OnReseted()
        self._px.clear()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        super(momentum_asset_growth_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")
        for s, dt in self.GetWorkingSecurities():
            self._px[s] = RollingWin(self.MomLook + 1)
            self.SubscribeCandles(dt, True, s) \
                .Bind(lambda c, sec=s: self.ProcessCandle(c, sec)) \
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
        if d.Month == 1:
            return
        if d.Day != 1:
            return
        self.Rebalance()

    def Rebalance(self):
        aset = {}
        for s in self.Universe:
            ok, g = self.TryGetAssetGrowth(s)
            if ok:
                aset[s] = g
        if len(aset) < self.AssetDecile:
            return
        dec = len(aset) // self.AssetDecile
        highAG = [kv[0] for kv in sorted(aset.items(), key=lambda kv: kv[1], reverse=True)[:dec]]

        mom = {}
        for s in highAG:
            win = self._px.get(s)
            if win and win.Full:
                data = win.Data
                idx1 = max(0, len(data) - (self.SkipMonths * 21 + 1))
                idx2 = max(0, len(data) - (self.MomLook + 1))
                if data[idx2] != 0:
                    mom[s] = (data[idx1] - data[idx2]) / data[idx2]
        if len(mom) < self.Quintile * 2:
            return
        q = len(mom) // self.Quintile
        longs = [kv[0] for kv in sorted(mom.items(), key=lambda kv: kv[1], reverse=True)[:q]]
        shorts = [kv[0] for kv in sorted(mom.items(), key=lambda kv: kv[1])[:q]]

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
        diff = tgt - self.Pos(sec)
        price = self.GetLatestPrice(sec)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return
        order = Order()
        order.Security = sec
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "MomAG"
        self.RegisterOrder(order)

    def Pos(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def TryGetAssetGrowth(self, sec):
        return False, 0

    def GetLatestPrice(self, sec):
        return self._latest_prices.get(sec, 0)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return momentum_asset_growth_strategy()

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

    @property
    def Data(self):
        return list(self._q)
