import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class forex_profit_strategy(Strategy):
    def __init__(self):
        super(forex_profit_strategy, self).__init__()

        self._fast_ema_length = self.Param("FastEmaLength", 10)
        self._medium_ema_length = self.Param("MediumEmaLength", 25)
        self._slow_ema_length = self.Param("SlowEmaLength", 50)
        self._take_profit_buy_points = self.Param("TakeProfitBuyPoints", 55.0)
        self._take_profit_sell_points = self.Param("TakeProfitSellPoints", 65.0)
        self._stop_loss_buy_points = self.Param("StopLossBuyPoints", 60.0)
        self._stop_loss_sell_points = self.Param("StopLossSellPoints", 85.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 74.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 5.0)
        self._profit_threshold = self.Param("ProfitThreshold", 10.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._sar_acceleration = self.Param("SarAcceleration", 0.02)
        self._sar_max_acceleration = self.Param("SarMaxAcceleration", 0.2)

        self._ema_fast = None
        self._ema_medium = None
        self._ema_slow = None
        self._sar = None
        self._ema10_prev = None
        self._ema10_prev_prev = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    @property
    def FastEmaLength(self):
        return self._fast_ema_length.Value

    @FastEmaLength.setter
    def FastEmaLength(self, value):
        self._fast_ema_length.Value = value

    @property
    def MediumEmaLength(self):
        return self._medium_ema_length.Value

    @MediumEmaLength.setter
    def MediumEmaLength(self, value):
        self._medium_ema_length.Value = value

    @property
    def SlowEmaLength(self):
        return self._slow_ema_length.Value

    @SlowEmaLength.setter
    def SlowEmaLength(self, value):
        self._slow_ema_length.Value = value

    @property
    def TakeProfitBuyPoints(self):
        return self._take_profit_buy_points.Value

    @TakeProfitBuyPoints.setter
    def TakeProfitBuyPoints(self, value):
        self._take_profit_buy_points.Value = value

    @property
    def TakeProfitSellPoints(self):
        return self._take_profit_sell_points.Value

    @TakeProfitSellPoints.setter
    def TakeProfitSellPoints(self, value):
        self._take_profit_sell_points.Value = value

    @property
    def StopLossBuyPoints(self):
        return self._stop_loss_buy_points.Value

    @StopLossBuyPoints.setter
    def StopLossBuyPoints(self, value):
        self._stop_loss_buy_points.Value = value

    @property
    def StopLossSellPoints(self):
        return self._stop_loss_sell_points.Value

    @StopLossSellPoints.setter
    def StopLossSellPoints(self, value):
        self._stop_loss_sell_points.Value = value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @TrailingStepPoints.setter
    def TrailingStepPoints(self, value):
        self._trailing_step_points.Value = value

    @property
    def ProfitThreshold(self):
        return self._profit_threshold.Value

    @ProfitThreshold.setter
    def ProfitThreshold(self, value):
        self._profit_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SarAcceleration(self):
        return self._sar_acceleration.Value

    @SarAcceleration.setter
    def SarAcceleration(self, value):
        self._sar_acceleration.Value = value

    @property
    def SarMaxAcceleration(self):
        return self._sar_max_acceleration.Value

    @SarMaxAcceleration.setter
    def SarMaxAcceleration(self, value):
        self._sar_max_acceleration.Value = value

    def OnStarted2(self, time):
        super(forex_profit_strategy, self).OnStarted2(time)

        self._ema10_prev = None
        self._ema10_prev_prev = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self.FastEmaLength
        self._ema_medium = ExponentialMovingAverage()
        self._ema_medium.Length = self.MediumEmaLength
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self.SlowEmaLength
        self._sar = ParabolicSar()
        self._sar.Acceleration = self.SarAcceleration
        self._sar.AccelerationMax = self.SarMaxAcceleration

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        ema10_result = self._ema_fast.Process(median, candle.CloseTime, True)
        ema25_result = self._ema_medium.Process(median, candle.CloseTime, True)
        ema50_result = self._ema_slow.Process(median, candle.CloseTime, True)
        sar_result = self._sar.Process(candle)

        ema10_value = float(ema10_result)
        ema25_value = float(ema25_result)
        ema50_value = float(ema50_result)
        sar_value = float(sar_result)

        ema10_prev = self._ema10_prev
        ema10_prev_prev = self._ema10_prev_prev

        if not self._ema_slow.IsFormed or not self._sar.IsFormed:
            self._ema10_prev_prev = ema10_prev
            self._ema10_prev = ema10_value
            return

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        long_signal = (ema10_value > ema25_value and
                       ema10_value > ema50_value and
                       ema10_prev_prev is not None and
                       ema10_prev_prev <= ema50_value and
                       sar_value < close)

        short_signal = (ema10_value < ema25_value and
                        ema10_value < ema50_value and
                        ema10_prev_prev is not None and
                        ema10_prev_prev >= ema50_value and
                        sar_value > close)

        if self.Position == 0:
            if long_signal:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - step * float(self.StopLossBuyPoints)
                self._take_profit_price = close + step * float(self.TakeProfitBuyPoints)
            elif short_signal:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + step * float(self.StopLossSellPoints)
                self._take_profit_price = close - step * float(self.TakeProfitSellPoints)
        elif self.Position > 0:
            self._manage_long(candle, ema10_value, ema10_prev, step)
        elif self.Position < 0:
            self._manage_short(candle, ema10_value, ema10_prev, step)

        self._ema10_prev_prev = ema10_prev
        self._ema10_prev = ema10_value

    def _manage_long(self, candle, ema10_value, ema10_prev, step):
        if self._entry_price is None:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        profit = self._compute_profit(close, step)

        if ema10_prev is not None and ema10_value < ema10_prev and profit > float(self.ProfitThreshold):
            self.SellMarket()
            self._reset_position_targets()
            return

        if self._take_profit_price is not None and high >= self._take_profit_price:
            self.SellMarket()
            self._reset_position_targets()
            return

        if self._stop_price is not None and low <= self._stop_price:
            self.SellMarket()
            self._reset_position_targets()
            return

        self._update_long_trailing(candle, step)

    def _manage_short(self, candle, ema10_value, ema10_prev, step):
        if self._entry_price is None:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        profit = self._compute_profit(close, step)

        if ema10_prev is not None and ema10_value > ema10_prev and profit > float(self.ProfitThreshold):
            self.BuyMarket()
            self._reset_position_targets()
            return

        if self._take_profit_price is not None and low <= self._take_profit_price:
            self.BuyMarket()
            self._reset_position_targets()
            return

        if self._stop_price is not None and high >= self._stop_price:
            self.BuyMarket()
            self._reset_position_targets()
            return

        self._update_short_trailing(candle, step)

    def _update_long_trailing(self, candle, step):
        trail_pts = float(self.TrailingStopPoints)
        if trail_pts <= 0.0 or self._entry_price is None:
            return

        close = float(candle.ClosePrice)
        trailing_distance = step * trail_pts
        trailing_step = step * float(self.TrailingStepPoints)
        movement = close - self._entry_price

        if movement > trailing_distance:
            new_stop = close - trailing_distance
            if self._stop_price is None or new_stop - self._stop_price >= trailing_step:
                self._stop_price = new_stop

    def _update_short_trailing(self, candle, step):
        trail_pts = float(self.TrailingStopPoints)
        if trail_pts <= 0.0 or self._entry_price is None:
            return

        close = float(candle.ClosePrice)
        trailing_distance = step * trail_pts
        trailing_step = step * float(self.TrailingStepPoints)
        movement = self._entry_price - close

        if movement > trailing_distance:
            new_stop = close + trailing_distance
            if self._stop_price is None or self._stop_price - new_stop >= trailing_step:
                self._stop_price = new_stop

    def _compute_profit(self, current_price, step):
        if self._entry_price is None or self.Position == 0:
            return 0.0
        ticks = (current_price - self._entry_price) / step
        return ticks * step * self.Position

    def _reset_position_targets(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def OnReseted(self):
        super(forex_profit_strategy, self).OnReseted()
        self._ema_fast = None
        self._ema_medium = None
        self._ema_slow = None
        self._sar = None
        self._ema10_prev = None
        self._ema10_prev_prev = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return forex_profit_strategy()
