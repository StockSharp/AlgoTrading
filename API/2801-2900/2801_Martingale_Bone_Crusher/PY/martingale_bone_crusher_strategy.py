import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType
from System import TimeSpan, Math


class martingale_bone_crusher_strategy(Strategy):
    def __init__(self):
        super(martingale_bone_crusher_strategy, self).__init__()

        self._initial_volume = self.Param("InitialVolume", 0.01)
        self._multiply = self.Param("Multiply", 2.0)
        self._double_lot_size = self.Param("DoubleLotSize", False)
        self._lot_size_increment = self.Param("LotSizeIncrement", 0.01)
        self._trailing_stop_steps = self.Param("TrailingStopSteps", 30.0)
        self._stop_loss_steps = self.Param("StopLossSteps", 5.0)
        self._take_profit_steps = self.Param("TakeProfitSteps", 5.0)
        self._fast_period = self.Param("FastPeriod", 2)
        self._slow_period = self.Param("SlowPeriod", 50)
        self._enable_trailing = self.Param("EnableTrailing", True)
        self._trailing_tp_money = self.Param("TrailingTakeProfitMoney", 40.0)
        self._trailing_stop_money = self.Param("TrailingStopMoney", 10.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._fast_ma = None
        self._slow_ma = None
        self._average_price = 0.0
        self._position_volume = 0.0
        self._current_volume = 0.01
        self._last_order_volume = 0.01
        self._last_trade_result = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._max_floating_profit = 0.0
        self._last_position_side = None
        self._last_losing_side = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    def OnStarted2(self, time):
        super(martingale_bone_crusher_strategy, self).OnStarted2(time)

        self._current_volume = self.InitialVolume
        self._last_order_volume = self.InitialVolume
        self.Volume = self.InitialVolume

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self._fast_period.Value
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._fast_ma, self._slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        fast_value = float(fast_val)
        slow_value = float(slow_val)

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        if self.Position != 0:
            self._update_extremes(candle)
            if self._try_stop_take(candle):
                return
            if self._try_money_targets(float(candle.ClosePrice)):
                return
            return

        entry_side = None
        if fast_value < slow_value:
            entry_side = "buy"
        elif fast_value > slow_value:
            entry_side = "sell"

        if self._last_trade_result < 0 and self._last_losing_side is not None:
            entry_side = "sell" if self._last_losing_side == "buy" else "buy"

        if entry_side is None:
            return

        volume = self._current_volume
        if volume <= 0:
            return

        if entry_side == "buy":
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._average_price = float(candle.ClosePrice)
        self._position_volume = volume
        self._last_order_volume = volume
        self._last_position_side = entry_side
        self._highest_price = float(candle.ClosePrice)
        self._lowest_price = float(candle.ClosePrice)
        self._max_floating_profit = 0.0

    def _update_extremes(self, candle):
        if self._last_position_side is None:
            return
        if self._last_position_side == "buy":
            if float(candle.HighPrice) > self._highest_price:
                self._highest_price = float(candle.HighPrice)
        else:
            if self._lowest_price == 0 or float(candle.LowPrice) < self._lowest_price:
                self._lowest_price = float(candle.LowPrice)

    def _steps_to_price(self, steps):
        sec = self.Security
        ps = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if ps <= 0:
            return 0.0
        return steps * ps

    def _try_stop_take(self, candle):
        if self._position_volume <= 0 or self._last_position_side is None:
            return False
        stop_dist = self._steps_to_price(self._stop_loss_steps.Value)
        take_dist = self._steps_to_price(self._take_profit_steps.Value)
        trail_dist = self._steps_to_price(self._trailing_stop_steps.Value)
        close_price = float(candle.ClosePrice)

        if self._last_position_side == "buy":
            if stop_dist > 0 and float(candle.LowPrice) <= self._average_price - stop_dist:
                self._close_position(self._average_price - stop_dist)
                return True
            if take_dist > 0 and float(candle.HighPrice) >= self._average_price + take_dist:
                self._close_position(self._average_price + take_dist)
                return True
            if self._trailing_stop_steps.Value > 0 and trail_dist > 0 and close_price <= self._highest_price - trail_dist:
                self._close_position(close_price)
                return True
        else:
            if stop_dist > 0 and float(candle.HighPrice) >= self._average_price + stop_dist:
                self._close_position(self._average_price + stop_dist)
                return True
            if take_dist > 0 and float(candle.LowPrice) <= self._average_price - take_dist:
                self._close_position(self._average_price - take_dist)
                return True
            if self._trailing_stop_steps.Value > 0 and trail_dist > 0 and close_price >= self._lowest_price + trail_dist:
                self._close_position(close_price)
                return True
        return False

    def _try_money_targets(self, close_price):
        profit = self._get_floating_profit(close_price)
        if self._enable_trailing.Value and profit > 0:
            if profit >= self._trailing_tp_money.Value:
                self._max_floating_profit = max(self._max_floating_profit, profit)
            if self._max_floating_profit > 0 and self._max_floating_profit - profit >= self._trailing_stop_money.Value:
                self._close_position(close_price)
                return True
        return False

    def _get_floating_profit(self, current_price):
        if self._position_volume <= 0 or self._last_position_side is None:
            return 0.0
        sec = self.Security
        ps = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if ps <= 0:
            return 0.0
        direction = 1.0 if self._last_position_side == "buy" else -1.0
        diff = (current_price - self._average_price) * direction
        steps = diff / ps
        return steps * ps * self._position_volume

    def _close_position(self, exit_price):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(abs(self.Position))
        self._compute_trade_result(exit_price)
        self._reset_position_state()
        self._update_next_volume()

    def _compute_trade_result(self, exit_price):
        if self._position_volume <= 0 or self._last_position_side is None:
            self._last_trade_result = 0.0
            self._last_losing_side = None
            return
        sec = self.Security
        ps = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if ps <= 0:
            self._last_trade_result = 0.0
            self._last_losing_side = None
            return
        direction = 1.0 if self._last_position_side == "buy" else -1.0
        diff = (exit_price - self._average_price) * direction
        steps = diff / ps
        pnl = steps * ps * self._position_volume
        self._last_trade_result = pnl
        self._last_losing_side = self._last_position_side if pnl < 0 else None

    def _reset_position_state(self):
        self._average_price = 0.0
        self._position_volume = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._max_floating_profit = 0.0
        self._last_position_side = None

    def _update_next_volume(self):
        if self._last_trade_result < 0:
            if self._double_lot_size.Value:
                nv = self._last_order_volume * self._multiply.Value
            else:
                nv = self._last_order_volume + self._lot_size_increment.Value
        else:
            nv = self.InitialVolume
        self._current_volume = nv
        self._last_order_volume = nv

    def OnReseted(self):
        super(martingale_bone_crusher_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._average_price = 0.0
        self._position_volume = 0.0
        self._current_volume = self.InitialVolume
        self._last_order_volume = self.InitialVolume
        self._last_trade_result = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._max_floating_profit = 0.0
        self._last_position_side = None
        self._last_losing_side = None

    def CreateClone(self):
        return martingale_bone_crusher_strategy()
