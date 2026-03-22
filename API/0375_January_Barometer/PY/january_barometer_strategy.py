import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order


class january_barometer_strategy(Strategy):
    """January barometer strategy that rotates between the primary instrument and a benchmark proxy based on the primary January return."""

    def __init__(self):
        super(january_barometer_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Benchmark Security Id", "Defensive benchmark proxy", "General")

        self._min_trade_usd = self.Param("MinTradeUsd", 200.0) \
            .SetRange(1.0, 100000.0) \
            .SetDisplay("Min trade USD", "Minimum order value", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._latest_prices = {}
        self._january_open = 0.0
        self._decision_year = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        sec2_id = str(self._security2_id.Value)
        if sec2_id:
            s = Security()
            s.Id = sec2_id
            result.append((s, self.candle_type))
        return result

    def OnReseted(self):
        super(january_barometer_strategy, self).OnReseted()
        self._latest_prices = {}
        self._january_open = 0.0
        self._decision_year = 0

    def OnStarted(self, time):
        super(january_barometer_strategy, self).OnStarted(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Benchmark security identifier is not specified.")

        primary_subscription = self.SubscribeCandles(self.candle_type, True, self.Security)
        primary_subscription.Bind(lambda candle: self.ProcessCandle(candle, self.Security)).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, security):
        if candle.State != CandleStates.Finished:
            return

        self._latest_prices[security] = candle.ClosePrice

        if security != self.Security:
            return

        day = candle.OpenTime.Date

        if day.Month == 1 and self._january_open == 0.0:
            self._january_open = float(candle.OpenPrice)

        if day.Month == 2 and self._decision_year != day.Year and self._january_open > 0.0:
            self._decision_year = day.Year
            january_return = (float(candle.ClosePrice) - self._january_open) / self._january_open
            self.Rebalance(january_return > 0.0)

    def Rebalance(self, bullish):
        weight = 1.0 if bullish else -1.0
        self.Move(self.Security, weight)

    def Move(self, security, weight):
        price = self.GetLatestPrice(security)
        if price <= 0.0:
            return

        portfolio_value = float(self.Portfolio.CurrentValue) if self.Portfolio.CurrentValue is not None else 0.0
        target = weight * portfolio_value / price
        pos_val = self.GetPositionValue(security, self.Portfolio)
        current_pos = float(pos_val) if pos_val is not None else 0.0
        diff = target - current_pos

        min_trade = float(self._min_trade_usd.Value)
        if abs(diff) * price < min_trade:
            return

        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy if diff > 0 else Sides.Sell
        order.Volume = abs(diff)
        order.Type = OrderTypes.Market
        order.Comment = "JanBar"
        self.RegisterOrder(order)

    def GetLatestPrice(self, security):
        if security in self._latest_prices:
            return float(self._latest_prices[security])
        return 0.0

    def CreateClone(self):
        return january_barometer_strategy()
