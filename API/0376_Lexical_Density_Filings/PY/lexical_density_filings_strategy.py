import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *

class lexical_density_filings_strategy(Strategy):
    """
    Strategy based on lexical density of company filings.
    Rebalances quarterly using the first three trading days of February, May,
    August and November.
    """

    def __init__(self):
        super(lexical_density_filings_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._quintile = self.Param("Quintile", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Quintile", "Number of quintiles for ranking", "Parameters")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum order value in USD", "Parameters")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

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
    def Quintile(self):
        return self._quintile.Value

    @Quintile.setter
    def Quintile(self, value):
        self._quintile.Value = value

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
        super(lexical_density_filings_strategy, self).OnReseted()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        super(lexical_density_filings_strategy, self).OnStarted(time)

        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")

        trigger = self.Universe[0]

        self.SubscribeCandles(self.CandleType, True, trigger) \
            .Bind(lambda c: self.ProcessCandle(c, trigger)) \
            .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice

        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d

        if self.IsQuarterRebalanceDay(d):
            self.Rebalance()

    def IsQuarterRebalanceDay(self, d):
        return (d.Month == 2 or d.Month == 5 or d.Month == 8 or d.Month == 11) and d.Day <= 3

    def Rebalance(self):
        dens = {}
        for s in self.Universe:
            ok, val = self.TryGetLexicalDensity(s)
            if ok:
                dens[s] = val

        if len(dens) < self.Quintile * 2:
            return

        bucket = len(dens) // self.Quintile
        longs = [kv[0] for kv in sorted(dens.items(), key=lambda kv: kv[1], reverse=True)[:bucket]]
        shorts = [kv[0] for kv in sorted(dens.items(), key=lambda kv: kv[1])[:bucket]]

        self._weights.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s in longs:
            self._weights[s] = wl
        for s in shorts:
            self._weights[s] = ws

        for p in self.Positions:
            if p.Security not in self._weights:
                self.TradeTo(p.Security, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0
        for s, w in self._weights.items():
            price = self.GetLatestPrice(s)
            if price > 0:
                self.TradeTo(s, w * portfolio_value / price)

    def TradeTo(self, sec, tgt):
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
        order.Comment = "LexDensity"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def TryGetLexicalDensity(self, sec):
        return False, 0

    def GetLatestPrice(self, sec):
        return self._latest_prices.get(sec, 0)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return lexical_density_filings_strategy()
