import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class bezier_reopen_strategy(Strategy):
    def __init__(self):
        super(bezier_reopen_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._b_period = self.Param("BPeriod", 8) \
            .SetDisplay("Bezier Period", "Number of bars for Bezier calculation", "Indicator")
        self._t = self.Param("T", 0.5) \
            .SetDisplay("T", "Bezier curve tension", "Indicator")
        self._price_step_param = self.Param("PriceStep", 300.0) \
            .SetDisplay("Price Step", "Price distance for additional entries", "Trading")
        self._pos_total = self.Param("PosTotal", 1) \
            .SetDisplay("Max Positions", "Maximum number of positions", "Trading")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Enabled", "Allow long entries", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Enabled", "Allow short entries", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long", "Close longs on opposite signal", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short", "Close shorts on opposite signal", "Trading")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop-loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take-profit in price units", "Risk")
        self._prices = []
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._order_count = 0
        self._last_entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def b_period(self):
        return self._b_period.Value
    @property
    def t_param(self):
        return self._t.Value
    @property
    def price_step_param(self):
        return self._price_step_param.Value
    @property
    def pos_total(self):
        return self._pos_total.Value
    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value
    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value
    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value
    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value
    @property
    def stop_loss(self):
        return self._stop_loss.Value
    @property
    def take_profit(self):
        return self._take_profit.Value

    def OnReseted(self):
        super(bezier_reopen_strategy, self).OnReseted()
        self._prices = []
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._order_count = 0
        self._last_entry_price = 0.0

    def OnStarted(self, time):
        super(bezier_reopen_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _factorial(self, value):
        res = 1.0
        for j in range(2, value + 1):
            res *= j
        return res

    def _binomial(self, n, k):
        return self._factorial(n) / (self._factorial(k) * self._factorial(n - k))

    def _compute_bezier(self):
        n = self.b_period
        if len(self._prices) < n + 1:
            return 0.0
        t = self.t_param
        result = 0.0
        for i in range(n + 1):
            price_val = self._prices[len(self._prices) - n - 1 + i]
            result += price_val * self._binomial(n, i) * (t ** i) * ((1 - t) ** (n - i))
        return result

    def _check_stops(self, price):
        if self.Position > 0:
            if self.stop_loss > 0 and price <= self._last_entry_price - self.stop_loss:
                self.SellMarket()
            if self.take_profit > 0 and price >= self._last_entry_price + self.take_profit:
                self.SellMarket()
        elif self.Position < 0:
            if self.stop_loss > 0 and price >= self._last_entry_price + self.stop_loss:
                self.BuyMarket()
            if self.take_profit > 0 and price <= self._last_entry_price - self.take_profit:
                self.BuyMarket()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = (2.0 * float(candle.ClosePrice) + float(candle.HighPrice) + float(candle.LowPrice)) / 4.0
        self._prices.append(price)
        if len(self._prices) > self.b_period + 2:
            self._prices.pop(0)

        bezier_value = self._compute_bezier()
        if bezier_value == 0.0:
            return

        self._prev2 = self._prev1
        self._prev1 = bezier_value

        if self._prev2 == 0.0:
            return

        open_long = False
        open_short = False
        close_long = False
        close_short = False

        if self._prev1 > self._prev2:
            if self.buy_pos_open:
                open_long = True
            if self.sell_pos_close and self.Position < 0:
                close_short = True
        elif self._prev1 < self._prev2:
            if self.sell_pos_open:
                open_short = True
            if self.buy_pos_close and self.Position > 0:
                close_long = True

        if close_long:
            self.SellMarket()
        if close_short:
            self.BuyMarket()

        if open_long and self.Position <= 0:
            self.BuyMarket()
            self._order_count = 1
            self._last_entry_price = float(candle.ClosePrice)
            return

        if open_short and self.Position >= 0:
            self.SellMarket()
            self._order_count = 1
            self._last_entry_price = float(candle.ClosePrice)
            return

        if self.Position > 0 and self._order_count < self.pos_total:
            if float(candle.ClosePrice) - self._last_entry_price >= self.price_step_param:
                self.BuyMarket()
                self._last_entry_price = float(candle.ClosePrice)
                self._order_count += 1
        elif self.Position < 0 and self._order_count < self.pos_total:
            if self._last_entry_price - float(candle.ClosePrice) >= self.price_step_param:
                self.SellMarket()
                self._last_entry_price = float(candle.ClosePrice)
                self._order_count += 1

        self._check_stops(float(candle.ClosePrice))

    def CreateClone(self):
        return bezier_reopen_strategy()
