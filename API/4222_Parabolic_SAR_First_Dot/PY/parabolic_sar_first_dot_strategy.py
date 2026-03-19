import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class parabolic_sar_first_dot_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_first_dot_strategy, self).__init__()
        self._trade_volume = self.Param("TradeVolume", 0.1).SetGreaterThanZero().SetDisplay("Volume", "Order volume in lots", "General")
        self._sl_points = self.Param("StopLossPoints", 90).SetGreaterThanZero().SetDisplay("Stop-Loss Points", "Stop-loss distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 20).SetGreaterThanZero().SetDisplay("Take-Profit Points", "Take-profit distance", "Risk")
        self._use_multiplier = self.Param("UseStopMultiplier", True).SetDisplay("Use Stop Multiplier", "Multiply distances by 10", "Risk")
        self._sar_step = self.Param("SarAccelerationStep", 0.02).SetDisplay("SAR Step", "Initial acceleration factor", "Indicator")
        self._sar_max = self.Param("SarAccelerationMax", 0.2).SetDisplay("SAR Max", "Maximum acceleration factor", "Indicator")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(parabolic_sar_first_dot_strategy, self).OnReseted()
        self._prev_is_sar_above = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def OnStarted(self, time):
        super(parabolic_sar_first_dot_strategy, self).OnStarted(time)
        self._prev_is_sar_above = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._price_step = self._get_price_step()
        self.Volume = self._trade_volume.Value

        sar = ParabolicSar()
        sar.Acceleration = self._sar_step.Value
        sar.AccelerationMax = self._sar_max.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sar, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _get_price_step(self):
        step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        return step

    def _get_distance(self, base_points):
        multiplier = 10 if self._use_multiplier.Value else 1
        return base_points * multiplier * self._price_step

    def OnProcess(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        self._check_protective_levels(candle)

        is_sar_above = sar_value > candle.ClosePrice

        if self._prev_is_sar_above is None:
            self._prev_is_sar_above = is_sar_above
            return

        sar_switched_below = self._prev_is_sar_above and not is_sar_above
        sar_switched_above = not self._prev_is_sar_above and is_sar_above

        if sar_switched_below:
            self._try_enter_long(candle)
        elif sar_switched_above:
            self._try_enter_short(candle)

        self._prev_is_sar_above = is_sar_above

    def _try_enter_long(self, candle):
        if self.Position > 0:
            return
        volume = self.Volume + Math.Abs(self.Position)
        if volume <= 0:
            return
        self.BuyMarket(volume)
        entry = candle.ClosePrice
        stop_dist = self._get_distance(self._sl_points.Value)
        take_dist = self._get_distance(self._tp_points.Value)
        self._long_stop = entry - stop_dist
        self._long_take = entry + take_dist
        self._short_stop = None
        self._short_take = None

    def _try_enter_short(self, candle):
        if self.Position < 0:
            return
        volume = self.Volume + Math.Abs(self.Position)
        if volume <= 0:
            return
        self.SellMarket(volume)
        entry = candle.ClosePrice
        stop_dist = self._get_distance(self._sl_points.Value)
        take_dist = self._get_distance(self._tp_points.Value)
        self._short_stop = entry + stop_dist
        self._short_take = entry - take_dist
        self._long_stop = None
        self._long_take = None

    def _check_protective_levels(self, candle):
        if self.Position > 0:
            if self._long_stop is not None and candle.LowPrice <= self._long_stop:
                self.SellMarket(Math.Abs(self.Position))
                self._long_stop = None
                self._long_take = None
            elif self._long_take is not None and candle.HighPrice >= self._long_take:
                self.SellMarket(Math.Abs(self.Position))
                self._long_stop = None
                self._long_take = None
        elif self.Position < 0:
            if self._short_stop is not None and candle.HighPrice >= self._short_stop:
                self.BuyMarket(Math.Abs(self.Position))
                self._short_stop = None
                self._short_take = None
            elif self._short_take is not None and candle.LowPrice <= self._short_take:
                self.BuyMarket(Math.Abs(self.Position))
                self._short_stop = None
                self._short_take = None

    def CreateClone(self):
        return parabolic_sar_first_dot_strategy()
