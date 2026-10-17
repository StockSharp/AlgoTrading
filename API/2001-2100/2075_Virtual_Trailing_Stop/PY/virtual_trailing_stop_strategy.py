import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Strategies import Strategy


class virtual_trailing_stop_strategy(Strategy):
    """External-position-only virtual close-based stop, target and trailing manager."""

    def __init__(self):
        super(virtual_trailing_stop_strategy, self).__init__()
        self._stop_loss = self.Param("StopLoss", 0.0).SetNotNegative().SetDisplay(
            "Stop Loss", "Fixed distance from filled entry in price steps", "Risk")
        self._take_profit = self.Param("TakeProfit", 0.0).SetNotNegative().SetDisplay(
            "Take Profit", "Fixed target distance in price steps", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 30.0).SetNotNegative().SetDisplay(
            "Trailing Stop", "Trailing distance in price steps", "Risk")
        self._trailing_start = self.Param("TrailingStart", 10.0).SetNotNegative().SetDisplay(
            "Trailing Start", "Required favorable movement in price steps", "Risk")
        self._trailing_step = self.Param("TrailingStep", 1.0).SetGreaterThanZero().SetDisplay(
            "Trailing Step", "Minimum improvement before tightening", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))).SetDisplay(
            "Candle Type", "Processed finished candles", "General")
        self._reset_tracking()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_tracking(self):
        self._entry_price = self._best_price = self._virtual_stop = None
        self._tracked_position = 0.0
        self._pending = None

    def OnReseted(self):
        super(virtual_trailing_stop_strategy, self).OnReseted()
        self._reset_tracking()

    def OnStarted2(self, time):
        super(virtual_trailing_stop_strategy, self).OnStarted2(time)
        self._reset_tracking()
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def OnNewPosition(self, position):
        super(virtual_trailing_stop_strategy, self).OnNewPosition(position)
        self._observe_position(position)

    def OnPositionChanged(self, position):
        super(virtual_trailing_stop_strategy, self).OnPositionChanged(position)
        self._observe_position(position)

    def OnPositionReceived(self, position):
        super(virtual_trailing_stop_strategy, self).OnPositionReceived(position)
        self._observe_position(position)

    def _observe_position(self, position):
        if position.Security != self.Security or position.Portfolio != self.Portfolio:
            return
        volume = float(position.CurrentValue or 0)
        if volume == 0:
            self._entry_price = self._best_price = self._virtual_stop = None
            self._tracked_position = 0.0
            return
        if position.AveragePrice is None or position.AveragePrice <= 0:
            return
        price = float(position.AveragePrice)
        if self._entry_price == price and self._tracked_position * volume > 0:
            self._tracked_position = volume
            return
        self._entry_price = self._best_price = price
        self._virtual_stop = None
        self._tracked_position = volume

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished or self.Position == 0 or self._entry_price is None:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return

        is_long = self.Position > 0
        price = float(candle.ClosePrice)
        entry = self._entry_price
        step = float(self.Security.PriceStep or 1)
        if step <= 0:
            step = 1.0
        best = self._best_price if self._best_price is not None else entry
        best = max(best, price) if is_long else min(best, price)
        self._best_price = best

        distance = float(self._trailing_stop.Value) * step
        if distance > 0:
            profit = best - entry if is_long else entry - best
            if profit >= float(self._trailing_start.Value) * step:
                candidate = best - distance if is_long else best + distance
                move = float(self._trailing_step.Value) * step
                current = self._virtual_stop
                if current is None or (candidate >= current + move if is_long else candidate <= current - move):
                    self._virtual_stop = (max(current, candidate) if is_long else min(current, candidate)) if current is not None else candidate

        fixed_dist = float(self._stop_loss.Value) * step
        take_dist = float(self._take_profit.Value) * step
        fixed_stop = fixed_dist > 0 and (price <= entry - fixed_dist if is_long else price >= entry + fixed_dist)
        target = take_dist > 0 and (price >= entry + take_dist if is_long else price <= entry - take_dist)
        trail_hit = self._virtual_stop is not None and (price <= self._virtual_stop if is_long else price >= self._virtual_stop)
        if fixed_stop or target or trail_hit:
            self._pending = self.SellMarket(abs(self.Position)) if is_long else self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        return virtual_trailing_stop_strategy()
