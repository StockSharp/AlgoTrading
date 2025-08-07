import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import DateTime, TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from datatype_extensions import *


class dollar_carry_trade_strategy(Strategy):
    """Dollar carry trade strategy (High-Level API)."""

    def __init__(self):
        super(dollar_carry_trade_strategy, self).__init__()

        self._pairs = self.Param("Pairs", Array.Empty[Security]()) \
            .SetDisplay("Pairs", "USD crosses (required)", "Universe")

        self._k = self.Param("K", 3) \
            .SetDisplay("K", "# of currencies per leg", "Ranking")

        self._min_usd = self.Param("MinTradeUsd", 100.0) \
            .SetDisplay("Min Trade $", "Ignore tiny rebalances", "Risk")

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._carry = {}
        self._weights = {}
        self._latest_prices = {}
        self._last_rebalance_date = DateTime.MinValue

    # region Properties
    @property
    def Pairs(self):
        return self._pairs.Value

    @Pairs.setter
    def Pairs(self, value):
        self._pairs.Value = value

    @property
    def K(self):
        return self._k.Value

    @K.setter
    def K(self, value):
        self._k.Value = value

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
        if not self.Pairs:
            raise Exception("Pairs list is empty – populate before start.")
        return [(s, self.CandleType) for s in self.Pairs]

    def OnReseted(self):
        super(dollar_carry_trade_strategy, self).OnReseted()
        self._carry.clear()
        self._weights.clear()
        self._latest_prices.clear()
        self._last_rebalance_date = DateTime.MinValue

    def OnStarted(self, time):
        super(dollar_carry_trade_strategy, self).OnStarted(time)
        for sec, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, sec) \
                .Bind(lambda candle, security=sec: self.ProcessCandle(candle, security)) \
                .Start()
        self.LogInfo("Dollar Carry strategy started. Universe = {0} pairs, K = {1}".format(len(self.Pairs), self.K))

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return
        self._latest_prices[security] = candle.ClosePrice
        candle_date = candle.OpenTime.Date
        if candle_date.Day == 1 and candle_date != self._last_rebalance_date:
            self._last_rebalance_date = candle_date
            self.Rebalance()

    def Rebalance(self):
        self._carry.clear()
        for p in self.Pairs:
            ok, c = self.TryGetCarry(p)
            if ok:
                self._carry[p] = c
        if len(self._carry) < self.K * 2:
            self.LogInfo("Not enough carry data yet.")
            return
        high_carry = [kv[0] for kv in sorted(self._carry.items(), key=lambda kv: kv[1], reverse=True)[:self.K]]
        low_carry = [kv[0] for kv in sorted(self._carry.items(), key=lambda kv: kv[1])[:self.K]]

        self._weights.clear()
        w_long = 1.0 / len(low_carry)
        w_short = -1.0 / len(high_carry)
        for s in low_carry:
            self._weights[s] = w_long
        for s in high_carry:
            self._weights[s] = w_short

        for position in list(self.Positions):
            if position.Security not in self._weights:
                self.TradeToTarget(position.Security, 0)

        portfolio_value = self.Portfolio.CurrentValue or 0
        for sec, w in self._weights.items():
            price = self.GetLatestPrice(sec)
            if price > 0:
                tgt_qty = w * portfolio_value / price
                self.TradeToTarget(sec, tgt_qty)
        self.LogInfo("Rebalanced: Long {0} | Short {1}".format(len(low_carry), len(high_carry)))

    def GetLatestPrice(self, security):
        return self._latest_prices.get(security, 0)

    def TradeToTarget(self, sec, tgt_qty):
        diff = tgt_qty - self.PositionBy(sec)
        price = self.GetLatestPrice(sec)
        if price <= 0 or Math.Abs(diff) * price < self.MinTradeUsd:
            return
        order = Order()
        order.Security = sec
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = Math.Abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "DollarCarry"
        self.RegisterOrder(order)

    def PositionBy(self, sec):
        val = self.GetPositionValue(sec, self.Portfolio)
        return val if val is not None else 0

    def TryGetCarry(self, pair):
        return False, 0.0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return dollar_carry_trade_strategy()
