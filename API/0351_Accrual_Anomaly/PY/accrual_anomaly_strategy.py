import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")

from System import DateTime, TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from datatype_extensions import *

class accrual_anomaly_strategy(Strategy):
    """Strategy implementing the accrual anomaly factor."""

    def __init__(self):
        super(accrual_anomaly_strategy, self).__init__()

        self._universe = self.Param("Universe", []) \
            .SetDisplay("Universe", "Securities to trade", "General")

        self._deciles = self.Param("Deciles", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Deciles", "Number of decile buckets", "General")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Candle type used for rebalancing", "General")

        self._prev = {}
        self._weights = {}
        self._latest_prices = {}
        self._last_day = DateTime.MinValue

    # region Properties
    @property
    def Universe(self):
        """Trading universe."""
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def Deciles(self):
        """Number of decile buckets."""
        return self._deciles.Value

    @Deciles.setter
    def Deciles(self, value):
        self._deciles.Value = value

    @property
    def CandleType(self):
        """Candle type used to detect rebalancing date."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value
    # endregion

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe]

    def OnReseted(self):
        super(accrual_anomaly_strategy, self).OnReseted()
        self._latest_prices.clear()
        self._weights.clear()
        self._prev.clear()
        self._last_day = DateTime.MinValue

    def OnStarted(self, time):
        if self.Universe is None or len(self.Universe) == 0:
            raise Exception("Universe cannot be empty.")

        super(accrual_anomaly_strategy, self).OnStarted(time)

        for sec, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()

    def ProcessCandle(self, candle, security):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Store the latest closing price for this security
        self._latest_prices[security] = candle.ClosePrice

        d = candle.OpenTime.Date
        if d == self._last_day:
            return
        self._last_day = d

        # Rebalance on the first trading day of May
        if d.Month == 5 and d.Day == 1:
            self.Rebalance()

    def Rebalance(self):
        accr = {}
        for s in self.Universe:
            ok, cur = self.TryGetFundamentals(s)
            if not ok:
                continue
            if s in self._prev:
                accr[s] = self.CalcAccrual(cur, self._prev[s])
            self._prev[s] = cur

        if len(accr) < self.Deciles * 2:
            return

        bucket = len(accr) // self.Deciles
        sorted_items = sorted(accr.items(), key=lambda kv: kv[1])
        longs = [kv[0] for kv in sorted_items[:bucket]]
        shorts = [kv[0] for kv in sorted_items[-bucket:]]

        self._weights.clear()
        wl = 1.0 / len(longs)
        ws = -1.0 / len(shorts)
        for s in longs:
            self._weights[s] = wl
        for s in shorts:
            self._weights[s] = ws

        for position in self.Positions:
            if position.Security not in self._weights:
                self.Move(position.Security, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec, weight in self._weights.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                self.Move(sec, weight * portfolio_value / price)

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def Move(self, sec, tgt):
        diff = tgt - self.PositionBy(sec)
        price = self.GetLatestPrice(sec)
        if price <= 0 or Math.Abs(diff) * price < 100:
            return

        order = Order()
        order.Security = sec
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "Accrual"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def TryGetFundamentals(self, s):
        return False, None

    def CalcAccrual(self, cur, prev):
        return 0

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return accrual_anomaly_strategy()

class BalanceSnapshot:
    def __init__(self, a, b):
        self.a = a
        self.b = b
