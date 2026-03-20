import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class reco_rsi_grid_strategy(Strategy):
    def __init__(self):
        super(reco_rsi_grid_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI indicator period", "Signal")
        self._rsi_sell_zone = self.Param("RsiSellZone", 70.0) \
            .SetDisplay("RSI Sell Zone", "RSI level to sell", "Signal")
        self._rsi_buy_zone = self.Param("RsiBuyZone", 30.0) \
            .SetDisplay("RSI Buy Zone", "RSI level to buy", "Signal")
        self._grid_step = self.Param("GridStep", 200.0) \
            .SetDisplay("Grid Step", "Distance between grid orders", "Grid")
        self._max_orders = self.Param("MaxOrders", 5) \
            .SetDisplay("Max Orders", "Maximum number of grid orders", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._last_order_price = 0.0
        self._last_order_is_buy = False
        self._orders_total = 0
        self._entry_price = 0.0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_sell_zone(self):
        return self._rsi_sell_zone.Value

    @property
    def rsi_buy_zone(self):
        return self._rsi_buy_zone.Value

    @property
    def grid_step(self):
        return self._grid_step.Value

    @property
    def max_orders(self):
        return self._max_orders.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(reco_rsi_grid_strategy, self).OnReseted()
        self._last_order_price = 0.0
        self._last_order_is_buy = False
        self._orders_total = 0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(reco_rsi_grid_strategy, self).OnStarted(time)
        self._last_order_price = 0.0
        self._last_order_is_buy = False
        self._orders_total = 0
        self._entry_price = 0.0
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _get_signal(self, price, rsi_value):
        grid_step = float(self.grid_step)
        max_ord = int(self.max_orders)
        if self._orders_total == 0:
            if rsi_value >= float(self.rsi_sell_zone):
                return -1
            if rsi_value <= float(self.rsi_buy_zone):
                return 1
            return 0
        if max_ord > 0 and self._orders_total >= max_ord:
            self._orders_total = 0
            self._last_order_price = 0.0
            return 0
        if self._last_order_is_buy and price <= self._last_order_price - grid_step:
            return 1
        elif not self._last_order_is_buy and price >= self._last_order_price + grid_step:
            return -1
        return 0

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        price = float(candle.ClosePrice)
        grid_step = float(self.grid_step)
        if self._orders_total > 0 and self._entry_price > 0:
            if self.Position > 0:
                unrealized = price - self._entry_price
            else:
                unrealized = self._entry_price - price
            if unrealized > grid_step * 0.5:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._orders_total = 0
                self._last_order_price = 0.0
                self._entry_price = 0.0
                return
        signal = self._get_signal(price, rsi_value)
        if signal > 0 and self.Position <= 0:
            self.BuyMarket()
            self._last_order_is_buy = True
            self._last_order_price = price
            self._entry_price = price
            self._orders_total += 1
        elif signal < 0 and self.Position >= 0:
            self.SellMarket()
            self._last_order_is_buy = False
            self._last_order_price = price
            self._entry_price = price
            self._orders_total += 1

    def CreateClone(self):
        return reco_rsi_grid_strategy()
