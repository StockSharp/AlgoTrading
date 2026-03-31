import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class nevalyashka_martingale_strategy(Strategy):
    def __init__(self):
        super(nevalyashka_martingale_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 0.1)
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.1)
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0)
        self._move_profit_points = self.Param("MoveProfitPoints", 100.0)
        self._move_step_points = self.Param("MoveStepPoints", 50.0)
        self._stop_loss_points = self.Param("StopLossPoints", 400.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._planned_volume = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._equity_peak = 0.0
        self._next_direction_is_sell = True
        self._initial_order_placed = False

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def VolumeMultiplier(self):
        return self._volume_multiplier.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volume_multiplier.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def MoveProfitPoints(self):
        return self._move_profit_points.Value

    @MoveProfitPoints.setter
    def MoveProfitPoints(self, value):
        self._move_profit_points.Value = value

    @property
    def MoveStepPoints(self):
        return self._move_step_points.Value

    @MoveStepPoints.setter
    def MoveStepPoints(self, value):
        self._move_step_points.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _get_point_value(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        digits = 0
        value = step
        while value < 1.0 and digits < 10:
            value *= 10.0
            digits += 1

        if digits == 3 or digits == 5:
            step *= 10.0

        return step

    def OnStarted2(self, time):
        super(nevalyashka_martingale_strategy, self).OnStarted2(time)

        self._equity_peak = float(self.Portfolio.CurrentValue) if self.Portfolio is not None and self.Portfolio.CurrentValue is not None else 0.0
        self._planned_volume = float(self.BaseVolume)

        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._next_direction_is_sell = True
        self._initial_order_placed = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        point = self._get_point_value()

        self._handle_open_position(candle, point)

        if self.Position != 0:
            return

        equity = float(self.Portfolio.CurrentValue) if self.Portfolio is not None and self.Portfolio.CurrentValue is not None else 0.0

        if not self._initial_order_placed:
            if self._planned_volume <= 0.0:
                self._planned_volume = float(self.BaseVolume)
                if self._planned_volume <= 0.0:
                    return

            if self._open_position(True, float(candle.ClosePrice), point):
                self._initial_order_placed = True
                self._next_direction_is_sell = True
            return

        if equity > self._equity_peak:
            self._equity_peak = equity
            self._planned_volume = float(self.BaseVolume)

            if self._planned_volume <= 0.0:
                return

            if self._next_direction_is_sell:
                self._open_position(True, float(candle.ClosePrice), point)
            else:
                self._open_position(False, float(candle.ClosePrice), point)
        else:
            mult = float(self.VolumeMultiplier)
            if mult > 0.0:
                increased = self._planned_volume * mult
            else:
                increased = 0.0

            if increased <= 0.0:
                return

            self._planned_volume = increased

            if self._next_direction_is_sell:
                if self._open_position(False, float(candle.ClosePrice), point):
                    self._next_direction_is_sell = False
            else:
                if self._open_position(True, float(candle.ClosePrice), point):
                    self._next_direction_is_sell = True

    def _handle_open_position(self, candle, point):
        if self.Position > 0:
            self._handle_long_position(candle, point)
        elif self.Position < 0:
            self._handle_short_position(candle, point)

    def _handle_long_position(self, candle, point):
        if self._stop_price is None or self._take_price is None:
            return

        current_stop = self._stop_price
        current_take = self._take_price

        price = float(candle.ClosePrice)
        move_threshold = float(self.MoveProfitPoints) * point

        if price - self._entry_price > move_threshold:
            candidate = price - (float(self.StopLossPoints) + float(self.MoveStepPoints)) * point
            if candidate > current_stop:
                new_stop = price - float(self.StopLossPoints) * point
                self._stop_price = new_stop
                if self._planned_volume > float(self.BaseVolume) and new_stop > self._entry_price:
                    self._reduce_volume()

        if float(candle.LowPrice) <= self._stop_price:
            self.SellMarket()
            self._reset_protection()
            return

        if float(candle.HighPrice) >= current_take:
            self.SellMarket()
            self._reset_protection()

    def _handle_short_position(self, candle, point):
        if self._stop_price is None or self._take_price is None:
            return

        current_stop = self._stop_price
        current_take = self._take_price

        price = float(candle.ClosePrice)
        move_threshold = float(self.MoveProfitPoints) * point

        if self._entry_price - price > move_threshold:
            candidate = price + (float(self.StopLossPoints) + float(self.MoveStepPoints)) * point
            if candidate < current_stop:
                new_stop = price + float(self.StopLossPoints) * point
                self._stop_price = new_stop
                if self._planned_volume > float(self.BaseVolume) and new_stop < self._entry_price:
                    self._reduce_volume()

        if float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket()
            self._reset_protection()
            return

        if float(candle.LowPrice) <= current_take:
            self.BuyMarket()
            self._reset_protection()

    def _open_position(self, is_sell, price, point):
        if self._planned_volume <= 0.0:
            return False
        if point <= 0.0:
            return False

        stop_offset = float(self.StopLossPoints) * point
        take_offset = float(self.TakeProfitPoints) * point

        if stop_offset <= 0.0 or take_offset <= 0.0:
            return False

        if is_sell:
            self.SellMarket()
            self._stop_price = price + stop_offset
            self._take_price = price - take_offset
        else:
            self.BuyMarket()
            self._stop_price = price - stop_offset
            self._take_price = price + take_offset

        self._entry_price = price
        return True

    def _reduce_volume(self):
        mult = float(self.VolumeMultiplier)
        if mult <= 0.0:
            return

        base_vol = float(self.BaseVolume)
        if base_vol <= 0.0:
            return

        reduced = self._planned_volume / mult
        if reduced < base_vol:
            reduced = base_vol

        self._planned_volume = reduced

    def _reset_protection(self):
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0.0

    def OnReseted(self):
        super(nevalyashka_martingale_strategy, self).OnReseted()
        self._planned_volume = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._equity_peak = 0.0
        self._next_direction_is_sell = True
        self._initial_order_placed = False

    def CreateClone(self):
        return nevalyashka_martingale_strategy()
