import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trailing_stop_trigger_manager_strategy(Strategy):

    def __init__(self):
        super(trailing_stop_trigger_manager_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._trailing_points = self.Param("TrailingPoints", 1000) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Points", "Trailing stop distance", "Trailing Management")
        self._trigger_points = self.Param("TriggerPoints", 1500) \
            .SetGreaterThanZero() \
            .SetDisplay("Trigger Points", "Profit to activate trailing", "Trailing Management")

        self._last_entry_price = 0.0
        self._active_stop_price = None
        self._trailing_enabled = False
        self._trailing_distance = 0.0
        self._trigger_distance = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TrailingPoints(self):
        return self._trailing_points.Value

    @TrailingPoints.setter
    def TrailingPoints(self, value):
        self._trailing_points.Value = value

    @property
    def TriggerPoints(self):
        return self._trigger_points.Value

    @TriggerPoints.setter
    def TriggerPoints(self, value):
        self._trigger_points.Value = value

    def OnReseted(self):
        super(trailing_stop_trigger_manager_strategy, self).OnReseted()
        self._last_entry_price = 0.0
        self._active_stop_price = None
        self._trailing_enabled = False
        self._trailing_distance = 0.0
        self._trigger_distance = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(trailing_stop_trigger_manager_strategy, self).OnStarted(time)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            s = float(self.Security.PriceStep)
            if s > 0:
                step = s
        self._trailing_distance = step * self.TrailingPoints
        self._trigger_distance = step * self.TriggerPoints

        sma_fast = SimpleMovingAverage()
        sma_fast.Length = 10
        sma_slow = SimpleMovingAverage()
        sma_slow.Length = 30

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma_fast, sma_slow, self._process_candle).Start()

    def OnOwnTradeReceived(self, trade):
        super(trailing_stop_trigger_manager_strategy, self).OnOwnTradeReceived(trade)
        self._last_entry_price = float(trade.Trade.Price)

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        fv = float(fast)
        sv = float(slow)

        # Trailing stop management for long
        if self.Position > 0 and self._last_entry_price > 0:
            profit = price - self._last_entry_price
            if not self._trailing_enabled and profit >= self._trigger_distance:
                self._trailing_enabled = True
                self._active_stop_price = price - self._trailing_distance
            elif self._trailing_enabled:
                desired_stop = price - self._trailing_distance
                if self._active_stop_price is None or desired_stop > self._active_stop_price:
                    self._active_stop_price = desired_stop

            if self._trailing_enabled and self._active_stop_price is not None and price <= self._active_stop_price:
                self.SellMarket()
                self._reset_trailing_state()
                return

        # Trailing stop management for short
        elif self.Position < 0 and self._last_entry_price > 0:
            profit = self._last_entry_price - price
            if not self._trailing_enabled and profit >= self._trigger_distance:
                self._trailing_enabled = True
                self._active_stop_price = price + self._trailing_distance
            elif self._trailing_enabled:
                desired_stop = price + self._trailing_distance
                if self._active_stop_price is None or desired_stop < self._active_stop_price:
                    self._active_stop_price = desired_stop

            if self._trailing_enabled and self._active_stop_price is not None and price >= self._active_stop_price:
                self.BuyMarket()
                self._reset_trailing_state()
                return

        # SMA crossover entries
        if self._has_prev:
            cross_up = self._prev_fast <= self._prev_slow and fv > sv
            cross_down = self._prev_fast >= self._prev_slow and fv < sv

            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._reset_trailing_state()
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._reset_trailing_state()

        self._prev_fast = fv
        self._prev_slow = sv
        self._has_prev = True

    def _reset_trailing_state(self):
        self._trailing_enabled = False
        self._active_stop_price = None

    def CreateClone(self):
        return trailing_stop_trigger_manager_strategy()
