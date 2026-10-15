import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, TimeSpan, Math, Array
from System.Collections.Generic import IEnumerable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *

class dispersion_trading_strategy(Strategy):
    """Dispersion trading strategy.
    Trades an equity index against its constituent securities when the average correlation falls below a threshold.
    """

    def __init__(self):
        super(dispersion_trading_strategy, self).__init__()

        self._constituents = self.Param[IEnumerable[Security]]("Constituents", None) \
            .SetDisplay("Constituents", "Index constituent securities", "General")

        self._lookback_days = self.Param("LookbackDays", 60) \
            .SetDisplay("Lookback Days", "Days for rolling correlation", "Parameters")

        self._corr_threshold = self.Param("CorrThreshold", 0.4) \
            .SetDisplay("Correlation Threshold", "Average correlation threshold", "Parameters")

        self._min_trade_usd = self.Param("MinTradeUsd", 100.0) \
            .SetDisplay("Minimum Trade USD", "Minimal order value", "Risk")

        self._candle_type = self.Param("CandleType", tf(5)) \
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
        constituents_list = list(self.Constituents) if self.Constituents is not None else []
        securities = constituents_list + [self.Security]
        return [(s, self.CandleType) for s in securities]

    def OnReseted(self):
        super(dispersion_trading_strategy, self).OnReseted()
        self._windows.clear()
        self._latest_prices.clear()
        self._last_day = DateTime.MinValue
        self._open = False

    def OnStarted2(self, time):
        super(dispersion_trading_strategy, self).OnStarted2(time)

        if self.Security is None:
            raise Exception("IndexSec is not set.")

        constituents_list = list(self.Constituents) if self.Constituents is not None else []
        if len(constituents_list) == 0:
            raise Exception("Constituents collection is empty.")

        for sec, dt in self.GetWorkingSecurities():
            self._windows[sec] = RollingWindow(self.LookbackDays + 1)
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self._process_candle(candle, security)) \
                .Start()

    def _process_candle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = float(candle.ClosePrice)
        self._windows[security].add(float(candle.ClosePrice))

        day = candle.OpenTime.Date
        if day == self._last_day:
            return

        self._last_day = day

        if not all(w.is_full() for w in self._windows.values()):
            return

        self._evaluate_signal()

    def _evaluate_signal(self):
        index_returns = _returns(self._windows[self.Security])

        constituents_list = list(self.Constituents) if self.Constituents is not None else []
        corrs = []
        for s in constituents_list:
            corrs.append(_corr(_returns(self._windows[s]), index_returns))

        if len(corrs) == 0:
            return

        avg = sum(corrs) / len(corrs)

        if avg < self.CorrThreshold and not self._open:
            self._open_dispersion()
        elif avg >= self.CorrThreshold and self._open:
            self._close_all()

    def _open_dispersion(self):
        constituents_list = list(self.Constituents) if self.Constituents is not None else []
        count = len(constituents_list)
        if count == 0:
            return

        portfolio_value = self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else 0.0
        cap_leg = float(portfolio_value) * 0.5
        each_long = cap_leg / count

        for s in constituents_list:
            price = self._get_latest_price(s)
            if price > 0:
                self._trade_to_target(s, each_long / price)

        index_price = self._get_latest_price(self.Security)
        if index_price > 0:
            self._trade_to_target(self.Security, -cap_leg / index_price)

        self._open = True
        self.LogInfo("Opened dispersion spread")

    def _close_all(self):
        for position in self.Positions:
            self._trade_to_target(position.Security, 0.0)

        self._open = False
        self.LogInfo("Closed dispersion spread")

    def _get_latest_price(self, security):
        return self._latest_prices.get(security, 0.0)

    def _trade_to_target(self, sec, tgt):
        diff = tgt - self._position_by(sec)
        price = self._get_latest_price(sec)

        if price <= 0 or abs(diff) * price < self.MinTradeUsd:
            return

        order = Order()
        order.Security = sec
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "Dispersion"
        self.RegisterOrder(order)

    def _position_by(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return float(val) if val is not None else 0.0

    def CreateClone(self):
        return dispersion_trading_strategy()


class RollingWindow:
    """Rolling window of fixed size, mimicking the C# RollingWindow inner class."""

    def __init__(self, size):
        self._size = size
        self._queue = []

    def add(self, value):
        if len(self._queue) == self._size:
            self._queue.pop(0)
        self._queue.append(value)

    def is_full(self):
        return len(self._queue) == self._size

    def last(self):
        return self._queue[-1]

    def to_array(self):
        return list(self._queue)


def _returns(win):
    """Compute simple returns from a rolling window of prices."""
    arr = win.to_array()
    r = []
    for i in range(1, len(arr)):
        r.append((arr[i] - arr[i - 1]) / arr[i - 1])
    return r


def _corr(x, y):
    """Compute Pearson correlation between two return arrays."""
    n = min(len(x), len(y))
    if n == 0:
        return 0.0

    mx = sum(x[:n]) / n
    my = sum(y[:n]) / n

    num = 0.0
    dx = 0.0
    dy = 0.0

    for i in range(n):
        a = x[i] - mx
        b = y[i] - my
        num += a * b
        dx += a * a
        dy += b * b

    if dx <= 0 or dy <= 0:
        return 0.0

    return num / math.sqrt(dx * dy)
