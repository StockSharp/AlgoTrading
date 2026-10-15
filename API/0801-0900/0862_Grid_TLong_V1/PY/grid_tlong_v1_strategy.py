import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class grid_tlong_v1_strategy(Strategy):
    """
    Grid TLong V1 strategy.
    Always keeps a position, starting long. Once the position gains Percent percent from its entry price it is closed and restarted
    in the same direction; once it loses Percent percent it is reversed. With UseLimitOrders the orders are limit orders at the
    candle close instead of market orders.
    """

    def __init__(self):
        super(grid_tlong_v1_strategy, self).__init__()
        self._percent = self.Param("Percent", 1.0).SetGreaterThanZero().SetDisplay("Percent", "Grid step in percent of the entry price", "Trading")
        self._use_limit_orders = self.Param("UseLimitOrders", False).SetDisplay("Use Limit Orders", "Use limit orders at the candle close instead of market orders", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_tlong_v1_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(grid_tlong_v1_strategy, self).OnStarted2(time)

        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _buy(self, volume, price):
        if self._use_limit_orders.Value:
            self.BuyLimit(price, volume)
        else:
            self.BuyMarket(volume)

    def _sell(self, volume, price):
        if self._use_limit_orders.Value:
            self.SellLimit(price, volume)
        else:
            self.SellMarket(volume)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price = candle.ClosePrice
        close = float(price)

        # Unfilled limit orders from the previous bar are replaced by the current decision.
        if self._use_limit_orders.Value:
            self.CancelActiveOrders()

        if self.Position == 0:
            self._buy(self.Volume, price)
            self._entry_price = close
            return

        if self._entry_price <= 0.0:
            self._entry_price = close
            return

        change = (close - self._entry_price) / self._entry_price * 100.0
        profit = change if self.Position > 0 else -change
        percent = float(self._percent.Value)

        if profit >= percent:
            # Restart the position in the same direction at the new grid level.
            if self.Position > 0:
                self._sell(self.Position, price)
                self._buy(self.Volume, price)
            else:
                self._buy(-self.Position, price)
                self._sell(self.Volume, price)
            self._entry_price = close
        elif profit <= -percent:
            if self.Position > 0:
                self._sell(self.Volume + abs(self.Position), price)
            else:
                self._buy(self.Volume + abs(self.Position), price)
            self._entry_price = close

    def CreateClone(self):
        return grid_tlong_v1_strategy()
