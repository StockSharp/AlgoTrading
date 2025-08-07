import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class crude_oil_predicts_equity_strategy(Strategy):
    """Invest in equity ETF when crude oil return is positive, otherwise hold cash ETF."""

    def __init__(self):
        super(crude_oil_predicts_equity_strategy, self).__init__()

        self._oil = self.Param("Oil", None) \
            .SetDisplay("Oil", "Crude oil security for signal", "General")

        self._cash = self.Param("CashEtf", None) \
            .SetDisplay("Cash ETF", "Cash ETF when not invested", "General")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")

        self._lookback = self.Param("Lookback", 22) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Number of candles for return calculation", "General")

        self._wins = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region Properties
    @property
    def Equity(self):
        return self.Security

    @Equity.setter
    def Equity(self, value):
        self.Security = value

    @property
    def Oil(self):
        return self._oil.Value

    @Oil.setter
    def Oil(self, value):
        self._oil.Value = value

    @property
    def CashEtf(self):
        return self._cash.Value

    @CashEtf.setter
    def CashEtf(self, value):
        self._cash.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value
    # endregion

    def GetWorkingSecurities(self):
        if self.Equity is None or self.Oil is None or self.CashEtf is None:
            raise Exception("Set securities")
        return [(self.Equity, self.CandleType), (self.Oil, self.CandleType), (self.CashEtf, self.CandleType)]

    def OnReseted(self):
        super(crude_oil_predicts_equity_strategy, self).OnReseted()
        self._wins.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        super(crude_oil_predicts_equity_strategy, self).OnStarted(time)

        for s, dt in self.GetWorkingSecurities():
            self._wins[s] = RollingWindow(self.Lookback + 1)
            self.SubscribeCandles(dt, True, s) \
                .Bind(lambda candle, security=s: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        self._wins[security].add(candle.ClosePrice)
        day = candle.OpenTime.Date
        if day == self._last_day:
            return
        self._last_day = day
        if day.Day == 1:
            self.Rebalance()

    def Rebalance(self):
        win = self._wins.get(self.Oil)
        if win is None or not win.is_full():
            return
        oil_ret = (win.last() - win[0]) / win[0]
        target = self.Equity if oil_ret > 0 else self.CashEtf
        self.MoveTo(target)

    def MoveTo(self, target):
        for position in self.Positions:
            if position.Security != target:
                self.Move(position.Security, 0)
        portfolio_value = self.Portfolio.CurrentValue or 0
        price = self.GetLatestPrice(target)
        if price > 0:
            self.Move(target, portfolio_value / price)

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, security, target):
        diff = target - self.PositionBy(security)
        price = self.GetLatestPrice(security)
        if price <= 0 or Math.Abs(diff) * price < 100:
            return
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "OilEq"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def CreateClone(self):
        return crude_oil_predicts_equity_strategy()

# Helper class for rolling window functionality
class RollingWindow:
    def __init__(self, n):
        self._n = n
        self._q = []

    def add(self, v):
        if len(self._q) == self._n:
            self._q.pop(0)
        self._q.append(v)

    def is_full(self):
        return len(self._q) == self._n

    def last(self):
        return self._q[-1]

    def __getitem__(self, idx):
        return self._q[idx]
