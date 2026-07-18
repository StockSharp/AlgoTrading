import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class bot_for_spot_market_custom_grid_strategy(Strategy):
    def __init__(self):
        super(bot_for_spot_market_custom_grid_strategy, self).__init__()
        self._next_entry_percent = self.Param("NextEntryPercent", 10.0) \
            .SetDisplay("Next Entry Less Than (%)", "Price drop from last entry to add new order", "Parameters")
        self._profit_percent = self.Param("ProfitPercent", 15.0) \
            .SetDisplay("Profit (%)", "Profit target from average price", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_entry_price = 0.0
        self._avg_price = 0.0
        self._initial_order_sent = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bot_for_spot_market_custom_grid_strategy, self).OnReseted()
        self._last_entry_price = 0.0
        self._avg_price = 0.0
        self._initial_order_sent = False

    def OnStarted2(self, time):
        super(bot_for_spot_market_custom_grid_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        if self.Position <= 0 and not self._initial_order_sent:
            self.BuyMarket()
            self._last_entry_price = price
            self._avg_price = price
            self._initial_order_sent = True
            return
        next_entry = float(self._next_entry_percent.Value)
        if self.Position > 0 and self._last_entry_price > 0 and price < self._last_entry_price * (1.0 - next_entry / 100.0):
            self.BuyMarket()
            self._avg_price = (self._avg_price + price) / 2.0
            self._last_entry_price = price
            return
        profit_pct = float(self._profit_percent.Value)
        if self.Position > 0 and self._avg_price > 0:
            target = self._avg_price * (1.0 + profit_pct / 100.0)
            if price > target:
                self.SellMarket()
                self._last_entry_price = 0.0
                self._avg_price = 0.0
                self._initial_order_sent = False

    def CreateClone(self):
        return bot_for_spot_market_custom_grid_strategy()
