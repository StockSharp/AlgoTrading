import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import (SmoothedMovingAverage, Momentum,
    DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class momentum_m15_strategy(Strategy):
    def __init__(self):
        super(momentum_m15_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._ma_period = self.Param("MaPeriod", 26)
        self._ma_shift = self.Param("MaShift", 8)
        self._momentum_period = self.Param("MomentumPeriod", 23)
        self._momentum_threshold = self.Param("MomentumThreshold", 100.0)
        self._momentum_shift = self.Param("MomentumShift", -0.2)
        self._momentum_open_length = self.Param("MomentumOpenLength", 6)
        self._momentum_close_length = self.Param("MomentumCloseLength", 10)
        self._gap_level = self.Param("GapLevel", 30)
        self._gap_timeout = self.Param("GapTimeout", 100)
        self._trailing_stop = self.Param("TrailingStop", 0.0)

        self._ma = None
        self._momentum = None
        self._ma_history = []
        self._momentum_history = []
        self._previous_close = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._gap_timer = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(momentum_m15_strategy, self).OnStarted(time)

        self._ma = SmoothedMovingAverage()
        self._ma.Length = self._ma_period.Value
        self._momentum = Momentum()
        self._momentum.Length = self._momentum_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._ma is None or self._momentum is None:
            return

        ma_value = self._process_ma(candle)
        momentum_value = self._process_momentum(candle)

        if ma_value is None or momentum_value is None:
            self._previous_close = float(candle.ClosePrice)
            return

        previous_close = self._previous_close
        self._previous_close = float(candle.ClosePrice)

        if previous_close is None:
            return

        self._handle_gap_filter(previous_close, float(candle.OpenPrice))

        if self._gap_timer > 0:
            self._gap_timer -= 1
            if self._gap_timer > 0:
                return

        if self.Position == 0:
            self._try_open_positions(previous_close, float(candle.OpenPrice), ma_value, momentum_value)
        else:
            self._manage_existing_position(previous_close, candle, ma_value, momentum_value)

    def _process_ma(self, candle):
        price = float(candle.LowPrice)
        value = self._ma.Process(DecimalIndicatorValue(self._ma, price, candle.OpenTime))

        if value.IsEmpty or not self._ma.IsFormed:
            return None

        ma = float(value)
        self._ma_history.append(ma)

        max_count = self._ma_shift.Value + 1
        while len(self._ma_history) > max_count:
            self._ma_history.pop(0)

        index = len(self._ma_history) - 1 - self._ma_shift.Value
        if index < 0 or index >= len(self._ma_history):
            return None

        return self._ma_history[index]

    def _process_momentum(self, candle):
        price = float(candle.OpenPrice)
        value = self._momentum.Process(DecimalIndicatorValue(self._momentum, price, candle.OpenTime))

        if value.IsEmpty or not self._momentum.IsFormed:
            return None

        mom = float(value)
        self._momentum_history.append(mom)

        max_len = max(max(self._momentum_open_length.Value, self._momentum_close_length.Value), 1)
        while len(self._momentum_history) > max_len:
            self._momentum_history.pop(0)

        return mom

    def _handle_gap_filter(self, previous_close, current_open):
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if price_step <= 0:
            return

        gap = (current_open - previous_close) / price_step
        if gap > self._gap_level.Value:
            self._gap_timer = self._gap_timeout.Value

    def _try_open_positions(self, previous_close, current_open, ma_value, momentum_value):
        long_momentum_ok = self._momentum_open_length.Value > 0 and self._is_momentum_down_sequence(self._momentum_open_length.Value)
        short_momentum_ok = self._momentum_open_length.Value > 0 and self._is_momentum_up_sequence(self._momentum_open_length.Value)

        long_condition = (momentum_value < self._momentum_threshold.Value + self._momentum_shift.Value
                          and previous_close < ma_value
                          and current_open < ma_value
                          and long_momentum_ok)

        short_condition = (momentum_value > self._momentum_threshold.Value - self._momentum_shift.Value
                           and previous_close > ma_value
                           and current_open > ma_value
                           and short_momentum_ok)

        if long_condition:
            vol = self._trade_volume.Value if self._trade_volume.Value > 0 else float(self.Volume)
            self.BuyMarket(vol)
            self._long_trailing_stop = None
            self._short_trailing_stop = None
        elif short_condition:
            vol = self._trade_volume.Value if self._trade_volume.Value > 0 else float(self.Volume)
            self.SellMarket(vol)
            self._long_trailing_stop = None
            self._short_trailing_stop = None

    def _manage_existing_position(self, previous_close, candle, ma_value, momentum_value):
        if self.Position > 0:
            exit_momentum = self._momentum_close_length.Value > 0 and self._is_momentum_down_sequence(self._momentum_close_length.Value)
            should_close = exit_momentum or previous_close < ma_value

            if should_close:
                self.SellMarket(self.Position)
                self._long_trailing_stop = None
                return

            self._update_long_trailing(candle)
        elif self.Position < 0:
            exit_momentum = self._momentum_close_length.Value > 0 and self._is_momentum_up_sequence(self._momentum_close_length.Value)
            should_close = exit_momentum or previous_close > ma_value

            if should_close:
                self.BuyMarket(abs(self.Position))
                self._short_trailing_stop = None
                return

            self._update_short_trailing(candle)

    def _update_long_trailing(self, candle):
        if self._trailing_stop.Value <= 0:
            return
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if price_step <= 0:
            return

        distance = self._trailing_stop.Value * price_step
        candidate = float(candle.LowPrice) - distance

        if self._long_trailing_stop is None or candidate > self._long_trailing_stop:
            self._long_trailing_stop = candidate

        if self._long_trailing_stop is not None and float(candle.LowPrice) <= self._long_trailing_stop:
            self.SellMarket(self.Position)
            self._long_trailing_stop = None

    def _update_short_trailing(self, candle):
        if self._trailing_stop.Value <= 0:
            return
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if price_step <= 0:
            return

        distance = self._trailing_stop.Value * price_step
        candidate = float(candle.HighPrice) + distance

        if self._short_trailing_stop is None or candidate < self._short_trailing_stop:
            self._short_trailing_stop = candidate

        if self._short_trailing_stop is not None and float(candle.HighPrice) >= self._short_trailing_stop:
            self.BuyMarket(abs(self.Position))
            self._short_trailing_stop = None

    def _is_momentum_down_sequence(self, length):
        if length <= 0 or len(self._momentum_history) < length:
            return False
        start = len(self._momentum_history) - length
        previous = self._momentum_history[start]
        for i in range(start + 1, len(self._momentum_history)):
            current = self._momentum_history[i]
            if current > previous:
                return False
            previous = current
        return True

    def _is_momentum_up_sequence(self, length):
        if length <= 0 or len(self._momentum_history) < length:
            return False
        start = len(self._momentum_history) - length
        previous = self._momentum_history[start]
        for i in range(start + 1, len(self._momentum_history)):
            current = self._momentum_history[i]
            if current < previous:
                return False
            previous = current
        return True

    def OnReseted(self):
        super(momentum_m15_strategy, self).OnReseted()
        self._ma = None
        self._momentum = None
        self._ma_history = []
        self._momentum_history = []
        self._previous_close = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._gap_timer = 0

    def CreateClone(self):
        return momentum_m15_strategy()
