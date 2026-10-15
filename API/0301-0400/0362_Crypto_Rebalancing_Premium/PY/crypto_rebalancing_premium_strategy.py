import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, DayOfWeek
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security


class crypto_rebalancing_premium_strategy(Strategy):
    """Equal-weight crypto basket strategy that rebalances the primary and secondary instruments on a weekly schedule."""

    def __init__(self):
        super(crypto_rebalancing_premium_strategy, self).__init__()

        self._secondary_security_id = self.Param("SecondarySecurityId", "TONUSDT@BNBFT") \
            .SetDisplay("Second Security Id", "Identifier of the secondary crypto security", "General")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetRange(10.0, 10000.0) \
            .SetDisplay("Min Trade USD", "Minimum dollar amount per trade", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._secondary_security = None
        self._latest_primary_price = 0.0
        self._latest_secondary_price = 0.0
        self._last_rebalance_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        sec2_id = str(self._secondary_security_id.Value)
        if sec2_id:
            s = Security()
            s.Id = sec2_id
            result.append((s, self.candle_type))
        return result

    def OnReseted(self):
        super(crypto_rebalancing_premium_strategy, self).OnReseted()
        self._secondary_security = None
        self._latest_primary_price = 0.0
        self._latest_secondary_price = 0.0
        self._last_rebalance_time = None

    def OnStarted2(self, time):
        super(crypto_rebalancing_premium_strategy, self).OnStarted2(time)

        sec2_id = str(self._secondary_security_id.Value)
        if not sec2_id:
            raise Exception("Secondary crypto security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._secondary_security = s

        primary_subscription = self.SubscribeCandles(self.candle_type, True, self.Security)
        secondary_subscription = self.SubscribeCandles(self.candle_type, True, self._secondary_security)

        primary_subscription.Bind(lambda candle: self.ProcessCandle(candle, True)).Start()
        secondary_subscription.Bind(lambda candle: self.ProcessCandle(candle, False)).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawCandles(area, secondary_subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, is_primary):
        if candle.State != CandleStates.Finished:
            return

        if is_primary:
            self._latest_primary_price = float(candle.ClosePrice)
        else:
            self._latest_secondary_price = float(candle.ClosePrice)

        if self._latest_primary_price <= 0.0 or self._latest_secondary_price <= 0.0:
            return

        if self._last_rebalance_time is not None and candle.OpenTime == self._last_rebalance_time:
            return

        if candle.OpenTime.DayOfWeek != DayOfWeek.Monday or candle.OpenTime.Hour != 0:
            return

        self._last_rebalance_time = candle.OpenTime
        self.Rebalance()

    def Rebalance(self):
        portfolio_value = 0.0
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            portfolio_value = float(self.Portfolio.CurrentValue)

        if portfolio_value <= 0.0:
            return

        # Equal weight means equal value, not equal unit count: both legs get the same
        # half of the basket in USD, whatever one unit of each instrument happens to cost.
        target_value = portfolio_value / 2.0

        self.RebalanceSecurity(self.Security, target_value, True)
        self.RebalanceSecurity(self._secondary_security, target_value, False)

    def RebalanceSecurity(self, security, target_value, is_primary):
        price = self._latest_primary_price if is_primary else self._latest_secondary_price
        if price <= 0.0:
            return

        # The target weight is expressed in USD, so the leg price turns it into units to hold.
        target_volume = target_value / price

        pos_val = self.GetPositionValue(security, self.Portfolio)
        current_pos = float(pos_val) if pos_val is not None else 0.0
        diff = target_volume - current_pos

        # An adjustment worth less than this is not worth the round trip.
        min_trade = float(self._min_trade_usd.Value)
        if abs(diff) * price < min_trade:
            return

        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "RebalPrem"
        self.RegisterOrder(order)

    def CreateClone(self):
        return crypto_rebalancing_premium_strategy()
