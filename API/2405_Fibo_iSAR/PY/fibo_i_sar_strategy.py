import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates
from StockSharp.Algo.Indicators import ParabolicSar, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class fibo_i_sar_strategy(Strategy):
    def __init__(self):
        super(fibo_i_sar_strategy, self).__init__()

        self._step_fast = self.Param("StepFast", 0.02)
        self._max_fast = self.Param("MaximumFast", 0.2)
        self._step_slow = self.Param("StepSlow", 0.01)
        self._max_slow = self.Param("MaximumSlow", 0.1)
        self._count_bar_search = self.Param("CountBarSearch", 3)
        self._indent_stop_loss = self.Param("IndentStopLoss", 30)
        self._fibo_entrance_level = self.Param("FiboEntranceLevel", 50.0)
        self._fibo_profit_level = self.Param("FiboProfitLevel", 161.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._use_time_filter = self.Param("UseTimeFilter", False)
        self._start_hour = self.Param("StartHour", 7)
        self._stop_hour = self.Param("StopHour", 17)

        self._pending_order = None
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def StepFast(self):
        return self._step_fast.Value

    @StepFast.setter
    def StepFast(self, value):
        self._step_fast.Value = value

    @property
    def MaximumFast(self):
        return self._max_fast.Value

    @MaximumFast.setter
    def MaximumFast(self, value):
        self._max_fast.Value = value

    @property
    def StepSlow(self):
        return self._step_slow.Value

    @StepSlow.setter
    def StepSlow(self, value):
        self._step_slow.Value = value

    @property
    def MaximumSlow(self):
        return self._max_slow.Value

    @MaximumSlow.setter
    def MaximumSlow(self, value):
        self._max_slow.Value = value

    @property
    def CountBarSearch(self):
        return self._count_bar_search.Value

    @CountBarSearch.setter
    def CountBarSearch(self, value):
        self._count_bar_search.Value = value

    @property
    def IndentStopLoss(self):
        return self._indent_stop_loss.Value

    @IndentStopLoss.setter
    def IndentStopLoss(self, value):
        self._indent_stop_loss.Value = value

    @property
    def FiboEntranceLevel(self):
        return self._fibo_entrance_level.Value

    @FiboEntranceLevel.setter
    def FiboEntranceLevel(self, value):
        self._fibo_entrance_level.Value = value

    @property
    def FiboProfitLevel(self):
        return self._fibo_profit_level.Value

    @FiboProfitLevel.setter
    def FiboProfitLevel(self, value):
        self._fibo_profit_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def UseTimeFilter(self):
        return self._use_time_filter.Value

    @UseTimeFilter.setter
    def UseTimeFilter(self, value):
        self._use_time_filter.Value = value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @StartHour.setter
    def StartHour(self, value):
        self._start_hour.Value = value

    @property
    def StopHour(self):
        return self._stop_hour.Value

    @StopHour.setter
    def StopHour(self, value):
        self._stop_hour.Value = value

    def OnStarted2(self, time):
        super(fibo_i_sar_strategy, self).OnStarted2(time)

        self._fast_sar = ParabolicSar()
        self._fast_sar.Acceleration = self.StepFast
        self._fast_sar.AccelerationMax = self.MaximumFast

        self._slow_sar = ParabolicSar()
        self._slow_sar.Acceleration = self.StepSlow
        self._slow_sar.AccelerationMax = self.MaximumSlow

        self._highest = Highest()
        self._highest.Length = self.CountBarSearch

        self._lowest = Lowest()
        self._lowest.Length = self.CountBarSearch

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_sar, self._slow_sar, self._highest, self._lowest, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_sar, slow_sar, highest, lowest):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.CloseTime.Hour
        if self.UseTimeFilter and (hour < self.StartHour or hour > self.StopHour):
            self._cancel_pending_order()
            return

        price = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        fast_val = float(fast_sar)
        slow_val = float(slow_sar)
        high_val = float(highest)
        low_val = float(lowest)

        if self.Position > 0:
            if price <= self._stop_price or price >= self._take_profit_price:
                self.SellMarket()
        elif self.Position < 0:
            if price >= self._stop_price or price <= self._take_profit_price:
                self.BuyMarket()

        if self._pending_order is not None:
            side = self._pending_order.Side
            if side == Sides.Buy and (slow_val > fast_val or fast_val >= price):
                self._cancel_pending_order()
            elif side == Sides.Sell and (slow_val < fast_val or fast_val <= price):
                self._cancel_pending_order()

        if self._pending_order is not None or self.Position != 0:
            return

        range_high = high_val
        range_low = low_val
        rng = range_high - range_low

        if slow_val < fast_val and fast_val < price:
            entry = range_low + rng * (float(self.FiboEntranceLevel) / 100.0)
            profit = range_low + rng * (float(self.FiboProfitLevel) / 100.0)
            stop = range_low - float(self.IndentStopLoss) * step
            self._stop_price = stop
            self._take_profit_price = profit
            self._pending_order = self.BuyLimit(entry)
        elif slow_val > fast_val and fast_val > price:
            entry = range_high - rng * (float(self.FiboEntranceLevel) / 100.0)
            profit = range_high - rng * (float(self.FiboProfitLevel) / 100.0)
            stop = range_high + float(self.IndentStopLoss) * step
            self._stop_price = stop
            self._take_profit_price = profit
            self._pending_order = self.SellLimit(entry)

    def OnOrderReceived(self, order):
        super(fibo_i_sar_strategy, self).OnOrderReceived(order)
        if self._pending_order is not None and order == self._pending_order:
            if order.State != OrderStates.Active:
                self._pending_order = None

    def _cancel_pending_order(self):
        if self._pending_order is not None:
            if self._pending_order.State == OrderStates.Active:
                self.CancelOrder(self._pending_order)
            self._pending_order = None

    def OnReseted(self):
        super(fibo_i_sar_strategy, self).OnReseted()
        self._pending_order = None
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def CreateClone(self):
        return fibo_i_sar_strategy()
