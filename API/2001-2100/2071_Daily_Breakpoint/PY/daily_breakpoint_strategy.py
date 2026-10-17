import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class daily_breakpoint_strategy(Strategy):
    """Daily-open breakout with previous candle body and price-step protections."""

    def __init__(self):
        super(daily_breakpoint_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay(
            "Candle Type", "Signal timeframe", "General")
        self._break_point = self.Param("BreakPoint", 30.0).SetNotNegative().SetDisplay(
            "Break Point", "Daily-open breakout offset in price steps", "Trading")
        self._last_bar_min = self.Param("LastBarMin", 5.0).SetNotNegative().SetDisplay(
            "Last Bar Min", "Minimum previous candle body in steps", "Trading")
        self._last_bar_max = self.Param("LastBarMax", 100.0).SetNotNegative().SetDisplay(
            "Last Bar Max", "Maximum previous candle body in steps", "Trading")
        self._take_profit = self.Param("TakeProfit", 200.0).SetNotNegative().SetDisplay(
            "Take Profit", "Optional target in price steps", "Risk")
        self._stop_loss = self.Param("StopLoss", 100.0).SetNotNegative().SetDisplay(
            "Stop Loss", "Optional risk in price steps", "Risk")
        self._trailing_start = self.Param("TrailingStart", 0.0).SetNotNegative().SetDisplay(
            "Trailing Start", "Activation gain in price steps", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 0.0).SetNotNegative().SetDisplay(
            "Trailing Stop", "Trail distance; zero disables trailing", "Risk")
        self._trailing_step = self.Param("TrailingStep", 1.0).SetGreaterThanZero().SetDisplay(
            "Trailing Step", "Minimum improvement to adjust trailing", "Risk")
        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._daily_open = self._previous_open = self._previous_close = 0.0
        self._previous_time = None
        self._has_previous = False
        self._entry_price = self._best_price = self._trail_price = None
        self._tracked_position = 0.0
        self._pending = None

    def OnReseted(self):
        super(daily_breakpoint_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(daily_breakpoint_strategy, self).OnStarted2(time)
        self._reset_state()
        step = float(self.Security.PriceStep or 1)
        if step <= 0:
            step = 1
        self.StartProtection(
            takeProfit=Unit(float(self._take_profit.Value) * step, UnitTypes.Absolute),
            stopLoss=Unit(float(self._stop_loss.Value) * step, UnitTypes.Absolute),
            useMarketOrders=True)
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def OnNewPosition(self, position):
        super(daily_breakpoint_strategy, self).OnNewPosition(position)
        self._observe_position(position)

    def OnPositionChanged(self, position):
        super(daily_breakpoint_strategy, self).OnPositionChanged(position)
        self._observe_position(position)

    def OnPositionReceived(self, position):
        super(daily_breakpoint_strategy, self).OnPositionReceived(position)
        self._observe_position(position)

    def _observe_position(self, position):
        if position.Security != self.Security or position.Portfolio != self.Portfolio:
            return
        volume = float(position.CurrentValue or 0)
        if volume == 0:
            self._entry_price = self._best_price = self._trail_price = None
            self._tracked_position = 0.0
            return
        if position.AveragePrice is None or position.AveragePrice <= 0:
            return
        avg = float(position.AveragePrice)
        if self._entry_price == avg and self._tracked_position * volume > 0:
            self._tracked_position = volume
            return
        self._entry_price = self._best_price = avg
        self._trail_price = None
        self._tracked_position = volume

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        new_day = not self._has_previous or candle.OpenTime.Date != self._previous_time.Date
        if new_day:
            self._daily_open = float(candle.OpenPrice)
        price = float(candle.ClosePrice)
        step = float(self.Security.PriceStep or 1)
        if step <= 0:
            step = 1

        can_order = self.IsFormedAndOnlineAndAllowTrading() and (
            self._pending is None or self._pending.State in (OrderStates.Done, OrderStates.Failed))
        if can_order:
            pos = float(self.Position)
            if pos != 0 and self._entry_price is not None and float(self._trailing_stop.Value) > 0:
                entry = self._entry_price
                is_long = pos > 0
                best = self._best_price if self._best_price is not None else entry
                best = max(best, price) if is_long else min(best, price)
                self._best_price = best
                if (best - entry if is_long else entry - best) >= float(self._trailing_start.Value) * step:
                    candidate = best - float(self._trailing_stop.Value) * step if is_long else best + float(self._trailing_stop.Value) * step
                    current = self._trail_price
                    movement = float(self._trailing_step.Value) * step
                    if current is None or (candidate >= current + movement if is_long else candidate <= current - movement):
                        self._trail_price = candidate
                if self._trail_price is not None and (price <= self._trail_price if is_long else price >= self._trail_price):
                    self._pending = self.SellMarket(abs(self.Position)) if is_long else self.BuyMarket(abs(self.Position))
            elif pos == 0 and not new_day and self._has_previous and self._previous_time.Date == candle.OpenTime.Date:
                body = abs(self._previous_close - self._previous_open)
                offset = float(self._break_point.Value) * step
                buy_level = self._daily_open + offset
                sell_level = self._daily_open - offset
                valid_size = float(self._last_bar_min.Value) * step <= body <= float(self._last_bar_max.Value) * step
                if valid_size and self._previous_close > self._previous_open and price > buy_level and self._previous_open <= buy_level <= self._previous_close:
                    self._pending = self.BuyMarket(self.Volume)
                elif valid_size and self._previous_close < self._previous_open and price < sell_level and self._previous_close <= sell_level <= self._previous_open:
                    self._pending = self.SellMarket(self.Volume)

        self._previous_open = float(candle.OpenPrice)
        self._previous_close = price
        self._previous_time = candle.OpenTime
        self._has_previous = True

    def CreateClone(self):
        return daily_breakpoint_strategy()
