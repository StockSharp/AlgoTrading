import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class dispersion_trading_strategy(Strategy):
    """Dispersion trading strategy."""

    def __init__(self):
        super(dispersion_trading_strategy, self).__init__()

        self._constituents = self.Param("Constituents", Array.Empty[Security]()) \
            .SetDisplay("Constituents", "Index constituent securities", "General")

        self._lookback_days = self.Param("LookbackDays", 60) \
            .SetDisplay("Lookback Days", "Days for rolling correlation", "Parameters")

        self._corr_threshold = self.Param("CorrThreshold", 0.4) \
            .SetDisplay("Correlation Threshold", "Average correlation threshold", "Parameters")

        self._min_trade_usd = self.Param("MinTradeUsd", 100.0) \
            .SetDisplay("Minimum Trade USD", "Minimal order value", "Risk")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")

        self._windows = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue
        self._open = False

    # region Properties
    @property
    def Constituents(self):
        return self._constituents.Value

    @Constituents.setter
    def Constituents(self, value):
        self._constituents.Value = value

    @property
    def LookbackDays(self):
        return self._lookback_days.Value

    @LookbackDays.setter
    def LookbackDays(self, value):
        self._lookback_days.Value = value

    @property
    def CorrThreshold(self):
        return self._corr_threshold.Value

    @CorrThreshold.setter
    def CorrThreshold(self, value):
        self._corr_threshold.Value = value

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
        # Преобразуем .NET Array в Python list и объединяем с бенчмарком
        constituents_list = list(self.Constituents) if self.Constituents is not None else []
        securities = constituents_list + [self.Security]
        
        return [(s, self.CandleType) for s in securities]

    def OnReseted(self):
        super(dispersion_trading_strategy, self).OnReseted()
        self._windows.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue
        self._open = False

    def OnStarted(self, time):
        super(dispersion_trading_strategy, self).OnStarted(time)
        if self.Security is None:
            raise Exception("IndexSec is not set.")
        if self.Constituents is None or len(self.Constituents) == 0:
            raise Exception("Constituents collection is empty.")
        for sec, dt in self.GetWorkingSecurities():
            self._windows[sec] = RollingWindow(self.LookbackDays + 1)
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        self._windows[security].add(candle.ClosePrice)
        day = candle.OpenTime.Date
        if day == self._last_day:
            return
        self._last_day = day
        if all(w.is_full() for w in self._windows.values()):
            self.CheckDispersion()

    def CheckDispersion(self):
        index_returns = returns(self._windows[self.Security])
        # Преобразуем Constituents в список Python
        constituents_list = list(self.Constituents) if self.Constituents is not None else []
        cons_returns = [returns(self._windows[s]) for s in constituents_list]
        if not cons_returns:
            return
        corrs = [corr(index_returns, r) for r in cons_returns]
        avg = sum(corrs) / len(corrs)
        if not self._open and avg < self.CorrThreshold:
            self.OpenDispersion()
        elif self._open and avg >= self.CorrThreshold:
            self.CloseAll()

    def OpenDispersion(self):
        # Преобразуем Constituents в список Python
        constituents_list = list(self.Constituents) if self.Constituents is not None else []
        count = len(constituents_list)
        portfolio_value = self.Portfolio.CurrentValue or 0
        cap_leg = portfolio_value * 0.5
        each_long = cap_leg / count
        for s in constituents_list:
            price = self.GetLatestPrice(s)
            if price > 0:
                self.TradeToTarget(s, each_long / price)
        index_price = self.GetLatestPrice(self.Security)
        if index_price > 0:
            self.TradeToTarget(self.Security, -cap_leg / index_price)
        self._open = True

    def CloseAll(self):
        for position in self.Positions:
            self.TradeToTarget(position.Security, 0)
        self._open = False

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def TradeToTarget(self, sec, tgt):
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
        order.Comment = "Dispersion"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def CreateClone(self):
        return dispersion_trading_strategy()

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
    
    def to_array(self):
        return list(self._q)

# Helper functions for returns and correlation
def returns(win):
    arr = win.to_array()
    return [(arr[i] - arr[i-1]) / arr[i-1] for i in range(1, len(arr))]

def corr(x, y):
    n = min(len(x), len(y))
    if n == 0:
        return 0
    mx = sum(x[:n]) / n
    my = sum(y[:n]) / n
    num = 0
    dx = 0
    dy = 0
    for i in range(n):
        a = x[i] - mx
        b = y[i] - my
        num += a * b
        dx += a * a
        dy += b * b
    if dx <= 0 or dy <= 0:
        return 0
    return num / Math.Sqrt(dx * dy)
