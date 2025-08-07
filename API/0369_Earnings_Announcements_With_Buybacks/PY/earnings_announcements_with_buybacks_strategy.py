import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, DateTimeOffset, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *


class earnings_announcements_with_buybacks_strategy(Strategy):
    """Buys stocks with active buyback programs before earnings and exits after."""

    def __init__(self):
        super(earnings_announcements_with_buybacks_strategy, self).__init__()

        self._universe = self.Param("Universe", Array.Empty[Security]()) \
            .SetDisplay("Universe", "Securities to monitor", "General")

        self._days_before = self.Param("DaysBefore", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Days Before", "Days before earnings to enter", "Trading")

        self._days_after = self.Param("DaysAfter", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Days After", "Days after earnings to exit", "Trading")

        self._capital_usd = self.Param("CapitalPerTradeUsd", 5000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Capital Per Trade USD", "Capital allocated per trade", "Risk Management")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Minimum Trade USD", "Minimum trade value", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._exit = {}
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
    def DaysBefore(self):
        return self._days_before.Value

    @DaysBefore.setter
    def DaysBefore(self, value):
        self._days_before.Value = value

    @property
    def DaysAfter(self):
        return self._days_after.Value

    @DaysAfter.setter
    def DaysAfter(self, value):
        self._days_after.Value = value

    @property
    def CapitalPerTradeUsd(self):
        return self._capital_usd.Value

    @CapitalPerTradeUsd.setter
    def CapitalPerTradeUsd(self, value):
        self._capital_usd.Value = value

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
        super(earnings_announcements_with_buybacks_strategy, self).OnReseted()
        self._exit.clear()
        self._latest_prices.clear()
        self._last_processed = DateTime.MinValue

    def OnStarted(self, time):
        super(earnings_announcements_with_buybacks_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")
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
        self.DailyScan(d)

    def DailyScan(self, today):
        for stock in self.Universe:
            ok, earn_date = self.TryGetNextEarningsDate(stock)
            if not ok:
                continue
            diff = (earn_date.Date - today).TotalDays
            if diff == self.DaysBefore and stock not in self._exit and self.TryHasActiveBuyback(stock):
                price = self.GetLatestPrice(stock)
                if price <= 0:
                    continue
                qty = self.CapitalPerTradeUsd / price
                if qty * price >= self.MinTradeUsd:
                    self.Place(stock, qty, Sides.Buy, "Enter")
                    self._exit[stock] = earn_date.Date.AddDays(self.DaysAfter)

        for sec, exit_day in list(self._exit.items()):
            if today >= exit_day:
                pos = self.PositionBy(sec)
                if pos > 0:
                    self.Place(sec, pos, Sides.Sell, "Exit")
                del self._exit[sec]

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Place(self, security, qty, side, tag):
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = side
        order.Volume = qty
        order.Type = OrderTypes.Market
        order.Comment = f"EarnBuyback-{tag}"
        self.RegisterOrder(order)

    def TryGetNextEarningsDate(self, security):
        return False, DateTimeOffset.MinValue

    def TryHasActiveBuyback(self, security):
        return False

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return earnings_announcements_with_buybacks_strategy()
