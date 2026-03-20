import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SmoothedMovingAverage, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class get_trend_strategy(Strategy):
    def __init__(self):
        super(get_trend_strategy, self).__init__()

        self._m15_candle_type = self.Param("M15CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._h1_candle_type = self.Param("H1CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._ma_m15_length = self.Param("MaM15Length", 99)
        self._ma_h1_length = self.Param("MaH1Length", 184)
        self._stochastic_length = self.Param("StochasticLength", 27)
        self._stochastic_signal_length = self.Param("StochasticSignalLength", 3)
        self._threshold_points = self.Param("ThresholdPoints", 10.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 540.0)
        self._stop_loss_points = self.Param("StopLossPoints", 90.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 20.0)
        self._trade_volume = self.Param("TradeVolume", 0.1)

        self._ma_h1_value = None
        self._last_h1_close = None
        self._prev_stoch_fast = None
        self._prev_stoch_slow = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def M15CandleType(self):
        return self._m15_candle_type.Value

    @M15CandleType.setter
    def M15CandleType(self, value):
        self._m15_candle_type.Value = value

    @property
    def H1CandleType(self):
        return self._h1_candle_type.Value

    @H1CandleType.setter
    def H1CandleType(self, value):
        self._h1_candle_type.Value = value

    @property
    def MaM15Length(self):
        return self._ma_m15_length.Value

    @MaM15Length.setter
    def MaM15Length(self, value):
        self._ma_m15_length.Value = value

    @property
    def MaH1Length(self):
        return self._ma_h1_length.Value

    @MaH1Length.setter
    def MaH1Length(self, value):
        self._ma_h1_length.Value = value

    @property
    def StochasticLength(self):
        return self._stochastic_length.Value

    @StochasticLength.setter
    def StochasticLength(self, value):
        self._stochastic_length.Value = value

    @property
    def StochasticSignalLength(self):
        return self._stochastic_signal_length.Value

    @StochasticSignalLength.setter
    def StochasticSignalLength(self, value):
        self._stochastic_signal_length.Value = value

    @property
    def ThresholdPoints(self):
        return self._threshold_points.Value

    @ThresholdPoints.setter
    def ThresholdPoints(self, value):
        self._threshold_points.Value = value

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
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    def OnStarted(self, time):
        super(get_trend_strategy, self).OnStarted(time)

        self._ma_h1_value = None
        self._last_h1_close = None
        self._prev_stoch_fast = None
        self._prev_stoch_slow = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

        self._ma_m15 = SmoothedMovingAverage()
        self._ma_m15.Length = self.MaM15Length
        self._ma_h1 = SmoothedMovingAverage()
        self._ma_h1.Length = self.MaH1Length
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticLength
        self._stochastic.D.Length = self.StochasticSignalLength

        m15_sub = self.SubscribeCandles(self.M15CandleType)
        m15_sub.BindEx(self._ma_m15, self._stochastic, self.ProcessM15Candle).Start()

        h1_sub = self.SubscribeCandles(self.H1CandleType)
        h1_sub.Bind(self._ma_h1, self.ProcessH1Candle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessH1Candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        self._ma_h1_value = float(ma_value)
        self._last_h1_close = float(candle.ClosePrice)

    def ProcessM15Candle(self, candle, ma_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if not ma_value.IsFinal or not stochastic_value.IsFinal:
            return

        ma = float(ma_value)

        stoch_k = stochastic_value.K
        stoch_d = stochastic_value.D
        if stoch_k is None or stoch_d is None:
            return

        stoch_fast = float(stoch_k)
        stoch_slow = float(stoch_d)

        self._manage_open_position(candle)

        if self._ma_h1_value is None or self._last_h1_close is None:
            self._prev_stoch_fast = stoch_fast
            self._prev_stoch_slow = stoch_slow
            return

        ma_h1 = self._ma_h1_value
        price_h1 = self._last_h1_close
        price_m15 = float(candle.ClosePrice)

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        threshold = float(self.ThresholdPoints) * step

        near_lower = price_m15 < ma and price_h1 < ma_h1 and (ma - price_m15) <= threshold
        near_upper = price_m15 > ma and price_h1 > ma_h1 and (price_m15 - ma) <= threshold

        cross_up = (self._prev_stoch_fast is not None and self._prev_stoch_slow is not None and
                    self._prev_stoch_fast < self._prev_stoch_slow and stoch_fast > stoch_slow)
        cross_down = (self._prev_stoch_fast is not None and self._prev_stoch_slow is not None and
                      self._prev_stoch_fast > self._prev_stoch_slow and stoch_fast < stoch_slow)

        if near_lower and stoch_slow < 20.0 and stoch_fast < 20.0 and cross_up and self.Position <= 0:
            self._enter_long(float(candle.ClosePrice), step)
        elif near_upper and stoch_slow > 80.0 and stoch_fast > 80.0 and cross_down and self.Position >= 0:
            self._enter_short(float(candle.ClosePrice), step)

        self._prev_stoch_fast = stoch_fast
        self._prev_stoch_slow = stoch_slow

    def _enter_long(self, entry_price, price_step):
        self.BuyMarket()
        self._entry_price = entry_price
        self._take_price = entry_price + float(self.TakeProfitPoints) * price_step
        self._stop_price = entry_price - float(self.StopLossPoints) * price_step

    def _enter_short(self, entry_price, price_step):
        self.SellMarket()
        self._entry_price = entry_price
        self._take_price = entry_price - float(self.TakeProfitPoints) * price_step
        self._stop_price = entry_price + float(self.StopLossPoints) * price_step

    def _manage_open_position(self, candle):
        if self.Position == 0:
            return

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        trailing_distance = float(self.TrailingStopPoints) * step

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if self._entry_price is not None and float(self.TrailingStopPoints) > 0.0 and close - self._entry_price >= trailing_distance:
                candidate = close - trailing_distance
                if self._stop_price is not None:
                    self._stop_price = max(self._stop_price, candidate)
                else:
                    self._stop_price = candidate

            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_protection()
                return

            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_protection()

        elif self.Position < 0:
            if self._entry_price is not None and float(self.TrailingStopPoints) > 0.0 and self._entry_price - close >= trailing_distance:
                candidate = close + trailing_distance
                if self._stop_price is not None:
                    self._stop_price = min(self._stop_price, candidate)
                else:
                    self._stop_price = candidate

            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_protection()
                return

            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_protection()

    def _reset_protection(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(get_trend_strategy, self).OnReseted()
        self._ma_h1_value = None
        self._last_h1_close = None
        self._prev_stoch_fast = None
        self._prev_stoch_slow = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return get_trend_strategy()
