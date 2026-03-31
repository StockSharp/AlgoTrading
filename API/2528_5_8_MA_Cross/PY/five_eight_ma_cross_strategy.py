import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from System import Decimal
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class five_eight_ma_cross_strategy(Strategy):
    def __init__(self):
        super(five_eight_ma_cross_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 8)
        self._slow_length = self.Param("SlowLength", 21)
        self._take_profit_points = self.Param("TakeProfitPoints", 40.0)
        self._stop_loss_points = self.Param("StopLossPoints", 0.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 0.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._point_value = 1.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._trail_distance = 0.0
        self._max_price = 0.0
        self._min_price = 0.0

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

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
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(five_eight_ma_cross_strategy, self).OnStarted2(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._point_value = self._calculate_point_value()
        self._reset_position_state()

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.FastLength
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.SlowLength

        self.Volume = 0.1

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def _calculate_point_value(self):
        if self.Security is None or self.Security.PriceStep is None:
            return 1.0
        step = float(self.Security.PriceStep)
        if step <= 0.0:
            return 1.0
        import math
        digits = int(round(math.log10(1.0 / step)))
        multiplier = 10.0 if (digits == 3 or digits == 5) else 1.0
        return step * multiplier

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_price = float(candle.OpenPrice)

        fast_input = DecimalIndicatorValue(self._fast_ma, candle.ClosePrice, candle.OpenTime)
        fast_input.IsFinal = True
        fast_result = self._fast_ma.Process(fast_input)
        slow_input = DecimalIndicatorValue(self._slow_ma, candle.OpenPrice, candle.OpenTime)
        slow_input.IsFinal = True
        slow_result = self._slow_ma.Process(slow_input)

        fast_val = fast_result.Value
        slow_val = slow_result.Value

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        self._handle_risk_management(candle)

        if not self._is_initialized:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._is_initialized = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

        if cross_up and self.Position <= 0:
            self._enter_long(candle)
        elif cross_down and self.Position >= 0:
            self._enter_short(candle)

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def _enter_long(self, candle):
        pos_vol = abs(float(self.Position)) if self.Position < 0 else 0.0
        volume = float(self.Volume) + pos_vol
        if volume <= 0:
            return
        self._reset_position_state()
        self.BuyMarket(volume)

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        tp_pts = float(self.TakeProfitPoints)
        sl_pts = float(self.StopLossPoints)
        trail_pts = float(self.TrailingStopPoints)

        self._entry_price = close
        self._take_price = close + tp_pts * self._point_value if tp_pts > 0.0 else None
        self._stop_price = close - sl_pts * self._point_value if sl_pts > 0.0 else None
        self._trail_distance = trail_pts * self._point_value if trail_pts > 0.0 else 0.0
        self._max_price = high
        self._min_price = low

        if self._trail_distance > 0.0:
            trail_start = close - self._trail_distance
            if self._stop_price is None or trail_start > self._stop_price:
                self._stop_price = trail_start

    def _enter_short(self, candle):
        pos_vol = float(self.Position) if self.Position > 0 else 0.0
        volume = float(self.Volume) + pos_vol
        if volume <= 0:
            return
        self._reset_position_state()
        self.SellMarket(volume)

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        tp_pts = float(self.TakeProfitPoints)
        sl_pts = float(self.StopLossPoints)
        trail_pts = float(self.TrailingStopPoints)

        self._entry_price = close
        self._take_price = close - tp_pts * self._point_value if tp_pts > 0.0 else None
        self._stop_price = close + sl_pts * self._point_value if sl_pts > 0.0 else None
        self._trail_distance = trail_pts * self._point_value if trail_pts > 0.0 else 0.0
        self._max_price = high
        self._min_price = low

        if self._trail_distance > 0.0:
            trail_start = close + self._trail_distance
            if self._stop_price is None or trail_start < self._stop_price:
                self._stop_price = trail_start

    def _handle_risk_management(self, candle):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0 and self._entry_price is not None:
            self._max_price = max(self._max_price, high)

            if self._trail_distance > 0.0:
                trail_candidate = self._max_price - self._trail_distance
                if self._stop_price is None or trail_candidate > self._stop_price:
                    self._stop_price = trail_candidate

            if self._take_price is not None and close >= self._take_price:
                self.SellMarket(abs(float(self.Position)))
                self._reset_position_state()
                return

            if self._stop_price is not None and close <= self._stop_price:
                self.SellMarket(abs(float(self.Position)))
                self._reset_position_state()
                return

        elif self.Position < 0 and self._entry_price is not None:
            self._min_price = min(self._min_price, low)

            if self._trail_distance > 0.0:
                trail_candidate = self._min_price + self._trail_distance
                if self._stop_price is None or trail_candidate < self._stop_price:
                    self._stop_price = trail_candidate

            if self._take_price is not None and close <= self._take_price:
                self.BuyMarket(abs(float(self.Position)))
                self._reset_position_state()
                return

            if self._stop_price is not None and close >= self._stop_price:
                self.BuyMarket(abs(float(self.Position)))
                self._reset_position_state()

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._trail_distance = 0.0
        self._max_price = 0.0
        self._min_price = 0.0

    def OnReseted(self):
        super(five_eight_ma_cross_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._point_value = 1.0
        self._reset_position_state()

    def CreateClone(self):
        return five_eight_ma_cross_strategy()
