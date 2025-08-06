import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import DateTime, DateTimeOffset, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *


class earnings_announcement_premium_strategy(Strategy):
    """Earnings announcement premium strategy."""

    def __init__(self):
        super(earnings_announcement_premium_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._days_before = self.Param("DaysBefore", 5) \
            .SetDisplay("Days Before", "Days before earnings to enter", "General")

        self._days_after = self.Param("DaysAfter", 1) \
            .SetDisplay("Days After", "Days after earnings to exit", "General")

        self._capital_usd = self.Param("CapitalPerTradeUsd", 5000.0) \
            .SetDisplay("Capital per Trade (USD)", "Capital allocated per trade", "Risk")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetDisplay("Minimum Trade (USD)", "Minimal trade value", "Risk")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")

        self._exit_schedule = {}
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
        super(earnings_announcement_premium_strategy, self).OnReseted()
        self._exit_schedule.clear()
        self._latest_prices.clear()
        self._last_processed = DateTime.MinValue

    def OnStarted(self, time):
        super(earnings_announcement_premium_strategy, self).OnStarted(time)
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")
        for sec, tf in self.GetWorkingSecurities():
            self.SubscribeCandles(tf, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        day = candle.OpenTime.Date
        if day == self._last_processed:
            return
        self._last_processed = day
        self.DailyScan(day)

    def DailyScan(self, today):
        # Entries
        for stock in self.Universe:
            ok, earn_date = self.TryGetNextEarningsDate(stock)
            if not ok:
                continue
            diff = (earn_date.Date - today).TotalDays
            if diff == self.DaysBefore and stock not in self._exit_schedule:
                price = self.GetLatestPrice(stock)
                if price <= 0:
                    continue
                qty = self.CapitalPerTradeUsd / price
                if qty * price >= self.MinTradeUsd:
                    self.Place(stock, qty, Sides.Buy, "Enter")
                    self._exit_schedule[stock] = earn_date.Date.AddDays(self.DaysAfter)

        # Exits
        for sec, exit_day in list(self._exit_schedule.items()):
            if today < exit_day:
                continue
            pos = self.PositionBy(sec)
            if pos > 0:
                self.Place(sec, pos, Sides.Sell, "Exit")
            del self._exit_schedule[sec]

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Place(self, security, qty, side, tag):
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = side
        order.Volume = qty
        order.Type = OrderTypes.Market
        order.Comment = f"EarnPrem-{tag}"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def TryGetNextEarningsDate(self, security):
        return False, DateTimeOffset.MinValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return earnings_announcement_premium_strategy()
