import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class alexav_speed_up_m1_strategy(Strategy):
    """
    Candle breakout strategy converted from the Alexav SpeedUp M1 expert advisor.
    Enters in the direction of strong candle bodies and manages exits with
    stop-loss, take-profit, and trailing stop logic.
    """

    def __init__(self):
        super(alexav_speed_up_m1_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Position size in lots", "General")

        self._stop_loss_pips = self.Param("StopLossPips", 30) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk Management")

        self._take_profit_pips = self.Param("TakeProfitPips", 90) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk Management")

        self._trailing_stop_pips = self.Param("TrailingStopPips", 10) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management")

        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step (pips)", "Price movement required to move the trailing stop", "Risk Management")

        self._minimum_body_size_pips = self.Param("MinimumBodySizePips", 100) \
            .SetDisplay("Minimum Body (pips)", "Minimum candle body size to trigger entries", "Signal")

        self._candle_type = self.Param("CandleType", tf(240)) \
            .SetDisplay("Candle Type", "Type of candles for analysis", "General")

        self._current_direction = None
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_distance = None
        self._trailing_step_distance = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @TrailingStopPips.setter
    def TrailingStopPips(self, value):
        self._trailing_stop_pips.Value = value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @TrailingStepPips.setter
    def TrailingStepPips(self, value):
        self._trailing_step_pips.Value = value

    @property
    def MinimumBodySizePips(self):
        return self._minimum_body_size_pips.Value

    @MinimumBodySizePips.setter
    def MinimumBodySizePips(self, value):
        self._minimum_body_size_pips.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(alexav_speed_up_m1_strategy, self).OnReseted()
        self._reset_position_state()

    def OnStarted2(self, time):
        super(alexav_speed_up_m1_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._current_direction is not None and self.Position == 0:
            self._reset_position_state()

        if self._current_direction is not None:
            if self._manage_active_position(candle):
                return

        pip_size = self._get_pip_size()
        minimum_body = 0.0 if self.MinimumBodySizePips <= 0 else self.MinimumBodySizePips * pip_size
        body_size = abs(float(candle.ClosePrice) - float(candle.OpenPrice))

        if body_size <= minimum_body:
            return

        if self._current_direction is not None:
            return

        direction = Sides.Buy if float(candle.ClosePrice) >= float(candle.OpenPrice) else Sides.Sell
        self._open_position(direction, float(candle.ClosePrice))

    def _manage_active_position(self, candle):
        if self._current_direction is None:
            return False

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._current_direction == Sides.Buy:
            if self._stop_price is not None and low <= self._stop_price:
                self._close_position()
                return True
            if self._take_profit_price is not None and high >= self._take_profit_price:
                self._close_position()
                return True
            self._update_trailing_stop_for_long(close)
        elif self._current_direction == Sides.Sell:
            if self._stop_price is not None and high >= self._stop_price:
                self._close_position()
                return True
            if self._take_profit_price is not None and low <= self._take_profit_price:
                self._close_position()
                return True
            self._update_trailing_stop_for_short(close)

        return False

    def _open_position(self, direction, price):
        if self.OrderVolume <= 0:
            return

        if direction == Sides.Buy:
            self.BuyMarket()
        else:
            self.SellMarket()

        self._current_direction = direction
        self._entry_price = price

        pip_size = self._get_pip_size()

        if self.StopLossPips > 0:
            if direction == Sides.Buy:
                self._stop_price = price - self.StopLossPips * pip_size
            else:
                self._stop_price = price + self.StopLossPips * pip_size
        else:
            self._stop_price = None

        if self.TakeProfitPips > 0:
            if direction == Sides.Buy:
                self._take_profit_price = price + self.TakeProfitPips * pip_size
            else:
                self._take_profit_price = price - self.TakeProfitPips * pip_size
        else:
            self._take_profit_price = None

        if self.TrailingStopPips > 0:
            self._trailing_stop_distance = self.TrailingStopPips * pip_size
            self._trailing_step_distance = self.TrailingStepPips * pip_size
        else:
            self._trailing_stop_distance = None
            self._trailing_step_distance = None

    def _close_position(self):
        current_position = self.Position
        if current_position > 0:
            self.SellMarket(current_position)
        elif current_position < 0:
            self.BuyMarket(-current_position)
        self._reset_position_state()

    def _update_trailing_stop_for_long(self, price):
        if self._trailing_stop_distance is None or self._trailing_step_distance is None:
            return
        if price - self._entry_price < self._trailing_stop_distance + self._trailing_step_distance:
            return
        candidate = price - self._trailing_stop_distance
        if self._stop_price is not None and self._stop_price >= candidate - self._trailing_step_distance:
            return
        self._stop_price = candidate

    def _update_trailing_stop_for_short(self, price):
        if self._trailing_stop_distance is None or self._trailing_step_distance is None:
            return
        if self._entry_price - price < self._trailing_stop_distance + self._trailing_step_distance:
            return
        candidate = price + self._trailing_stop_distance
        if self._stop_price is not None and self._stop_price <= candidate + self._trailing_step_distance:
            return
        self._stop_price = candidate

    def _reset_position_state(self):
        self._current_direction = None
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_distance = None
        self._trailing_step_distance = None

    def _get_pip_size(self):
        return 0.0001

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alexav_speed_up_m1_strategy()
