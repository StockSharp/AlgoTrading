import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class hercules_atc2006_strategy(Strategy):
    def __init__(self):
        super(hercules_atc2006_strategy, self).__init__()

        self._trigger_pips = self.Param("TriggerPips", 38)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 90)
        self._take_profit1_pips = self.Param("TakeProfit1Pips", 210)
        self._take_profit2_pips = self.Param("TakeProfit2Pips", 280)
        self._fast_ma_period = self.Param("FastMaPeriod", 1)
        self._slow_ma_period = self.Param("SlowMaPeriod", 72)
        self._stop_loss_lookback = self.Param("StopLossLookback", 4)
        self._high_low_hours = self.Param("HighLowHours", 10)
        self._blackout_hours = self.Param("BlackoutHours", 4)
        self._rsi_length_param = self.Param("RsiLength", 10)
        self._rsi_upper = self.Param("RsiUpper", 55.0)
        self._rsi_lower = self.Param("RsiLower", 45.0)
        self._daily_envelope_period = self.Param("DailyEnvelopePeriod", 24)
        self._daily_envelope_deviation = self.Param("DailyEnvelopeDeviation", 0.99)
        self._h4_envelope_period = self.Param("H4EnvelopePeriod", 96)
        self._h4_envelope_deviation = self.Param("H4EnvelopeDeviation", 0.1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_time_frame = self.Param("RsiTimeFrame", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._daily_envelope_tf = self.Param("DailyEnvelopeTimeFrame", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._h4_envelope_tf = self.Param("H4EnvelopeTimeFrame", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self.Volume = 2.0

        self._fast_history = [0.0, 0.0, 0.0, 0.0]
        self._slow_history = [0.0, 0.0, 0.0, 0.0]
        self._time_history = [None, None, None, None]
        self._high_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._history_count = 0
        self._stop_history_count = 0

        self._recent_highs = []
        self._recent_lows = []
        self._rolling_high = 0.0
        self._rolling_low = 0.0

        self._price_step = 1.0
        self._pip_size = 1.0
        self._primary_tf = TimeSpan.FromMinutes(5)
        self._high_low_length = 1

        self._pending_direction = 0
        self._trigger_price = 0.0
        self._window_end_time = None
        self._cross_price = 0.0

        self._last_rsi = 0.0
        self._rsi_ready = False

        self._daily_upper = 0.0
        self._daily_lower = 0.0
        self._daily_ready = False

        self._h4_upper = 0.0
        self._h4_lower = 0.0
        self._h4_ready = False

        self._blackout_until = None

        self._entry_price = None
        self._stop_loss = None
        self._tp1 = None
        self._tp2 = None
        self._trailing_stop = None
        self._tp1_hit = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(hercules_atc2006_strategy, self).OnStarted(time)

        self._fast_history = [0.0, 0.0, 0.0, 0.0]
        self._slow_history = [0.0, 0.0, 0.0, 0.0]
        self._time_history = [None, None, None, None]
        self._high_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._history_count = 0
        self._stop_history_count = 0
        self._recent_highs = []
        self._recent_lows = []
        self._rolling_high = 0.0
        self._rolling_low = 0.0
        self._pending_direction = 0
        self._trigger_price = 0.0
        self._window_end_time = None
        self._cross_price = 0.0
        self._last_rsi = 0.0
        self._rsi_ready = False
        self._daily_upper = 0.0
        self._daily_lower = 0.0
        self._daily_ready = False
        self._h4_upper = 0.0
        self._h4_lower = 0.0
        self._h4_ready = False
        self._blackout_until = None
        self._entry_price = None
        self._stop_loss = None
        self._tp1 = None
        self._tp2 = None
        self._trailing_stop = None
        self._tp1_hit = False

        self.StartProtection(None, None)

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        decimals = int(self.Security.Decimals) if self.Security is not None and self.Security.Decimals is not None else 0
        pip_factor = 10.0 if decimals in (3, 5) else 1.0
        self._pip_size = self._price_step * pip_factor

        ct = self.CandleType
        arg = ct.Arg
        if arg is not None and hasattr(arg, 'TotalMinutes') and arg.TotalMinutes > 0:
            self._primary_tf = arg
        else:
            self._primary_tf = TimeSpan.FromMinutes(1)

        tf_minutes = self._primary_tf.TotalMinutes
        if tf_minutes > 0:
            self._high_low_length = max(1, int(round(float(self._high_low_hours.Value) * 60.0 / tf_minutes)))
        else:
            self._high_low_length = 1

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = int(self._fast_ma_period.Value)
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = int(self._slow_ma_period.Value)

        main_sub = self.SubscribeCandles(self.CandleType)
        main_sub.Bind(fast_ma, slow_ma, self._process_primary).Start()

        self._rsi_ind = RelativeStrengthIndex()
        self._rsi_ind.Length = int(self._rsi_length_param.Value)
        rsi_sub = self.SubscribeCandles(self._rsi_time_frame.Value)
        rsi_sub.Bind(self._rsi_ind, self._process_rsi).Start()

        self._daily_ma = SimpleMovingAverage()
        self._daily_ma.Length = int(self._daily_envelope_period.Value)
        daily_sub = self.SubscribeCandles(self._daily_envelope_tf.Value)
        daily_sub.Bind(self._daily_ma, self._process_daily_envelope).Start()

        self._h4_ma = SimpleMovingAverage()
        self._h4_ma.Length = int(self._h4_envelope_period.Value)
        h4_sub = self.SubscribeCandles(self._h4_envelope_tf.Value)
        h4_sub.Bind(self._h4_ma, self._process_h4_envelope).Start()

    def _process_rsi(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        self._last_rsi = float(rsi_value)
        self._rsi_ready = True

    def _process_daily_envelope(self, candle, basis):
        if candle.State != CandleStates.Finished:
            return
        dev = float(self._daily_envelope_deviation.Value) / 100.0
        b = float(basis)
        self._daily_upper = b * (1.0 + dev)
        self._daily_lower = b * (1.0 - dev)
        self._daily_ready = self._daily_ma.IsFormed

    def _process_h4_envelope(self, candle, basis):
        if candle.State != CandleStates.Finished:
            return
        dev = float(self._h4_envelope_deviation.Value) / 100.0
        b = float(basis)
        self._h4_upper = b * (1.0 + dev)
        self._h4_lower = b * (1.0 - dev)
        self._h4_ready = self._h4_ma.IsFormed

    def _process_primary(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)

        self._update_high_low(candle)
        self._update_stop_history(candle)
        self._update_history(candle, fast, slow)
        self._update_blackout(candle.OpenTime)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._evaluate_entry(candle)
        self._manage_position(candle)

    def _shift_history(self, arr, value):
        for i in range(len(arr) - 1, 0, -1):
            arr[i] = arr[i - 1]
        arr[0] = value

    def _update_history(self, candle, fast, slow):
        self._shift_history(self._fast_history, fast)
        self._shift_history(self._slow_history, slow)
        self._shift_history(self._time_history, candle.OpenTime)

        if self._history_count < len(self._fast_history):
            self._history_count += 1

        if self._history_count < len(self._fast_history):
            return

        cross_up1 = self._fast_history[1] > self._slow_history[1] and self._fast_history[2] < self._slow_history[2]
        cross_up2 = self._fast_history[2] > self._slow_history[2] and self._fast_history[3] < self._slow_history[3]
        cross_down1 = self._fast_history[1] < self._slow_history[1] and self._fast_history[2] > self._slow_history[2]
        cross_down2 = self._fast_history[2] < self._slow_history[2] and self._fast_history[3] > self._slow_history[3]

        if cross_up1:
            cp = (self._fast_history[1] + self._fast_history[2] + self._slow_history[1] + self._slow_history[2]) / 4.0
            self._prepare_trigger(1, cp, self._time_history[1])
        elif cross_up2:
            cp = (self._fast_history[2] + self._fast_history[3] + self._slow_history[2] + self._slow_history[3]) / 4.0
            self._prepare_trigger(1, cp, self._time_history[2])
        elif cross_down1:
            cp = (self._fast_history[1] + self._fast_history[2] + self._slow_history[1] + self._slow_history[2]) / 4.0
            self._prepare_trigger(-1, cp, self._time_history[1])
        elif cross_down2:
            cp = (self._fast_history[2] + self._fast_history[3] + self._slow_history[2] + self._slow_history[3]) / 4.0
            self._prepare_trigger(-1, cp, self._time_history[2])

    def _prepare_trigger(self, direction, cross_price, cross_time):
        self._pending_direction = direction
        self._cross_price = cross_price
        pip = self._pip_size
        trigger_pips = float(self._trigger_pips.Value)
        if direction > 0:
            self._trigger_price = cross_price + trigger_pips * pip
        else:
            self._trigger_price = cross_price - trigger_pips * pip
        self._window_end_time = cross_time + self._primary_tf + self._primary_tf

    def _update_stop_history(self, candle):
        self._shift_history(self._high_stop_history, float(candle.HighPrice))
        self._shift_history(self._low_stop_history, float(candle.LowPrice))
        if self._stop_history_count < len(self._high_stop_history):
            self._stop_history_count += 1

    def _update_high_low(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._recent_highs.append(high)
        while len(self._recent_highs) > self._high_low_length:
            self._recent_highs.pop(0)
        if len(self._recent_highs) >= self._high_low_length:
            self._rolling_high = max(self._recent_highs)

        self._recent_lows.append(low)
        while len(self._recent_lows) > self._high_low_length:
            self._recent_lows.pop(0)
        if len(self._recent_lows) >= self._high_low_length:
            self._rolling_low = min(self._recent_lows)

    def _update_blackout(self, current_time):
        if self._blackout_until is not None and current_time >= self._blackout_until:
            self._blackout_until = None

    def _evaluate_entry(self, candle):
        if self._pending_direction == 0:
            return

        if self._window_end_time is not None and candle.OpenTime > self._window_end_time:
            self._pending_direction = 0
            return

        if self._blackout_until is not None and candle.OpenTime < self._blackout_until:
            return

        pos = float(self.Position)
        if pos != 0 or self._entry_price is not None:
            return

        if not self._rsi_ready:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._pending_direction > 0:
            if high < self._trigger_price:
                return
            if self._last_rsi <= float(self._rsi_upper.Value):
                return
            stop_price = self._get_stop_price(False)
            if stop_price is None:
                return
            self.BuyMarket()
            self._init_position_state(close, stop_price, True)
        else:
            if low > self._trigger_price:
                return
            if self._last_rsi >= float(self._rsi_lower.Value):
                return
            stop_price = self._get_stop_price(True)
            if stop_price is None:
                return
            self.SellMarket()
            self._init_position_state(close, stop_price, False)

        self._blackout_until = candle.OpenTime + TimeSpan.FromHours(float(self._blackout_hours.Value))
        self._pending_direction = 0

    def _get_stop_price(self, is_short):
        lookback = int(self._stop_loss_lookback.Value)
        if self._stop_history_count <= lookback:
            return None
        if is_short:
            return self._high_stop_history[lookback]
        else:
            return self._low_stop_history[lookback]

    def _init_position_state(self, entry_price, stop_price, is_long):
        self._entry_price = entry_price
        self._stop_loss = stop_price
        self._tp1_hit = False
        self._trailing_stop = None
        pip = self._pip_size
        tp1_pips = float(self._take_profit1_pips.Value)
        tp2_pips = float(self._take_profit2_pips.Value)
        if tp1_pips > 0:
            self._tp1 = entry_price + tp1_pips * pip if is_long else entry_price - tp1_pips * pip
        else:
            self._tp1 = None
        if tp2_pips > 0:
            self._tp2 = entry_price + tp2_pips * pip if is_long else entry_price - tp2_pips * pip
        else:
            self._tp2 = None

    def _manage_position(self, candle):
        if self._entry_price is None:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        pip = self._pip_size
        trail_pips = float(self._trailing_stop_pips.Value)

        pos = float(self.Position)
        if pos > 0:
            self._update_trailing_stop(close, True)

            if self._stop_loss is not None and low <= self._stop_loss:
                self.SellMarket(pos)
                self._reset_position_state()
                return

            if self._trailing_stop is not None and low <= self._trailing_stop:
                pos = float(self.Position)
                self.SellMarket(pos)
                self._reset_position_state()
                return

            if not self._tp1_hit and self._tp1 is not None and high >= self._tp1:
                pos = float(self.Position)
                half = pos / 2.0
                if half > 0:
                    self.SellMarket(half)
                self._tp1_hit = True

            pos = float(self.Position)
            if self._tp2 is not None and high >= self._tp2:
                if pos > 0:
                    self.SellMarket(pos)
                self._reset_position_state()

        elif pos < 0:
            self._update_trailing_stop(close, False)

            if self._stop_loss is not None and high >= self._stop_loss:
                self.BuyMarket(abs(pos))
                self._reset_position_state()
                return

            if self._trailing_stop is not None and high >= self._trailing_stop:
                pos = float(self.Position)
                self.BuyMarket(abs(pos))
                self._reset_position_state()
                return

            if not self._tp1_hit and self._tp1 is not None and low <= self._tp1:
                pos = float(self.Position)
                half = abs(pos) / 2.0
                if half > 0:
                    self.BuyMarket(half)
                self._tp1_hit = True

            pos = float(self.Position)
            if self._tp2 is not None and low <= self._tp2:
                if pos < 0:
                    self.BuyMarket(abs(pos))
                self._reset_position_state()
        else:
            self._reset_position_state()

    def _update_trailing_stop(self, close_price, is_long):
        if float(self._trailing_stop_pips.Value) <= 0:
            return
        pip = self._pip_size
        trail_pips = float(self._trailing_stop_pips.Value)
        if is_long:
            candidate = close_price - trail_pips * pip
        else:
            candidate = close_price + trail_pips * pip

        if self._trailing_stop is None:
            self._trailing_stop = candidate
        elif is_long and candidate > self._trailing_stop:
            self._trailing_stop = candidate
        elif not is_long and candidate < self._trailing_stop:
            self._trailing_stop = candidate

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_loss = None
        self._tp1 = None
        self._tp2 = None
        self._trailing_stop = None
        self._tp1_hit = False

    def OnReseted(self):
        super(hercules_atc2006_strategy, self).OnReseted()
        self._fast_history = [0.0, 0.0, 0.0, 0.0]
        self._slow_history = [0.0, 0.0, 0.0, 0.0]
        self._time_history = [None, None, None, None]
        self._high_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._history_count = 0
        self._stop_history_count = 0
        self._recent_highs = []
        self._recent_lows = []
        self._rolling_high = 0.0
        self._rolling_low = 0.0
        self._price_step = 1.0
        self._pip_size = 1.0
        self._high_low_length = 1
        self._pending_direction = 0
        self._trigger_price = 0.0
        self._window_end_time = None
        self._cross_price = 0.0
        self._last_rsi = 0.0
        self._rsi_ready = False
        self._daily_upper = 0.0
        self._daily_lower = 0.0
        self._daily_ready = False
        self._h4_upper = 0.0
        self._h4_lower = 0.0
        self._h4_ready = False
        self._blackout_until = None
        self._entry_price = None
        self._stop_loss = None
        self._tp1 = None
        self._tp2 = None
        self._trailing_stop = None
        self._tp1_hit = False

    def CreateClone(self):
        return hercules_atc2006_strategy()
