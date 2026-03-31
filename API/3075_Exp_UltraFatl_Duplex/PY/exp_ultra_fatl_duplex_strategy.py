import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage,
    SimpleMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
    JurikMovingAverage,
    KaufmanAdaptiveMovingAverage,
    DecimalIndicatorValue,
)
from StockSharp.Algo.Strategies import Strategy


class exp_ultra_fatl_duplex_strategy(Strategy):

    _FATL_COEFFICIENTS = [
        0.4360409450, 0.3658689069, 0.2460452079, 0.1104506886,
        -0.0054034585, -0.0760367731, -0.0933058722, -0.0670110374,
        -0.0190795053, 0.0259609206, 0.0502044896, 0.0477818607,
        0.0249252327, -0.0047706151, -0.0272432537, -0.0338917071,
        -0.0244141482, -0.0055774838, 0.0128149838, 0.0226522218,
        0.0208778257, 0.0100299086, -0.0036771622, -0.0136744850,
        -0.0160483392, -0.0108597376, -0.0016060704, 0.0069480557,
        0.0110573605, 0.0095711419, 0.0040444064, -0.0023824623,
        -0.0067093714, -0.0072003400, -0.0047717710, 0.0005541115,
        0.0007860160, 0.0130129076, 0.0040364019,
    ]

    def __init__(self):
        super(exp_ultra_fatl_duplex_strategy, self).__init__()

        self._long_volume = self.Param("LongVolume", 1.0) \
            .SetDisplay("Long Volume", "Order volume for long entries", "Long")
        self._allow_long_entries = self.Param("AllowLongEntries", True) \
            .SetDisplay("Allow Long Entries", "Enable opening long positions", "Long")
        self._allow_long_exits = self.Param("AllowLongExits", True) \
            .SetDisplay("Allow Long Exits", "Enable closing long positions on opposite signals", "Long")
        self._long_candle_type = self.Param("LongCandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Long Candle Type", "Timeframe used by the long UltraFATL block", "Long")
        self._long_start_length = self.Param("LongStartLength", 3) \
            .SetDisplay("Long Start Length", "Initial smoothing length for the ladder", "Long")
        self._long_step = self.Param("LongStep", 1) \
            .SetDisplay("Long Step", "Increment between ladder lengths", "Long")
        self._long_steps_total = self.Param("LongStepsTotal", 3) \
            .SetDisplay("Long Steps", "Number of smoothing steps for the ladder", "Long")
        self._long_smooth_length = self.Param("LongSmoothLength", 3) \
            .SetDisplay("Long Counter Length", "Length used when smoothing the counters", "Long")
        self._long_signal_bar = self.Param("LongSignalBar", 1) \
            .SetDisplay("Long Signal Bar", "Closed-bar offset used when evaluating long signals", "Long")
        self._long_stop_loss_points = self.Param("LongStopLossPoints", 1000) \
            .SetDisplay("Long Stop pts", "Protective stop distance in price steps for long trades", "Long")
        self._long_take_profit_points = self.Param("LongTakeProfitPoints", 2000) \
            .SetDisplay("Long Target pts", "Take-profit distance in price steps for long trades", "Long")

        self._short_volume = self.Param("ShortVolume", 1.0) \
            .SetDisplay("Short Volume", "Order volume for short entries", "Short")
        self._allow_short_entries = self.Param("AllowShortEntries", True) \
            .SetDisplay("Allow Short Entries", "Enable opening short positions", "Short")
        self._allow_short_exits = self.Param("AllowShortExits", True) \
            .SetDisplay("Allow Short Exits", "Enable closing short positions on opposite signals", "Short")
        self._short_candle_type = self.Param("ShortCandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Short Candle Type", "Timeframe used by the short UltraFATL block", "Short")
        self._short_start_length = self.Param("ShortStartLength", 3) \
            .SetDisplay("Short Start Length", "Initial smoothing length for the short ladder", "Short")
        self._short_step = self.Param("ShortStep", 1) \
            .SetDisplay("Short Step", "Increment between smoothing lengths for the short ladder", "Short")
        self._short_steps_total = self.Param("ShortStepsTotal", 3) \
            .SetDisplay("Short Steps", "Number of smoothing steps for the short ladder", "Short")
        self._short_smooth_length = self.Param("ShortSmoothLength", 3) \
            .SetDisplay("Short Counter Length", "Length used when smoothing the short counters", "Short")
        self._short_signal_bar = self.Param("ShortSignalBar", 1) \
            .SetDisplay("Short Signal Bar", "Closed-bar offset used when evaluating short signals", "Short")
        self._short_stop_loss_points = self.Param("ShortStopLossPoints", 1000) \
            .SetDisplay("Short Stop pts", "Protective stop distance in price steps for short trades", "Short")
        self._short_take_profit_points = self.Param("ShortTakeProfitPoints", 2000) \
            .SetDisplay("Short Target pts", "Take-profit distance in price steps for short trades", "Short")

        self._long_entry_price = None
        self._short_entry_price = None
        self._price_step = 0.0

        # Long context state
        self._long_ladder = []
        self._long_prev_values = []
        self._long_bulls_smoother = None
        self._long_bears_smoother = None
        self._long_history = []
        self._long_fatl_buffer = []
        self._long_fatl_filled = 0

        # Short context state
        self._short_ladder = []
        self._short_prev_values = []
        self._short_bulls_smoother = None
        self._short_bears_smoother = None
        self._short_history = []
        self._short_fatl_buffer = []
        self._short_fatl_filled = 0

    @property
    def long_volume(self):
        return self._long_volume.Value

    @property
    def allow_long_entries(self):
        return self._allow_long_entries.Value

    @property
    def allow_long_exits(self):
        return self._allow_long_exits.Value

    @property
    def long_candle_type(self):
        return self._long_candle_type.Value

    @property
    def long_start_length(self):
        return self._long_start_length.Value

    @property
    def long_step(self):
        return self._long_step.Value

    @property
    def long_steps_total(self):
        return self._long_steps_total.Value

    @property
    def long_smooth_length(self):
        return self._long_smooth_length.Value

    @property
    def long_signal_bar(self):
        return self._long_signal_bar.Value

    @property
    def long_stop_loss_points(self):
        return self._long_stop_loss_points.Value

    @property
    def long_take_profit_points(self):
        return self._long_take_profit_points.Value

    @property
    def short_volume(self):
        return self._short_volume.Value

    @property
    def allow_short_entries(self):
        return self._allow_short_entries.Value

    @property
    def allow_short_exits(self):
        return self._allow_short_exits.Value

    @property
    def short_candle_type(self):
        return self._short_candle_type.Value

    @property
    def short_start_length(self):
        return self._short_start_length.Value

    @property
    def short_step(self):
        return self._short_step.Value

    @property
    def short_steps_total(self):
        return self._short_steps_total.Value

    @property
    def short_smooth_length(self):
        return self._short_smooth_length.Value

    @property
    def short_signal_bar(self):
        return self._short_signal_bar.Value

    @property
    def short_stop_loss_points(self):
        return self._short_stop_loss_points.Value

    @property
    def short_take_profit_points(self):
        return self._short_take_profit_points.Value

    def OnReseted(self):
        super(exp_ultra_fatl_duplex_strategy, self).OnReseted()
        self._long_entry_price = None
        self._short_entry_price = None
        self._price_step = 0.0
        self._long_ladder = []
        self._long_prev_values = []
        self._long_bulls_smoother = None
        self._long_bears_smoother = None
        self._long_history = []
        self._long_fatl_buffer = []
        self._long_fatl_filled = 0
        self._short_ladder = []
        self._short_prev_values = []
        self._short_bulls_smoother = None
        self._short_bears_smoother = None
        self._short_history = []
        self._short_fatl_buffer = []
        self._short_fatl_filled = 0

    def OnStarted2(self, time):
        super(exp_ultra_fatl_duplex_strategy, self).OnStarted2(time)

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0

        # Build long context ladder
        self._long_ladder = []
        self._long_prev_values = []
        self._long_fatl_buffer = [0.0] * len(self._FATL_COEFFICIENTS)
        self._long_fatl_filled = 0
        self._long_history = []
        for i in range(self.long_steps_total + 1):
            length = max(1, self.long_start_length + i * self.long_step)
            ind = ExponentialMovingAverage()
            ind.Length = length
            self._long_ladder.append(ind)
            self._long_prev_values.append(None)
        counter_len = max(1, self.long_smooth_length)
        self._long_bulls_smoother = ExponentialMovingAverage()
        self._long_bulls_smoother.Length = counter_len
        self._long_bears_smoother = ExponentialMovingAverage()
        self._long_bears_smoother.Length = counter_len

        # Build short context ladder
        self._short_ladder = []
        self._short_prev_values = []
        self._short_fatl_buffer = [0.0] * len(self._FATL_COEFFICIENTS)
        self._short_fatl_filled = 0
        self._short_history = []
        for i in range(self.short_steps_total + 1):
            length = max(1, self.short_start_length + i * self.short_step)
            ind = ExponentialMovingAverage()
            ind.Length = length
            self._short_ladder.append(ind)
            self._short_prev_values.append(None)
        counter_len = max(1, self.short_smooth_length)
        self._short_bulls_smoother = ExponentialMovingAverage()
        self._short_bulls_smoother.Length = counter_len
        self._short_bears_smoother = ExponentialMovingAverage()
        self._short_bears_smoother.Length = counter_len

        # Subscribe to candles for long context
        sub_long = self.SubscribeCandles(self.long_candle_type)
        sub_long.Bind(self._process_long_candle).Start()

        # Subscribe to candles for short context (may be same timeframe)
        sub_short = self.SubscribeCandles(self.short_candle_type)
        sub_short.Bind(self._process_short_candle).Start()

    def _fatl_process(self, buffer, filled_count, value):
        buf_len = len(self._FATL_COEFFICIENTS)
        for i in range(buf_len - 1, 0, -1):
            buffer[i] = buffer[i - 1]
        buffer[0] = value
        new_filled = min(filled_count + 1, buf_len)
        if new_filled < buf_len:
            return None, new_filled
        total = 0.0
        for i in range(buf_len):
            total += self._FATL_COEFFICIENTS[i] * buffer[i]
        return total, new_filled

    def _process_context_candle(self, candle, is_long):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        price_step = self._price_step

        if is_long:
            ladder = self._long_ladder
            prev_values = self._long_prev_values
            bulls_sm = self._long_bulls_smoother
            bears_sm = self._long_bears_smoother
            history = self._long_history
            signal_bar = self.long_signal_bar
            sl_pts = self.long_stop_loss_points
            tp_pts = self.long_take_profit_points
            vol = float(self.long_volume)
            allow_entries = self.allow_long_entries
            allow_exits = self.allow_long_exits
        else:
            ladder = self._short_ladder
            prev_values = self._short_prev_values
            bulls_sm = self._short_bulls_smoother
            bears_sm = self._short_bears_smoother
            history = self._short_history
            signal_bar = self.short_signal_bar
            sl_pts = self.short_stop_loss_points
            tp_pts = self.short_take_profit_points
            vol = float(self.short_volume)
            allow_entries = self.allow_short_entries
            allow_exits = self.allow_short_exits

        # Check stops first
        self._check_stops(is_long, candle, sl_pts, tp_pts, price_step)

        if vol <= 0 and not allow_exits:
            return

        # FATL filter
        if is_long:
            fatl_val, self._long_fatl_filled = self._fatl_process(
                self._long_fatl_buffer, self._long_fatl_filled, close)
        else:
            fatl_val, self._short_fatl_filled = self._fatl_process(
                self._short_fatl_buffer, self._short_fatl_filled, close)

        if fatl_val is None:
            return

        # Process ladder
        up_count = 0.0
        down_count = 0.0

        for i in range(len(ladder)):
            ind = ladder[i]
            ind_value = ind.Process(
                DecimalIndicatorValue(ind, fatl_val, candle.OpenTime))
            if not ind_value.IsFinal:
                return

            cur_val = float(ind_value)

            if prev_values[i] is None:
                prev_values[i] = cur_val
                return

            prev_val = prev_values[i]
            if cur_val > prev_val:
                up_count += 1.0
            else:
                down_count += 1.0
            prev_values[i] = cur_val

        if bulls_sm is None or bears_sm is None:
            return

        bulls_value = bulls_sm.Process(
            DecimalIndicatorValue(bulls_sm, up_count, candle.OpenTime))
        bears_value = bears_sm.Process(
            DecimalIndicatorValue(bears_sm, down_count, candle.OpenTime))

        if not bulls_value.IsFinal or not bears_value.IsFinal:
            return

        bulls = float(bulls_value)
        bears = float(bears_value)

        history.append((bulls, bears, close, high, low))
        max_hist = max(10, max(1, signal_bar) + 5)
        if len(history) > max_hist:
            history[:] = history[-max_hist:]

        effective_shift = max(1, signal_bar)
        if len(history) <= effective_shift:
            return

        current_index = len(history) - effective_shift
        previous_index = current_index - 1
        if previous_index < 0 or current_index >= len(history):
            return

        cur_bulls, cur_bears, cur_close, _, _ = history[current_index]
        prev_bulls, prev_bears, _, _, _ = history[previous_index]

        bullish_bias = cur_bulls > cur_bears
        bearish_bias = cur_bears > cur_bulls

        if is_long:
            open_signal = bullish_bias and prev_bulls <= prev_bears
            close_signal = bearish_bias
        else:
            open_signal = bearish_bias and prev_bulls >= prev_bears
            close_signal = bullish_bias

        if not open_signal and not close_signal:
            return

        if not allow_entries:
            open_signal = False
        if not allow_exits:
            close_signal = False

        self._process_directional_signal(is_long, open_signal, close_signal, cur_close, vol)

    def _process_long_candle(self, candle):
        self._process_context_candle(candle, True)

    def _process_short_candle(self, candle):
        self._process_context_candle(candle, False)

    def _process_directional_signal(self, is_long, open_signal, close_signal, close_price, volume):
        if is_long:
            if close_signal and self.allow_long_exits and self.Position > 0:
                self.SellMarket(self.Position)
                self._long_entry_price = None

            if open_signal and self.allow_long_entries and self.Position <= 0 and volume > 0:
                buy_vol = volume + (-self.Position if self.Position < 0 else 0)
                self.BuyMarket(buy_vol)
                self._long_entry_price = close_price
        else:
            if close_signal and self.allow_short_exits and self.Position < 0:
                self.BuyMarket(-self.Position)
                self._short_entry_price = None

            if open_signal and self.allow_short_entries and self.Position >= 0 and volume > 0:
                sell_vol = volume + (self.Position if self.Position > 0 else 0)
                self.SellMarket(sell_vol)
                self._short_entry_price = close_price

    def _check_stops(self, is_long, candle, stop_loss_points, take_profit_points, price_step):
        if price_step <= 0:
            return

        if is_long:
            if self.Position <= 0 or self._long_entry_price is None:
                return
            entry = self._long_entry_price
            sl_price = entry - stop_loss_points * price_step if stop_loss_points > 0 else None
            tp_price = entry + take_profit_points * price_step if take_profit_points > 0 else None
            if sl_price is not None and float(candle.LowPrice) <= sl_price:
                self.SellMarket()
                self._long_entry_price = None
                return
            if tp_price is not None and float(candle.HighPrice) >= tp_price:
                self.SellMarket()
                self._long_entry_price = None
        else:
            if self.Position >= 0 or self._short_entry_price is None:
                return
            entry = self._short_entry_price
            sl_price = entry + stop_loss_points * price_step if stop_loss_points > 0 else None
            tp_price = entry - take_profit_points * price_step if take_profit_points > 0 else None
            if sl_price is not None and float(candle.HighPrice) >= sl_price:
                self.BuyMarket()
                self._short_entry_price = None
                return
            if tp_price is not None and float(candle.LowPrice) <= tp_price:
                self.BuyMarket()
                self._short_entry_price = None

    def CreateClone(self):
        return exp_ultra_fatl_duplex_strategy()
