import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class currency_ppp_value_strategy(Strategy):
    """Currency purchasing power parity value strategy."""

    def __init__(self):
        super(currency_ppp_value_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._k = self.Param("K", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("K", "Number of currencies to long/short", "General")

        self._tf = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Time frame of candles", "General")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum trade size in USD", "Risk Management")

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
        super(currency_ppp_value_strategy, self).OnReseted()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        super(currency_ppp_value_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")
        for s, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, s) \
                .Bind(lambda candle, sec=s: self.ProcessCandle(candle, sec)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        self.Daily(candle)

    def Daily(self, candle):
        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d
        if d.Day == 1:
            self.Rebalance()

    def Rebalance(self):
        dev = {}
        for s in self.Universe:
            ok, val = self.TryGetPPPDeviation(s)
            if ok:
                dev[s] = val
        if len(dev) < self.K * 2:
            return
        underv = sorted(dev.items(), key=lambda kv: kv[1])[:self.K]
        over = sorted(dev.items(), key=lambda kv: kv[1], reverse=True)[:self.K]
        self._weights.clear()
        wl = 1.0 / len(underv)
        ws = -1.0 / len(over)
        for s, _ in underv:
            self._weights[s] = wl
        for s, _ in over:
            self._weights[s] = ws
        for position in self.Positions:
            if position.Security not in self._weights:
                self.Move(position.Security, 0)
        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec, w in self._weights.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                self.Move(sec, w * portfolio_value / price)

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
        order.Comment = "PPPValue"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def TryGetPPPDeviation(self, s):
        return False, 0

    def CreateClone(self):
        return currency_ppp_value_strategy()
