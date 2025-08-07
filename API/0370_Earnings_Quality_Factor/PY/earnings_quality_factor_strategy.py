import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *


class earnings_quality_factor_strategy(Strategy):
    """Earnings quality factor strategy."""

    def __init__(self):
        super(earnings_quality_factor_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum trade size in USD", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles for calculation", "General")

        self._weights = {}
        self._latest_prices = {}
        self._last_processed = DateTime.MinValue

    # region Properties
    @property
    def Universe(self):
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

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
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(earnings_quality_factor_strategy, self).OnReseted()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_processed = DateTime.MinValue

    def OnStarted(self, time):
        super(earnings_quality_factor_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe empty")
        for sec, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        d = candle.OpenTime.Date
        if d == self._last_processed:
            return
        self._last_processed = d
        if d.Month == 7 and d.Day == 1:
            self.Rebalance()

    def Rebalance(self):
        scores = {}
        for s in self.Universe:
            ok, q = self.TryGetQualityScore(s)
            if ok:
                scores[s] = q
        if len(scores) < 20:
            return
        dec = len(scores) // 10
        longs = [kv[0] for kv in sorted(scores.items(), key=lambda kv: kv[1], reverse=True)[:dec]]
        shorts = [kv[0] for kv in sorted(scores.items(), key=lambda kv: kv[1])[:dec]]

        self._weights.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s in longs:
            self._weights[s] = wl
        for s in shorts:
            self._weights[s] = ws

        for position in list(self.Positions):
            if position.Security not in self._weights:
                self.Move(position.Security, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec, w in self._weights.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                self.Move(sec, w * portfolio_value / price)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, security, tgt):
        diff = tgt - self.PositionBy(security)
        price = self.GetLatestPrice(security)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "EQuality"
        self.RegisterOrder(order)

    def TryGetQualityScore(self, security):
        return False, 0.0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return earnings_quality_factor_strategy()
