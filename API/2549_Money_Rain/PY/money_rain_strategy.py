import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy

EXIT_NONE = 0
EXIT_STOP_LOSS = 1
EXIT_TAKE_PROFIT = 2

SIDE_BUY = 1
SIDE_SELL = -1


class money_rain_strategy(Strategy):
    def __init__(self):
        super(money_rain_strategy, self).__init__()

        self._demarker_period = self.Param("DeMarkerPeriod", 31)
        self._take_profit_points = self.Param("TakeProfitPoints", 5.0)
        self._stop_loss_points = self.Param("StopLossPoints", 20.0)
        self._base_volume = self.Param("BaseVolume", 0.01)
        self._loss_limit = self.Param("LossLimit", 1000000)
        self._fast_optimize = self.Param("FastOptimize", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._adjusted_point = 0.0
        self._take_profit_offset = 0.0
        self._stop_loss_offset = 0.0
        self._last_spread_points = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._active_volume = 0.0
        self._consecutive_losses = 0
        self._consecutive_profits = 0
        self._losses_volume = 0.0
        self._exit_order_active = False
        self._pending_exit_reason = EXIT_NONE
        self._current_side = None

    @property
    def DeMarkerPeriod(self):
        return self._demarker_period.Value

    @DeMarkerPeriod.setter
    def DeMarkerPeriod(self, value):
        self._demarker_period.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def LossLimit(self):
        return self._loss_limit.Value

    @LossLimit.setter
    def LossLimit(self, value):
        self._loss_limit.Value = value

    @property
    def FastOptimize(self):
        return self._fast_optimize.Value

    @FastOptimize.setter
    def FastOptimize(self, value):
        self._fast_optimize.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(money_rain_strategy, self).OnStarted2(time)

        self._update_offsets()

        self._demarker = DeMarker()
        self._demarker.Length = self.DeMarkerPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._demarker, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, demarker_value):
        if candle.State != CandleStates.Finished:
            return

        self._manage_open_position(candle)

        if not self._demarker.IsFormed:
            return

        if self.Position != 0 or self._exit_order_active:
            return

        if self.LossLimit > 0 and self._consecutive_losses >= self.LossLimit:
            return

        if self._adjusted_point <= 0.0:
            self._update_offsets()

        volume = self._get_trade_volume()
        if volume <= 0.0:
            return

        dm = float(demarker_value)
        close = float(candle.ClosePrice)

        if dm > 0.5:
            self._enter_position(SIDE_BUY, volume, close)
        else:
            self._enter_position(SIDE_SELL, volume, close)

    def _manage_open_position(self, candle):
        if self._current_side is None or self.Position == 0 or self._exit_order_active:
            return

        has_stop = self._stop_loss_offset > 0.0
        has_take = self._take_profit_offset > 0.0

        hit_stop = False
        hit_take = False

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._current_side == SIDE_BUY:
            hit_stop = has_stop and low <= self._stop_price
            hit_take = has_take and high >= self._take_price
        elif self._current_side == SIDE_SELL:
            hit_stop = has_stop and high >= self._stop_price
            hit_take = has_take and low <= self._take_price

        if not hit_stop and not hit_take:
            return

        self._exit_order_active = True
        self._pending_exit_reason = EXIT_STOP_LOSS if hit_stop else EXIT_TAKE_PROFIT

        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        is_profit = (self._pending_exit_reason == EXIT_TAKE_PROFIT)
        self._update_trade_stats(is_profit)

        self._exit_order_active = False
        self._pending_exit_reason = EXIT_NONE
        self._current_side = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._active_volume = 0.0

    def _enter_position(self, side, volume, reference_price):
        self._current_side = side
        self._exit_order_active = False
        self._pending_exit_reason = EXIT_NONE
        self._entry_price = reference_price
        self._active_volume = volume

        if side == SIDE_BUY:
            self._stop_price = reference_price - self._stop_loss_offset
            self._take_price = reference_price + self._take_profit_offset
            self.BuyMarket()
        else:
            self._stop_price = reference_price + self._stop_loss_offset
            self._take_price = reference_price - self._take_profit_offset
            self.SellMarket()

    def _get_trade_volume(self):
        volume = float(self.BaseVolume)
        if volume <= 0.0:
            return 0.0

        if self.FastOptimize:
            return volume

        if self._losses_volume <= 0.5 or self._consecutive_profits > 0:
            return volume

        spread = max(0.0, self._last_spread_points)
        denominator = float(self.TakeProfitPoints) - spread
        if denominator <= 0.0:
            return volume

        multiplier = self._losses_volume * (float(self.StopLossPoints) + spread) / denominator
        if multiplier <= 0.0:
            return volume

        return volume * multiplier

    def _update_trade_stats(self, is_profit):
        if is_profit:
            self._consecutive_losses = 0
            if self._consecutive_profits > 1:
                self._losses_volume = 0.0
            self._consecutive_profits += 1
        else:
            self._consecutive_losses += 1
            self._consecutive_profits = 0
            bv = float(self.BaseVolume)
            if bv > 0.0:
                self._losses_volume += self._active_volume / bv

    def _update_offsets(self):
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        if price_step <= 0.0:
            price_step = 0.0001

        decimals = self.Security.Decimals if self.Security is not None and self.Security.Decimals is not None else 0
        digits_adjust = 10.0 if (decimals == 3 or decimals == 5) else 1.0

        self._adjusted_point = price_step * digits_adjust
        self._take_profit_offset = float(self.TakeProfitPoints) * self._adjusted_point
        self._stop_loss_offset = float(self.StopLossPoints) * self._adjusted_point

    def OnReseted(self):
        super(money_rain_strategy, self).OnReseted()
        self._adjusted_point = 0.0
        self._take_profit_offset = 0.0
        self._stop_loss_offset = 0.0
        self._last_spread_points = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._active_volume = 0.0
        self._consecutive_losses = 0
        self._consecutive_profits = 0
        self._losses_volume = 0.0
        self._exit_order_active = False
        self._pending_exit_reason = EXIT_NONE
        self._current_side = None

    def CreateClone(self):
        return money_rain_strategy()
