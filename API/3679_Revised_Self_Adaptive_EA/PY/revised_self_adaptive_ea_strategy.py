import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides, Level1Fields
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class revised_self_adaptive_ea_strategy(Strategy):
    def __init__(self):
        super(revised_self_adaptive_ea_strategy, self).__init__()

        self._average_body_period = self.Param("AverageBodyPeriod", 3)
        self._moving_average_period = self.Param("MovingAveragePeriod", 2)
        self._rsi_period = self.Param("RsiPeriod", 6)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._volume_param = self.Param("TradeVolume", 1.0)
        self._max_spread_points = self.Param("MaxSpreadPoints", 20.0)
        self._max_risk_percent = self.Param("MaxRiskPercent", 10.0)
        self._use_trailing_stop = self.Param("UseTrailingStop", True)
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0)
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 4.0)
        self._trailing_stop_atr_multiplier = self.Param("TrailingStopAtrMultiplier", 1.5)
        self._oversold_level = self.Param("OversoldLevel", 40.0)
        self._overbought_level = self.Param("OverboughtLevel", 60.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))

        self._rsi = None
        self._ma = None
        self._atr = None
        self._body_average = None

        self._previous_candle = None
        self._last_atr_value = 0.0
        self._average_body_value = 0.0

        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_profit_price = None
        self._long_trailing_stop_price = None

        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_profit_price = None
        self._short_trailing_stop_price = None

    @property
    def AverageBodyPeriod(self):
        return self._average_body_period.Value

    @AverageBodyPeriod.setter
    def AverageBodyPeriod(self, value):
        self._average_body_period.Value = value

    @property
    def MovingAveragePeriod(self):
        return self._moving_average_period.Value

    @MovingAveragePeriod.setter
    def MovingAveragePeriod(self, value):
        self._moving_average_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def TradeVolume(self):
        return self._volume_param.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._volume_param.Value = value

    @property
    def MaxSpreadPoints(self):
        return self._max_spread_points.Value

    @MaxSpreadPoints.setter
    def MaxSpreadPoints(self, value):
        self._max_spread_points.Value = value

    @property
    def MaxRiskPercent(self):
        return self._max_risk_percent.Value

    @MaxRiskPercent.setter
    def MaxRiskPercent(self, value):
        self._max_risk_percent.Value = value

    @property
    def UseTrailingStop(self):
        return self._use_trailing_stop.Value

    @UseTrailingStop.setter
    def UseTrailingStop(self, value):
        self._use_trailing_stop.Value = value

    @property
    def StopLossAtrMultiplier(self):
        return self._stop_loss_atr_multiplier.Value

    @StopLossAtrMultiplier.setter
    def StopLossAtrMultiplier(self, value):
        self._stop_loss_atr_multiplier.Value = value

    @property
    def TakeProfitAtrMultiplier(self):
        return self._take_profit_atr_multiplier.Value

    @TakeProfitAtrMultiplier.setter
    def TakeProfitAtrMultiplier(self, value):
        self._take_profit_atr_multiplier.Value = value

    @property
    def TrailingStopAtrMultiplier(self):
        return self._trailing_stop_atr_multiplier.Value

    @TrailingStopAtrMultiplier.setter
    def TrailingStopAtrMultiplier(self, value):
        self._trailing_stop_atr_multiplier.Value = value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversold_level.Value = value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overbought_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(revised_self_adaptive_ea_strategy, self).OnReseted()
        self._previous_candle = None
        self._last_atr_value = 0.0
        self._average_body_value = 0.0
        self._reset_long_risk_levels()
        self._reset_short_risk_levels()

    def _reset_long_risk_levels(self):
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_profit_price = None
        self._long_trailing_stop_price = None

    def _reset_short_risk_levels(self):
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_profit_price = None
        self._short_trailing_stop_price = None

    def OnStarted2(self, time):
        super(revised_self_adaptive_ea_strategy, self).OnStarted2(time)

        self.Volume = float(self.TradeVolume)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MovingAveragePeriod

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        self._body_average = SimpleMovingAverage()
        self._body_average.Length = self.AverageBodyPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._ma, self._atr, self._process_candle).Start()

    def _update_average_body(self, candle):
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        value = process_float(self._body_average, body, candle.OpenTime, True)
        if value.IsFinal:
            self._average_body_value = float(value.GetValue[float]())

    def _process_candle(self, candle, rsi_value, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        ma_val = float(ma_value)
        atr_val = float(atr_value)

        self._last_atr_value = atr_val
        self._update_average_body(candle)
        self._manage_open_positions(candle)

        if not self._rsi.IsFormed or not self._ma.IsFormed or not self._atr.IsFormed:
            self._previous_candle = candle
            return

        if self._previous_candle is None:
            self._previous_candle = candle
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        prev_close = float(self._previous_candle.ClosePrice)
        prev_open = float(self._previous_candle.OpenPrice)

        body_size = abs(close - open_p)
        minimum_body = self._average_body_value if self._average_body_value > 0 else 0.0

        bullish_engulfing = (close > open_p and
                             prev_close < prev_open and
                             open_p <= prev_close and
                             body_size >= minimum_body)

        bearish_engulfing = (close < open_p and
                             prev_close > prev_open and
                             open_p >= prev_close and
                             body_size >= minimum_body)

        if bullish_engulfing and rsi_val <= float(self.OversoldLevel) and close >= ma_val:
            self._try_open_long(close)
        elif bearish_engulfing and rsi_val >= float(self.OverboughtLevel) and close <= ma_val:
            self._try_open_short(close)

        self._previous_candle = candle

    def _try_open_long(self, price):
        if self.Position > 0:
            return

        if self.Position < 0:
            self.BuyMarket(abs(float(self.Position)))
            return

        volume = float(self.TradeVolume)
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._initialize_long_risk_levels(price)

    def _try_open_short(self, price):
        if self.Position < 0:
            return

        if self.Position > 0:
            self.SellMarket(abs(float(self.Position)))
            return

        volume = float(self.TradeVolume)
        if volume <= 0:
            return

        self.SellMarket(volume)
        self._initialize_short_risk_levels(price)

    def _manage_open_positions(self, candle):
        if self.Position > 0:
            exit_volume = float(self.Position)

            if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
                self.SellMarket(exit_volume)
                return

            if self._long_take_profit_price is not None and float(candle.HighPrice) >= self._long_take_profit_price:
                self.SellMarket(exit_volume)
                return

            if self.UseTrailingStop and self._long_trailing_stop_price is not None and self._last_atr_value > 0:
                candidate = float(candle.ClosePrice) - self._last_atr_value * float(self.TrailingStopAtrMultiplier)
                if candidate > self._long_trailing_stop_price:
                    self._long_trailing_stop_price = candidate

                if float(candle.LowPrice) <= self._long_trailing_stop_price:
                    self.SellMarket(exit_volume)
                    return

        elif self.Position < 0:
            exit_volume = abs(float(self.Position))

            if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
                self.BuyMarket(exit_volume)
                return

            if self._short_take_profit_price is not None and float(candle.LowPrice) <= self._short_take_profit_price:
                self.BuyMarket(exit_volume)
                return

            if self.UseTrailingStop and self._short_trailing_stop_price is not None and self._last_atr_value > 0:
                candidate = float(candle.ClosePrice) + self._last_atr_value * float(self.TrailingStopAtrMultiplier)
                if candidate < self._short_trailing_stop_price:
                    self._short_trailing_stop_price = candidate

                if float(candle.HighPrice) >= self._short_trailing_stop_price:
                    self.BuyMarket(exit_volume)
                    return

    def _initialize_long_risk_levels(self, entry_price):
        if self._last_atr_value <= 0:
            self._reset_long_risk_levels()
            return

        sl_mult = float(self.StopLossAtrMultiplier)
        tp_mult = float(self.TakeProfitAtrMultiplier)
        trail_mult = float(self.TrailingStopAtrMultiplier)

        self._long_stop_price = entry_price - self._last_atr_value * sl_mult if sl_mult > 0 else None
        self._long_take_profit_price = entry_price + self._last_atr_value * tp_mult if tp_mult > 0 else None
        self._long_trailing_stop_price = entry_price - self._last_atr_value * trail_mult if self.UseTrailingStop and trail_mult > 0 else None

    def _initialize_short_risk_levels(self, entry_price):
        if self._last_atr_value <= 0:
            self._reset_short_risk_levels()
            return

        sl_mult = float(self.StopLossAtrMultiplier)
        tp_mult = float(self.TakeProfitAtrMultiplier)
        trail_mult = float(self.TrailingStopAtrMultiplier)

        self._short_stop_price = entry_price + self._last_atr_value * sl_mult if sl_mult > 0 else None
        self._short_take_profit_price = entry_price - self._last_atr_value * tp_mult if tp_mult > 0 else None
        self._short_trailing_stop_price = entry_price + self._last_atr_value * trail_mult if self.UseTrailingStop and trail_mult > 0 else None

    def CreateClone(self):
        return revised_self_adaptive_ea_strategy()
