import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    DecimalIndicatorValue, ExponentialMovingAverage,
    SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


# MA method constants
MA_SIMPLE = 0
MA_EXPONENTIAL = 1
MA_SMOOTHED = 2
MA_WEIGHTED = 3

# Signal state constants
SIGNAL_NEUTRAL = 0
SIGNAL_BAR_A = 1
SIGNAL_BAR_B = 2


class var_mov_avg_strategy(Strategy):
    """Variable Moving Average reversal strategy. Tracks adaptive VMA swings and
    enters on the Bar A/Bar B breakout pattern with MA-based trailing stop."""

    def __init__(self):
        super(var_mov_avg_strategy, self).__init__()

        self._ama_period = self.Param("AmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Period", "Fast smoothing period for VMA", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Period", "Slow smoothing period for VMA", "Indicators")
        self._smoothing_power = self.Param("SmoothingPower", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Smoothing Power", "Exponent applied to the smoothing coefficient", "Indicators")
        self._signal_pips_bar_a = self.Param("SignalPipsBarA", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bar A Distance", "Pips distance below/above VMA for Bar A", "Signals")
        self._signal_pips_bar_b = self.Param("SignalPipsBarB", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bar B Distance", "Extra pips distance for Bar B confirmation", "Signals")
        self._signal_pips_trade = self.Param("SignalPipsTrade", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Offset", "Pips offset from Bar B extreme to entry", "Signals")
        self._entry_pips_diff = self.Param("EntryPipsDiff", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Band", "Accepted pips range around the entry price", "Signals")
        self._stop_pips_diff = self.Param("StopPipsDiff", 34.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Offset", "Pips offset from the trailing moving average", "Risk")
        self._stop_ma_period = self.Param("StopMaPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop MA Period", "Period of the trailing moving average", "Risk")
        self._stop_ma_shift = self.Param("StopMaShift", 0) \
            .SetDisplay("Stop MA Shift", "Bars shift applied to the stop moving average", "Risk")
        self._stop_ma_method = self.Param("StopMaMethod", MA_EXPONENTIAL) \
            .SetDisplay("Stop MA Method", "Moving average type used for stops", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Working candle timeframe", "General")

        # VMA internal state
        self._vma_closes = []
        self._vma_prev_ama = None

        # Stop MA internal state
        self._stop_low_ma = None
        self._stop_high_ma = None
        self._low_ma_buffer = []
        self._high_ma_buffer = []

        # Signal trackers
        self._long_state = SIGNAL_NEUTRAL
        self._long_bar_a_ref = 0.0
        self._long_entry_price = 0.0
        self._short_state = SIGNAL_NEUTRAL
        self._short_bar_a_ref = 0.0
        self._short_entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AmaPeriod(self):
        return self._ama_period.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def SmoothingPower(self):
        return self._smoothing_power.Value

    @property
    def SignalPipsBarA(self):
        return self._signal_pips_bar_a.Value

    @property
    def SignalPipsBarB(self):
        return self._signal_pips_bar_b.Value

    @property
    def SignalPipsTrade(self):
        return self._signal_pips_trade.Value

    @property
    def EntryPipsDiff(self):
        return self._entry_pips_diff.Value

    @property
    def StopPipsDiff(self):
        return self._stop_pips_diff.Value

    @property
    def StopMaPeriod(self):
        return self._stop_ma_period.Value

    @property
    def StopMaShift(self):
        return self._stop_ma_shift.Value

    @property
    def StopMaMethod(self):
        return self._stop_ma_method.Value

    def OnReseted(self):
        super(var_mov_avg_strategy, self).OnReseted()
        self._vma_closes = []
        self._vma_prev_ama = None
        self._stop_low_ma = None
        self._stop_high_ma = None
        self._low_ma_buffer = []
        self._high_ma_buffer = []
        self._long_state = SIGNAL_NEUTRAL
        self._long_bar_a_ref = 0.0
        self._long_entry_price = 0.0
        self._short_state = SIGNAL_NEUTRAL
        self._short_bar_a_ref = 0.0
        self._short_entry_price = 0.0

    def OnStarted(self, time):
        super(var_mov_avg_strategy, self).OnStarted(time)

        method = self.StopMaMethod
        period = self.StopMaPeriod

        self._stop_low_ma = self._create_ma(method, period)
        self._stop_high_ma = self._create_ma(method, period)
        self._low_ma_buffer = []
        self._high_ma_buffer = []
        self._vma_closes = []
        self._vma_prev_ama = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _create_ma(self, method, period):
        if method == MA_SIMPLE:
            ma = SimpleMovingAverage()
        elif method == MA_SMOOTHED:
            ma = SmoothedMovingAverage()
        elif method == MA_WEIGHTED:
            ma = WeightedMovingAverage()
        else:
            ma = ExponentialMovingAverage()
        ma.Length = period
        return ma

    def _calculate_vma(self, close_price, time):
        """Custom VMA calculation matching the C# VariableMovingAverage."""
        self._vma_closes.append(close_price)
        length = self.AmaPeriod
        required = max(2, length + 1)
        while len(self._vma_closes) > required:
            self._vma_closes.pop(0)

        if self._vma_prev_ama is None:
            self._vma_prev_ama = close_price

        close_count = len(self._vma_closes)
        if close_count < 2:
            return close_price, False

        effective_length = min(length, close_count - 1)
        newest_index = close_count - 1
        base_index = newest_index - effective_length
        if base_index < 0:
            base_index = 0

        newest = self._vma_closes[newest_index]
        oldest = self._vma_closes[base_index]
        signal = abs(newest - oldest)

        noise = 0.000000001
        for i in range(base_index, newest_index):
            noise += abs(self._vma_closes[i + 1] - self._vma_closes[i])

        efficiency = signal / noise if noise != 0 else 0.0
        slow_sc = 2.0 / (float(self.SlowPeriod) + 1.0)
        fast_sc = 2.0 / (float(self.FastPeriod) + 1.0)
        smoothing = slow_sc + efficiency * (fast_sc - slow_sc)
        sp = float(self.SmoothingPower)
        smoothing_factor = pow(smoothing, sp) if smoothing > 0 else 0.0

        ama_prev = self._vma_prev_ama if self._vma_prev_ama is not None else oldest
        ama = ama_prev + smoothing_factor * (close_price - ama_prev)
        self._vma_prev_ama = ama
        is_formed = close_count >= required

        return ama, is_formed

    def _process_stop_ma(self, ma, value, time):
        """Process a decimal value through the stop MA indicator."""
        result = ma.Process(DecimalIndicatorValue(ma, value, time))
        if result.IsEmpty:
            return None
        return float(result.GetValue[float]())

    def _get_shifted_value(self, buffer, value, shift):
        """Get a shifted MA value from the buffer."""
        buffer.append(value)
        max_count = max(1, shift + 1)
        while len(buffer) > max_count:
            buffer.pop(0)
        index = len(buffer) - 1 - min(shift, len(buffer) - 1)
        return buffer[index]

    def _to_price_distance(self, pips):
        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            return float(pips)
        return float(pips) * float(step)

    def _update_long_signal(self, candle, vma, bar_a_offset, bar_b_offset, trade_offset):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)

        if close <= vma - bar_a_offset:
            self._long_state = SIGNAL_NEUTRAL
            self._long_bar_a_ref = 0.0
            self._long_entry_price = 0.0
            return

        if self._long_state == SIGNAL_NEUTRAL:
            if close >= vma + bar_a_offset:
                self._long_state = SIGNAL_BAR_A
                self._long_bar_a_ref = close
        elif self._long_state == SIGNAL_BAR_A:
            if close <= vma - bar_a_offset:
                self._long_state = SIGNAL_NEUTRAL
                self._long_bar_a_ref = 0.0
                self._long_entry_price = 0.0
                return
            if close >= self._long_bar_a_ref + bar_b_offset:
                self._long_state = SIGNAL_BAR_B
                self._long_entry_price = high + trade_offset
        elif self._long_state == SIGNAL_BAR_B:
            if close <= vma - bar_a_offset:
                self._long_state = SIGNAL_NEUTRAL
                self._long_bar_a_ref = 0.0
                self._long_entry_price = 0.0

    def _update_short_signal(self, candle, vma, bar_a_offset, bar_b_offset, trade_offset):
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)

        if close >= vma + bar_a_offset:
            self._short_state = SIGNAL_NEUTRAL
            self._short_bar_a_ref = 0.0
            self._short_entry_price = 0.0
            return

        if self._short_state == SIGNAL_NEUTRAL:
            if close <= vma - bar_a_offset:
                self._short_state = SIGNAL_BAR_A
                self._short_bar_a_ref = close
        elif self._short_state == SIGNAL_BAR_A:
            if close >= vma + bar_a_offset:
                self._short_state = SIGNAL_NEUTRAL
                self._short_bar_a_ref = 0.0
                self._short_entry_price = 0.0
                return
            if close <= self._short_bar_a_ref - bar_b_offset:
                self._short_state = SIGNAL_BAR_B
                self._short_entry_price = low - trade_offset
        elif self._short_state == SIGNAL_BAR_B:
            if close >= vma + bar_a_offset:
                self._short_state = SIGNAL_NEUTRAL
                self._short_bar_a_ref = 0.0
                self._short_entry_price = 0.0

    def _try_long_enter(self, candle, entry_band):
        if self._long_state != SIGNAL_BAR_B:
            return False
        close = float(candle.ClosePrice)
        upper = self._long_entry_price + entry_band
        if close >= self._long_entry_price and close <= upper:
            self._reset_signals()
            return True
        return False

    def _try_short_enter(self, candle, entry_band):
        if self._short_state != SIGNAL_BAR_B:
            return False
        close = float(candle.ClosePrice)
        lower = self._short_entry_price - entry_band
        if close <= self._short_entry_price and close >= lower:
            self._reset_signals()
            return True
        return False

    def _reset_signals(self):
        self._long_state = SIGNAL_NEUTRAL
        self._long_bar_a_ref = 0.0
        self._long_entry_price = 0.0
        self._short_state = SIGNAL_NEUTRAL
        self._short_bar_a_ref = 0.0
        self._short_entry_price = 0.0

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        time = candle.CloseTime

        # Calculate VMA
        vma_value, vma_formed = self._calculate_vma(close, time)
        if not vma_formed:
            return

        # Calculate stop MAs
        low_ma_raw = self._process_stop_ma(self._stop_low_ma, low, time)
        if low_ma_raw is None:
            return
        high_ma_raw = self._process_stop_ma(self._stop_high_ma, high, time)
        if high_ma_raw is None:
            return

        shift = self.StopMaShift
        low_ma = self._get_shifted_value(self._low_ma_buffer, low_ma_raw, shift)
        high_ma = self._get_shifted_value(self._high_ma_buffer, high_ma_raw, shift)

        bar_a_distance = self._to_price_distance(self.SignalPipsBarA)
        bar_b_distance = self._to_price_distance(self.SignalPipsBarB)
        trade_offset = self._to_price_distance(self.SignalPipsTrade)
        entry_band = self._to_price_distance(self.EntryPipsDiff)
        stop_offset = self._to_price_distance(self.StopPipsDiff)

        self._update_long_signal(candle, vma_value, bar_a_distance, bar_b_distance, trade_offset)
        self._update_short_signal(candle, vma_value, bar_a_distance, bar_b_distance, trade_offset)

        if self.Position == 0:
            if self._try_long_enter(candle, entry_band):
                self.BuyMarket()
                self._reset_signals()
            elif self._try_short_enter(candle, entry_band):
                self.SellMarket()
                self._reset_signals()
            return

        if self.Position > 0:
            if self._try_short_enter(candle, entry_band):
                # Reverse: close long and open short
                self.SellMarket()
                self.SellMarket()
                self._reset_signals()
                return

            stop_price = low_ma - stop_offset
            if stop_price > 0 and low <= stop_price:
                self.SellMarket()
                self._reset_signals()
        else:
            if self._try_long_enter(candle, entry_band):
                # Reverse: close short and open long
                self.BuyMarket()
                self.BuyMarket()
                self._reset_signals()
                return

            stop_price = high_ma + stop_offset
            if stop_price > 0 and high >= stop_price:
                self.BuyMarket()
                self._reset_signals()

    def CreateClone(self):
        return var_mov_avg_strategy()
