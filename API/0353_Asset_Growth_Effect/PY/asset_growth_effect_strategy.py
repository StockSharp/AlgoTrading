import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *

class asset_growth_effect_strategy(Strategy):
    """Asset Growth Effect strategy.
    Rebalances annually in July based on total asset growth."""

    def __init__(self):
        super(asset_growth_effect_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities universe to trade", "General")

        self._quant = self.Param("Quantiles", 10) \
            .SetDisplay("Quantiles", "Number of growth quantiles", "General")

        self._lev = self.Param("Leverage", 1.0) \
            .SetDisplay("Leverage", "Target portfolio leverage", "General")

        self._min_usd = self.Param("MinTradeUsd", 50.0) \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "General")

        self._candle_type = self.Param("CandleType", tf(1*1440)) \
            .SetDisplay("Candle Type", "Candle type for calculations", "General")

        self._prev = {}
        self._w = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region Properties
    @property
    def Universe(self):
        """Securities universe to trade."""
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def Quantiles(self):
        """Number of quantiles used to rank securities."""
        return self._quant.Value

    @Quantiles.setter
    def Quantiles(self, value):
        self._quant.Value = value

    @property
    def Leverage(self):
        """Target portfolio leverage."""
        return self._lev.Value

    @Leverage.setter
    def Leverage(self, value):
        self._lev.Value = value

    @property
    def MinTradeUsd(self):
        """Minimum trade value in USD."""
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value

    @property
    def CandleType(self):
        """Candle type used for calculations."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(asset_growth_effect_strategy, self).OnReseted()
        self._prev.clear()
        self._w.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe cannot be empty.")

        super(asset_growth_effect_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice

        d = candle.OpenTime.Date
        if d == self._last_day:
            return

        self._last_day = d

        if d.Month == 7 and d.Day == 1:
            self.Rebalance()

    def Rebalance(self):
        growth = {}

        for s in self.Universe:
            ok, tot = self.TryGetTotalAssets(s)
            if not ok:
                continue

            prev = self._prev.get(s)
            if prev is not None and prev > 0:
                growth[s] = (tot - prev) / prev
            self._prev[s] = tot

        if len(growth) < self.Quantiles * 2:
            return

        qlen = len(growth) // self.Quantiles
        sorted_items = sorted(growth.items(), key=lambda kv: kv[1])
        longs = [kv[0] for kv in sorted_items[:qlen]]
        shorts = [kv[0] for kv in sorted_items[-qlen:]]

        self._w.clear()

        wl = self.Leverage / len(longs)
        ws = -self.Leverage / len(shorts)

        for s in longs:
            self._w[s] = wl
        for s in shorts:
            self._w[s] = ws

        for position in self.Positions:
            if position.Security not in self._w:
                self.Move(position.Security, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0

        for sec, weight in self._w.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                self.Move(sec, weight * portfolio_value / price)

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
        order.Comment = "AssetGrowth"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def TryGetTotalAssets(self, s):
        return False, 0

    def CreateClone(self):
        return asset_growth_effect_strategy()
