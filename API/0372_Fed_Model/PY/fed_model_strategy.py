import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order
from datatype_extensions import *


class fed_model_strategy(Strategy):
    """Fed Model yield-gap timing strategy."""

    def __init__(self):
        super(fed_model_strategy, self).__init__()

        self._univ = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._bond = self.Param[Security]("BondYieldSym", None) \
            .SetDisplay("Bond Yield", "10-year Treasury yield", "Data")

        self._earn = self.Param[Security]("EarningsYieldSym", None) \
            .SetDisplay("Earnings Yield", "Earnings yield series", "Data")

        self._months = self.Param("RegressionMonths", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Regression Months", "Months in regression window", "Settings")

        self._tf = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._min_usd = self.Param("MinTradeUsd", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trade USD", "Minimum trade value", "Risk Management")

        n = self.RegressionMonths + 1
        self._eq = RollingWin(n)
        self._gap = RollingWin(n)
        self._rf = RollingWin(n)
        self._latest_prices = {}
        self._last_month = DateTime.MinValue

    # region properties
    @property
    def Universe(self):
        return self._univ.Value

    @Universe.setter
    def Universe(self, value):
        self._univ.Value = value

    @property
    def BondYieldSym(self):
        return self._bond.Value

    @BondYieldSym.setter
    def BondYieldSym(self, value):
        self._bond.Value = value

    @property
    def EarningsYieldSym(self):
        return self._earn.Value

    @EarningsYieldSym.setter
    def EarningsYieldSym(self, value):
        self._earn.Value = value

    @property
    def RegressionMonths(self):
        return self._months.Value

    @RegressionMonths.setter
    def RegressionMonths(self, value):
        self._months.Value = value

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
        res = [(s, self.CandleType) for s in self.Universe]
        if self.BondYieldSym is not None:
            res.append((self.BondYieldSym, self.CandleType))
        if self.EarningsYieldSym is not None:
            res.append((self.EarningsYieldSym, self.CandleType))
        return res

    def OnReseted(self):
        super(fed_model_strategy, self).OnReseted()
        self._eq.Clear()
        self._gap.Clear()
        self._rf.Clear()
        self._latest_prices.clear()
        self._last_month = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe is empty.")

        super(fed_model_strategy, self).OnStarted(time)

        for sec, tf in self.GetWorkingSecurities():
            self.SubscribeCandles(tf, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice
        self.OnDaily(candle, security)

    def OnDaily(self, candle, security):
        d = candle.OpenTime.Date
        if d.Day != 1 or self._last_month == d or security != (self.Universe[0] if len(self.Universe) > 0 else None):
            return
        self._last_month = d

        self._eq.Add(candle.ClosePrice)
        self._rf.Add(self.GetRF(d))
        gap = self.GetYieldGap(d)
        if gap is None:
            return
        self._gap.Add(gap)

        if not self._eq.Full or not self._gap.Full:
            return

        x = self._gap.Data
        yret = []
        for i in range(1, self._eq.Size):
            yret.append((self._eq.Data[i] - self._eq.Data[i-1]) / self._eq.Data[i-1] - self._rf.Data[i-1])

        n = len(yret)
        meanX = sum(x[:n]) / n
        meanY = sum(yret) / n
        cov = 0.0
        varX = 0.0
        for i in range(n):
            dx = x[i] - meanX
            cov += dx * (yret[i] - meanY)
            varX += dx * dx
        if varX == 0:
            return
        beta = cov / varX
        alpha = meanY - beta * meanX
        forecast = alpha + beta * x[-1]

        equity = self.Universe[0]
        cash = self.Universe[1] if len(self.Universe) > 1 else None

        if forecast > 0:
            self.Move(equity, 1.0)
            if cash is not None:
                self.Move(cash, 0)
        else:
            self.Move(equity, 0)
            if cash is not None:
                self.Move(cash, 1.0)

    def Move(self, security, weight):
        if security is None:
            return
        portfolio_value = self.Portfolio.CurrentValue or 0
        price = self.GetLatestPrice(security)
        if price <= 0:
            return
        tgt = weight * portfolio_value / price
        diff = tgt - self.PositionBy(security)
        if Math.Abs(diff) * price < self.MinTradeUsd:
            return
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "FedModel"
        self.RegisterOrder(order)

    def PositionBy(self, security):
        val = self.GetPositionValue(security, self.Portfolio)
        return val if val is not None else 0

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def GetRF(self, d):
        return 0.0002

    def GetYieldGap(self, d):
        ok1, ey = self.SeriesVal(self.EarningsYieldSym, d)
        ok2, y10 = self.SeriesVal(self.BondYieldSym, d)
        if not ok1 or not ok2:
            return None
        return ey - y10

    def SeriesVal(self, s, d):
        return False, 0

    def CreateClone(self):
        return fed_model_strategy()


class RollingWin:
    def __init__(self, size=0):
        self.Data = []
        self._max = size

    def SetSize(self, n):
        self.Data = []
        self._max = n

    def Add(self, v):
        self.Data.append(v)
        if len(self.Data) > self._max:
            self.Data.pop(0)

    def Clear(self):
        self.Data = []

    @property
    def Full(self):
        return len(self.Data) == self._max

    @property
    def Size(self):
        return len(self.Data)
