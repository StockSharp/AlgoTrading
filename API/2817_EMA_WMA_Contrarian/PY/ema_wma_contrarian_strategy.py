import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math, Decimal
from indicator_extensions import *

class ema_wma_contrarian_strategy(Strategy):
    def __init__(self):
        super(ema_wma_contrarian_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 28)
        self._wma_period = self.Param("WmaPeriod", 8)
        self._stop_loss_points = self.Param("StopLossPoints", 50.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 50.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 50.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 10.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._ema = None
        self._wma = None
        self._has_previous = False
        self._previous_ema = 0.0
        self._previous_wma = 0.0
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ema_wma_contrarian_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_period.Value
        self._wma = WeightedMovingAverage()
        self._wma.Length = self._wma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._ema is None or self._wma is None:
            return

        self._manage_active_position(candle)

        open_price = float(candle.OpenPrice)
        ema_result = process_float(self._ema, Decimal(float(open_price)), candle.OpenTime, True)
        wma_result = process_float(self._wma, Decimal(float(open_price)), candle.OpenTime, True)

        if ema_result.IsEmpty or wma_result.IsEmpty or not self._ema.IsFormed or not self._wma.IsFormed:
            return

        ema = float(ema_result.Value)
        wma = float(wma_result.Value)

        if not self._has_previous:
            self._previous_ema = ema
            self._previous_wma = wma
            self._has_previous = True
            return

        buy_signal = ema < wma and self._previous_ema > self._previous_wma
        sell_signal = ema > wma and self._previous_ema < self._previous_wma

        if buy_signal:
            self._enter_long(candle)
        elif sell_signal:
            self._enter_short(candle)

        self._previous_ema = ema
        self._previous_wma = wma

    def _manage_active_position(self, candle):
        price = float(candle.ClosePrice)

        if self.Position > 0:
            if self._take_profit_price is not None and price >= self._take_profit_price:
                self.SellMarket(self.Position)
                self._clear_position_state()
                return
            if self._stop_loss_price is not None and price <= self._stop_loss_price:
                self.SellMarket(self.Position)
                self._clear_position_state()
                return
            if self._entry_price is not None:
                self._update_trailing_for_long(price, self._entry_price)
        elif self.Position < 0:
            vol = abs(self.Position)
            if self._take_profit_price is not None and price <= self._take_profit_price:
                self.BuyMarket(vol)
                self._clear_position_state()
                return
            if self._stop_loss_price is not None and price >= self._stop_loss_price:
                self.BuyMarket(vol)
                self._clear_position_state()
                return
            if self._entry_price is not None:
                self._update_trailing_for_short(price, self._entry_price)
        else:
            self._clear_position_state()

    def _enter_long(self, candle):
        entry_price = float(candle.ClosePrice)

        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._clear_position_state()

        self.BuyMarket()
        self._setup_risk_levels(entry_price, True)

    def _enter_short(self, candle):
        entry_price = float(candle.ClosePrice)

        if self.Position > 0:
            self.SellMarket(self.Position)
            self._clear_position_state()

        self.SellMarket()
        self._setup_risk_levels(entry_price, False)

    def _setup_risk_levels(self, entry_price, is_long):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0

        self._entry_price = entry_price

        if self._stop_loss_points.Value > 0:
            sd = self._stop_loss_points.Value * step
            self._stop_loss_price = entry_price - sd if is_long else entry_price + sd
        else:
            self._stop_loss_price = None

        if self._take_profit_points.Value > 0:
            td = self._take_profit_points.Value * step
            self._take_profit_price = entry_price + td if is_long else entry_price - td
        else:
            self._take_profit_price = None

    def _update_trailing_for_long(self, current_price, entry_price):
        if self._trailing_stop_points.Value <= 0:
            return
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0
        trailing_distance = self._trailing_stop_points.Value * step
        trailing_step = self._trailing_step_points.Value * step

        if current_price - entry_price <= trailing_distance + trailing_step:
            return

        comparison_level = current_price - (trailing_distance + trailing_step)
        if self._stop_loss_price is None or self._stop_loss_price < comparison_level:
            self._stop_loss_price = current_price - trailing_distance

    def _update_trailing_for_short(self, current_price, entry_price):
        if self._trailing_stop_points.Value <= 0:
            return
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0
        trailing_distance = self._trailing_stop_points.Value * step
        trailing_step = self._trailing_step_points.Value * step

        if entry_price - current_price <= trailing_distance + trailing_step:
            return

        comparison_level = current_price + trailing_distance + trailing_step
        if self._stop_loss_price is None or self._stop_loss_price > comparison_level:
            self._stop_loss_price = current_price + trailing_distance

    def _clear_position_state(self):
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    def OnReseted(self):
        super(ema_wma_contrarian_strategy, self).OnReseted()
        self._ema = None
        self._wma = None
        self._has_previous = False
        self._previous_ema = 0.0
        self._previous_wma = 0.0
        self._clear_position_state()

    def CreateClone(self):
        return ema_wma_contrarian_strategy()
