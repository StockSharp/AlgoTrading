import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order
from datatype_extensions import *


class fx_carry_trade_strategy(Strategy):
    """FX carry trade strategy long top carry and short bottom."""

    def __init__(self):
        super(fx_carry_trade_strategy, self).__init__()

        self._univ = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Currencies to trade", "General")

        self._topk = self.Param("TopK", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Top K", "Number of currencies to long and short", "General")

        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min trade USD", "Minimum order value", "Risk")

        self._tf = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Time frame", "General")

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
    def TopK(self):
        return self._topk.Value

    @TopK.setter
    def TopK(self, value):
        self._topk.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_usd.Value = value

    @property
    def CandleType(self):
        return self._tf.Value

    @CandleType.setter
    def CandleType(self, value):
        self._tf.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(fx_carry_trade_strategy, self).OnReseted()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe empty")

        super(fx_carry_trade_strategy, self).OnStarted(time)

        first = self.Universe[0]
        self.SubscribeCandles(self.CandleType, True, first) \
            .Bind(lambda candle, security=first: self.ProcessCandle(candle, security)) \
            .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        day = candle.OpenTime.Date
        if day == self._last_day:
            return
        self._last_day = day

        if day.Day == 1:
            self.Rebalance()

    def Rebalance(self):
        carry = {}
        for fx in self.Universe:
            ok, c = self.TryGetCarry(fx)
            if ok:
                carry[fx] = c

        if len(carry) < self.TopK * 2:
            return

        top = [kv[0] for kv in sorted(carry.items(), key=lambda kv: kv[1], reverse=True)[:self.TopK]]
        bot = [kv[0] for kv in sorted(carry.items(), key=lambda kv: kv[1])[:self.TopK]]

        self._weights.clear()
        wl = 1.0 / len(top)
        ws = -1.0 / len(bot)
        for s in top:
            self._weights[s] = wl
        for s in bot:
            self._weights[s] = ws

        for pos in list(self.Positions):
            if pos.Security not in self._weights:
                self.Move(pos.Security, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec, weight in self._weights.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                self.Move(sec, weight * portfolio_value / price)

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
        order.Comment = "FXCarry"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def TryGetCarry(self, s):
        return False, 0

    def CreateClone(self):
        return fx_carry_trade_strategy()
