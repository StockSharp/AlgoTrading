import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ilan16_dynamic_strategy(Strategy):
    def __init__(self):
        super(ilan16_dynamic_strategy, self).__init__()
        self._pip_step = self.Param("PipStep", 50000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Pip Step", "Distance in price steps between grid levels", "Trading")
        self._take_profit = self.Param("TakeProfit", 30000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit", "Profit target from average price in price steps", "Trading")
        self._max_trades = self.Param("MaxTrades", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Trades", "Maximum number of averaging entries", "Trading")
        self._start_long = self.Param("StartLong", True) \
            .SetDisplay("Start Long", "Open first trade as long", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._trade_count = 0
        self._last_entry_price = 0.0
        self._avg_price = 0.0
        self._is_long = True

    @property
    def pip_step(self):
        return self._pip_step.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def max_trades(self):
        return self._max_trades.Value

    @property
    def start_long(self):
        return self._start_long.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._trade_count = 0
        self._last_entry_price = 0.0
        self._avg_price = 0.0
        self._is_long = self.start_long

    def OnReseted(self):
        super(ilan16_dynamic_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted(self, time):
        super(ilan16_dynamic_strategy, self).OnStarted(time)
        self._is_long = self.start_long
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 1.0
        price = float(candle.ClosePrice)

        # No position - open initial entry
        if self.Position == 0:
            if self._is_long:
                self.BuyMarket()
            else:
                self.SellMarket()
            self._trade_count = 1
            self._last_entry_price = price
            self._avg_price = price
            return

        # Check take profit: close entire basket
        if self._is_long and price >= self._avg_price + float(self.take_profit) * step:
            self._close_all()
            return
        elif not self._is_long and price <= self._avg_price - float(self.take_profit) * step:
            self._close_all()
            return

        # Check for grid averaging entry (price moved against us)
        if self._is_long and self._trade_count < self.max_trades and self._last_entry_price - price >= float(self.pip_step) * step:
            self.BuyMarket()
            self._trade_count += 1
            self._avg_price = (self._avg_price * (self._trade_count - 1) + price) / self._trade_count
            self._last_entry_price = price
        elif not self._is_long and self._trade_count < self.max_trades and price - self._last_entry_price >= float(self.pip_step) * step:
            self.SellMarket()
            self._trade_count += 1
            self._avg_price = (self._avg_price * (self._trade_count - 1) + price) / self._trade_count
            self._last_entry_price = price

    def _close_all(self):
        pos = self.Position
        if pos > 0:
            for i in range(int(abs(pos))):
                self.SellMarket()
        elif pos < 0:
            for i in range(int(abs(pos))):
                self.BuyMarket()
        self._reset_state()

    def CreateClone(self):
        return ilan16_dynamic_strategy()
