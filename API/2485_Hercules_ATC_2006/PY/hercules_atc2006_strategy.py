import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
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
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._fast_history = [0.0, 0.0, 0.0, 0.0]
        self._slow_history = [0.0, 0.0, 0.0, 0.0]
        self._high_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._history_count = 0
        self._stop_history_count = 0

        self._recent_highs = []
        self._recent_lows = []
        self._rolling_high = 0.0
        self._rolling_low = 0.0

        self._pip_size = 1.0
        self._high_low_length = 1

        self._pending_direction = 0
        self._trigger_price = 0.0
        self._window_bars = 0
        self._cross_price = 0.0

        self._last_rsi = 0.0
        self._rsi_ready = False

        self._blackout_count = 0

        self._entry_price = None
        self._stop_loss = None
        self._tp1 = None
        self._tp2 = None
        self._trailing_stop = None
        self._tp1_hit = False

    @property
    def TriggerPips(self):
        return self._trigger_pips.Value

    @TriggerPips.setter
    def TriggerPips(self, value):
        self._trigger_pips.Value = value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @TrailingStopPips.setter
    def TrailingStopPips(self, value):
        self._trailing_stop_pips.Value = value

    @property
    def TakeProfit1Pips(self):
        return self._take_profit1_pips.Value

    @TakeProfit1Pips.setter
    def TakeProfit1Pips(self, value):
        self._take_profit1_pips.Value = value

    @property
    def TakeProfit2Pips(self):
        return self._take_profit2_pips.Value

    @TakeProfit2Pips.setter
    def TakeProfit2Pips(self, value):
        self._take_profit2_pips.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_ma_period.Value = value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_ma_period.Value = value

    @property
    def StopLossLookback(self):
        return self._stop_loss_lookback.Value

    @StopLossLookback.setter
    def StopLossLookback(self, value):
        self._stop_loss_lookback.Value = value

    @property
    def HighLowHours(self):
        return self._high_low_hours.Value

    @HighLowHours.setter
    def HighLowHours(self, value):
        self._high_low_hours.Value = value

    @property
    def BlackoutHours(self):
        return self._blackout_hours.Value

    @BlackoutHours.setter
    def BlackoutHours(self, value):
        self._blackout_hours.Value = value

    @property
    def RsiLength(self):
        return self._rsi_length_param.Value

    @RsiLength.setter
    def RsiLength(self, value):
        self._rsi_length_param.Value = value

    @property
    def RsiUpper(self):
        return self._rsi_upper.Value

    @RsiUpper.setter
    def RsiUpper(self, value):
        self._rsi_upper.Value = value

    @property
    def RsiLower(self):
        return self._rsi_lower.Value

    @RsiLower.setter
    def RsiLower(self, value):
        self._rsi_lower.Value = value

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
        self._window_bars = 0
        self._cross_price = 0.0
        self._last_rsi = 0.0
        self._rsi_ready = False
        self._blackout_count = 0
        self._entry_price = None
        self._stop_loss = None
        self._tp1 = None
        self._tp2 = None
        self._trailing_stop = None
        self._tp1_hit = False

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        self._pip_size = step
        self._high_low_length = max(1, int(self.HighLowHours) * 12)

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowMaPeriod

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength
        self._rsi_ind = rsi

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        rsi_val = float(rsi_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        self._last_rsi = rsi_val
        if self._rsi_ind.IsFormed:
            self._rsi_ready = True

        self._update_high_low(high, low)
        self._update_stop_history(high, low)
        self._update_history(fast, slow)

        if self._blackout_count > 0:
            self._blackout_count -= 1

        self._evaluate_entry(candle)
        self._manage_position(candle)

    def _shift_history(self, arr, value):
        for i in range(len(arr) - 1, 0, -1):
            arr[i] = arr[i - 1]
        arr[0] = value

    def _update_history(self, fast, slow):
        self._shift_history(self._fast_history, fast)
        self._shift_history(self._slow_history, slow)

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
            self._prepare_trigger(1, cp)
        elif cross_up2:
            cp = (self._fast_history[2] + self._fast_history[3] + self._slow_history[2] + self._slow_history[3]) / 4.0
            self._prepare_trigger(1, cp)
        elif cross_down1:
            cp = (self._fast_history[1] + self._fast_history[2] + self._slow_history[1] + self._slow_history[2]) / 4.0
            self._prepare_trigger(-1, cp)
        elif cross_down2:
            cp = (self._fast_history[2] + self._fast_history[3] + self._slow_history[2] + self._slow_history[3]) / 4.0
            self._prepare_trigger(-1, cp)

    def _prepare_trigger(self, direction, cross_price):
        self._pending_direction = direction
        self._cross_price = cross_price
        pip = self._pip_size
        trigger_pips = float(self.TriggerPips)
        if direction > 0:
            self._trigger_price = cross_price + trigger_pips * pip
        else:
            self._trigger_price = cross_price - trigger_pips * pip
        self._window_bars = 2

    def _update_stop_history(self, high, low):
        self._shift_history(self._high_stop_history, high)
        self._shift_history(self._low_stop_history, low)
        if self._stop_history_count < len(self._high_stop_history):
            self._stop_history_count += 1

    def _update_high_low(self, high, low):
        self._recent_highs.append(high)
        self._recent_lows.append(low)
        while len(self._recent_highs) > self._high_low_length:
            self._recent_highs.pop(0)
        while len(self._recent_lows) > self._high_low_length:
            self._recent_lows.pop(0)
        if len(self._recent_highs) >= self._high_low_length:
            self._rolling_high = max(self._recent_highs)
        if len(self._recent_lows) >= self._high_low_length:
            self._rolling_low = min(self._recent_lows)

    def _evaluate_entry(self, candle):
        if self._pending_direction == 0:
            return

        if self._window_bars <= 0:
            self._pending_direction = 0
            return
        self._window_bars -= 1

        if self._blackout_count > 0:
            return

        if self.Position != 0 or self._entry_price is not None:
            return

        if not self._rsi_ready:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        pip = self._pip_size

        if self._pending_direction > 0:
            if high < self._trigger_price:
                return
            if self._last_rsi <= float(self.RsiUpper):
                return
            stop_price = self._get_stop_price(False)
            if stop_price is None:
                return
            self.BuyMarket()
            self._init_position_state(close, stop_price, True)
        else:
            if low > self._trigger_price:
                return
            if self._last_rsi >= float(self.RsiLower):
                return
            stop_price = self._get_stop_price(True)
            if stop_price is None:
                return
            self.SellMarket()
            self._init_position_state(close, stop_price, False)

        self._blackout_count = int(self.BlackoutHours) * 12
        self._pending_direction = 0

    def _get_stop_price(self, is_short):
        lookback = int(self.StopLossLookback)
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
        tp1_pips = float(self.TakeProfit1Pips)
        tp2_pips = float(self.TakeProfit2Pips)
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
        trail_pips = float(self.TrailingStopPips)

        if self.Position > 0:
            if trail_pips > 0:
                candidate = close - trail_pips * pip
                if self._trailing_stop is None or candidate > self._trailing_stop:
                    self._trailing_stop = candidate

            if self._stop_loss is not None and low <= self._stop_loss:
                self.SellMarket()
                self._reset_position_state()
                return

            if self._trailing_stop is not None and low <= self._trailing_stop:
                self.SellMarket()
                self._reset_position_state()
                return

            if not self._tp1_hit and self._tp1 is not None and high >= self._tp1:
                self._tp1_hit = True

            if self._tp2 is not None and high >= self._tp2:
                self.SellMarket()
                self._reset_position_state()

        elif self.Position < 0:
            if trail_pips > 0:
                candidate = close + trail_pips * pip
                if self._trailing_stop is None or candidate < self._trailing_stop:
                    self._trailing_stop = candidate

            if self._stop_loss is not None and high >= self._stop_loss:
                self.BuyMarket()
                self._reset_position_state()
                return

            if self._trailing_stop is not None and high >= self._trailing_stop:
                self.BuyMarket()
                self._reset_position_state()
                return

            if not self._tp1_hit and self._tp1 is not None and low <= self._tp1:
                self._tp1_hit = True

            if self._tp2 is not None and low <= self._tp2:
                self.BuyMarket()
                self._reset_position_state()
        else:
            self._reset_position_state()

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
        self._high_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_stop_history = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._history_count = 0
        self._stop_history_count = 0
        self._recent_highs = []
        self._recent_lows = []
        self._rolling_high = 0.0
        self._rolling_low = 0.0
        self._pip_size = 1.0
        self._high_low_length = 1
        self._pending_direction = 0
        self._trigger_price = 0.0
        self._window_bars = 0
        self._cross_price = 0.0
        self._last_rsi = 0.0
        self._rsi_ready = False
        self._blackout_count = 0
        self._entry_price = None
        self._stop_loss = None
        self._tp1 = None
        self._tp2 = None
        self._trailing_stop = None
        self._tp1_hit = False

    def CreateClone(self):
        return hercules_atc2006_strategy()
