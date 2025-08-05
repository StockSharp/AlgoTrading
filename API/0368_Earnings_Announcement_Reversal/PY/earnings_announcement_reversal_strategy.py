import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *


class _Win:
    def __init__(self):
        self.Px = []
        self.Held = 0


class earnings_announcement_reversal_strategy(Strategy):
    """Trades short-term reversals around earnings announcements."""

    def __init__(self):
        super(earnings_announcement_reversal_strategy, self).__init__()

        self._univ = self.Param("Universe", []) \
            .SetDisplay("Universe", "Collection of securities to trade", "General")

        self._look = self.Param("LookbackDays", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Days", "Number of days to calculate returns", "Parameters")

        self._hold = self.Param("HoldingDays", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Holding Days", "Days to hold position", "Parameters")

        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum trade value in USD", "Parameters")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._map = {}
        self._latest_prices = {}

    # region Properties
    @property
    def Universe(self):
        return self._univ.Value

    @Universe.setter
    def Universe(self, value):
        self._univ.Value = value

    @property
    def LookbackDays(self):
        return self._look.Value

    @LookbackDays.setter
    def LookbackDays(self, value):
        self._look.Value = value

    @property
    def HoldingDays(self):
        return self._hold.Value

    @HoldingDays.setter
    def HoldingDays(self, value):
        self._hold.Value = value

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
        super(earnings_announcement_reversal_strategy, self).OnReseted()
        self._map.clear()
        self._latest_prices.clear()

    def OnStarted(self, time):
        super(earnings_announcement_reversal_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe cannot be empty.")
        for sec, tf in self.GetWorkingSecurities():
            self._map[sec] = _Win()
            self.SubscribeCandles(tf, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        self.OnDaily(security, candle)

    def OnDaily(self, security, candle):
        win = self._map[security]
        if len(win.Px) == self.LookbackDays + 1:
            win.Px.pop(0)
        win.Px.append(candle.ClosePrice)

        ok, ed = self.TryGetEarningsDate(security)
        if not ok:
            return
        day = candle.OpenTime.Date
        if abs((day - ed.Date).TotalDays) > 1:
            return
        if len(win.Px) < self.LookbackDays + 1:
            return

        arr = win.Px
        ret = (arr[0] - arr[-1]) / arr[-1]
        portfolio_value = self.Portfolio.CurrentValue or 0
        price = self.GetLatestPrice(security)
        if price <= 0:
            return
        if ret > 0:
            self.Move(security, -portfolio_value / len(self.Universe) / price)
        else:
            self.Move(security, portfolio_value / len(self.Universe) / price)
        win.Held = 0

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
        order.Comment = "EARev"
        self.RegisterOrder(order)

    def TryGetEarningsDate(self, security):
        return False, DateTime.MinValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return earnings_announcement_reversal_strategy()
