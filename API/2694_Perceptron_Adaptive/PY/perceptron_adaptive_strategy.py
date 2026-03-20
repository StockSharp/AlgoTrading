import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    RelativeStrengthIndex,
    CommodityChannelIndex,
    AwesomeOscillator,
)


class perceptron_adaptive_strategy(Strategy):
    """Adaptive multi-layer perceptron: combines 5 indicator signals, tunes weights after trades."""

    # Neuron-to-indicator mapping (each neuron uses 4 of 5 indicators, indices 1-5)
    _NEURON_INDICATORS = [
        [2, 3, 4, 5],
        [1, 3, 4, 5],
        [1, 2, 4, 5],
        [1, 2, 3, 5],
        [1, 2, 3, 4],
    ]

    def __init__(self):
        super(perceptron_adaptive_strategy, self).__init__()

        self._stop_loss_offset = self.Param("StopLossOffset", 500.0) \
            .SetDisplay("Stop Loss Offset", "Stop-loss distance in absolute price units", "Risk Management")
        self._take_profit_offset = self.Param("TakeProfitOffset", 300.0) \
            .SetDisplay("Take Profit Offset", "Take-profit distance in absolute price units", "Risk Management")
        self._sin_max = self.Param("SinMax", 5) \
            .SetDisplay("Synapse Upper Bound", "Maximum value for neuron bias weights", "Neural Network")
        self._sin_min = self.Param("SinMin", 0) \
            .SetDisplay("Synapse Lower Bound", "Minimum value for neuron bias weights", "Neural Network")
        self._sin_plus = self.Param("SinPlusStep", 0.03) \
            .SetGreaterThanZero() \
            .SetDisplay("Positive Adjustment", "Increment applied when trade is favorable", "Neural Network")
        self._sin_minus = self.Param("SinMinusStep", 0.03) \
            .SetGreaterThanZero() \
            .SetDisplay("Negative Adjustment", "Decrement applied when trade is unfavorable", "Neural Network")
        self._fast_ma_length = self.Param("FastMaLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA Length", "Fast simple moving average length", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA Length", "Slow simple moving average length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "Relative Strength Index period", "Indicators")
        self._cci_length = self.Param("CciLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Length", "Commodity Channel Index period", "Indicators")
        self._slope_ma_length = self.Param("SlopeMaLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope MA Length", "SMA for slope detection", "Indicators")
        self._ao_short_length = self.Param("AoShortLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("AO Short Length", "Short period for Awesome Oscillator", "Indicators")
        self._ao_long_length = self.Param("AoLongLength", 34) \
            .SetGreaterThanZero() \
            .SetDisplay("AO Long Length", "Long period for Awesome Oscillator", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")

        self._base_weights = [1.0] * 5
        # indicator_weights[neuron][indicator_index 0..5]
        self._indicator_weights = [[0.0] * 6 for _ in range(5)]
        self._last_indicator_signals = [0] * 5
        self._last_neuron_outputs = [0.0] * 5

        self._prev_fast_ma = None
        self._prev_prev_fast_ma = None
        self._prev_slow_ma = None
        self._prev_rsi = None
        self._prev_prev_rsi = None
        self._prev_cci = None
        self._prev_prev_cci = None
        self._prev_slope_ma = None
        self._prev_prev_slope_ma = None
        self._prev_ao = None

        self._has_last_signals = False
        self._last_trade_direction = 0
        self._entry_price = 0.0
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._is_long_position = False
        self._entry_candle_time = None

    @property
    def StopLossOffset(self):
        return float(self._stop_loss_offset.Value)
    @property
    def TakeProfitOffset(self):
        return float(self._take_profit_offset.Value)
    @property
    def SinMax(self):
        return int(self._sin_max.Value)
    @property
    def SinMin(self):
        return int(self._sin_min.Value)
    @property
    def SinPlusStep(self):
        return float(self._sin_plus.Value)
    @property
    def SinMinusStep(self):
        return float(self._sin_minus.Value)
    @property
    def FastMaLength(self):
        return int(self._fast_ma_length.Value)
    @property
    def SlowMaLength(self):
        return int(self._slow_ma_length.Value)
    @property
    def RsiLength(self):
        return int(self._rsi_length.Value)
    @property
    def CciLength(self):
        return int(self._cci_length.Value)
    @property
    def SlopeMaLength(self):
        return int(self._slope_ma_length.Value)
    @property
    def AoShortLength(self):
        return int(self._ao_short_length.Value)
    @property
    def AoLongLength(self):
        return int(self._ao_long_length.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._base_weights = [1.0] * 5
        self._indicator_weights = [[0.0] * 6 for _ in range(5)]
        for i in range(5):
            for idx in self._NEURON_INDICATORS[i]:
                self._indicator_weights[i][idx] = 1.0
        self._last_indicator_signals = [0] * 5
        self._last_neuron_outputs = [0.0] * 5
        self._prev_fast_ma = None
        self._prev_prev_fast_ma = None
        self._prev_slow_ma = None
        self._prev_rsi = None
        self._prev_prev_rsi = None
        self._prev_cci = None
        self._prev_prev_cci = None
        self._prev_slope_ma = None
        self._prev_prev_slope_ma = None
        self._prev_ao = None
        self._has_last_signals = False
        self._last_trade_direction = 0
        self._entry_price = 0.0
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._is_long_position = False
        self._entry_candle_time = None

    def OnStarted(self, time):
        super(perceptron_adaptive_strategy, self).OnStarted(time)
        self._reset_state()

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self.FastMaLength
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.SlowMaLength
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciLength
        self._slope_ma = SimpleMovingAverage()
        self._slope_ma.Length = self.SlopeMaLength
        self._ao = AwesomeOscillator()
        self._ao.ShortMa.Length = self.AoShortLength
        self._ao.LongMa.Length = self.AoLongLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ma, self._slow_ma, self._rsi, self._cci, self._slope_ma, self._ao, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_ma_val, slow_ma_val, rsi_val, cci_val, slope_ma_val, ao_val):
        if candle.State != CandleStates.Finished:
            return

        fast_ma_v = float(fast_ma_val)
        slow_ma_v = float(slow_ma_val)
        rsi_v = float(rsi_val)
        cci_v = float(cci_val)
        slope_v = float(slope_ma_val)
        ao_v = float(ao_val)

        ma_signal = self._update_ma_signal(fast_ma_v, slow_ma_v)
        rsi_signal = self._update_rsi_signal(rsi_v)
        cci_signal = self._update_cci_signal(cci_v)
        slope_signal = self._update_slope_signal(slope_v)
        ao_signal = self._update_ao_signal(ao_v)

        self._handle_position_management(candle)

        if (not self._fast_ma.IsFormed or not self._slow_ma.IsFormed
                or not self._rsi.IsFormed or not self._cci.IsFormed):
            return

        if self.Position != 0:
            return

        indicator_signals = [ma_signal, rsi_signal, cci_signal, slope_signal, ao_signal]
        neuron_outputs = self._calculate_neuron_outputs(indicator_signals)
        brain_return = self._calculate_brain_return(neuron_outputs)

        if brain_return > 0 and self._last_trade_direction != 2:
            self._open_position(True, float(candle.ClosePrice), candle.OpenTime, indicator_signals, neuron_outputs)
        elif brain_return < 0 and self._last_trade_direction != 1:
            self._open_position(False, float(candle.ClosePrice), candle.OpenTime, indicator_signals, neuron_outputs)

    def _open_position(self, is_long, entry_price, candle_time, indicator_signals, neuron_outputs):
        if is_long:
            self.BuyMarket()
            self._last_trade_direction = 2
        else:
            self.SellMarket()
            self._last_trade_direction = 1

        self._entry_price = entry_price
        self._is_long_position = is_long
        self._entry_candle_time = candle_time

        stop_offset = self.StopLossOffset
        take_offset = self.TakeProfitOffset

        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

        if stop_offset > 0:
            self._stop_loss_price = entry_price - stop_offset if is_long else entry_price + stop_offset
        if take_offset > 0:
            self._take_profit_price = entry_price + take_offset if is_long else entry_price - take_offset

        self._has_last_signals = True
        for i in range(len(indicator_signals)):
            self._last_indicator_signals[i] = indicator_signals[i]
        for i in range(len(neuron_outputs)):
            self._last_neuron_outputs[i] = neuron_outputs[i]

    def _handle_position_management(self, candle):
        if self.Position == 0 or self._entry_candle_time is None:
            return
        if candle.OpenTime <= self._entry_candle_time:
            return

        has_exit = False
        exit_price = 0.0
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self._is_long_position:
            if self._take_profit_price > 0 and h >= self._take_profit_price:
                exit_price = self._take_profit_price
                has_exit = True
            elif self._stop_loss_price > 0 and lo <= self._stop_loss_price:
                exit_price = self._stop_loss_price
                has_exit = True
        else:
            if self._take_profit_price > 0 and lo <= self._take_profit_price:
                exit_price = self._take_profit_price
                has_exit = True
            elif self._stop_loss_price > 0 and h >= self._stop_loss_price:
                exit_price = self._stop_loss_price
                has_exit = True

        if not has_exit:
            return

        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        profit = exit_price - self._entry_price if self._is_long_position else self._entry_price - exit_price

        if self._has_last_signals:
            self._adjust_weights(self._is_long_position, profit)

        self._reset_after_exit()

    def _adjust_weights(self, was_long, profit):
        if profit > 0:
            outcome_sign = 1
        elif profit < 0:
            outcome_sign = -1
        else:
            return

        direction_sign = -1 if was_long else 1
        sin_plus = self.SinPlusStep
        sin_minus = self.SinMinusStep
        sin_max = float(self.SinMax)
        sin_min = float(self.SinMin)

        for ni in range(5):
            last_output = self._last_neuron_outputs[ni]
            if last_output > 0:
                neuron_sign = 1
            elif last_output < 0:
                neuron_sign = -1
            else:
                neuron_sign = 0

            if neuron_sign != 0:
                product = neuron_sign * direction_sign
                if product > 0:
                    if outcome_sign > 0:
                        self._base_weights[ni] = min(self._base_weights[ni] + sin_plus, sin_max)
                    else:
                        self._base_weights[ni] = max(self._base_weights[ni] - sin_minus, sin_min)
                elif product < 0:
                    if outcome_sign > 0:
                        self._base_weights[ni] = max(self._base_weights[ni] - sin_minus, sin_min)
                    else:
                        self._base_weights[ni] = min(self._base_weights[ni] + sin_plus, sin_max)

            for ind_idx in self._NEURON_INDICATORS[ni]:
                ind_signal = self._last_indicator_signals[ind_idx - 1]
                if ind_signal == 0:
                    continue
                product = ind_signal * direction_sign
                if product > 0:
                    self._indicator_weights[ni][ind_idx] += sin_plus if outcome_sign > 0 else -sin_minus
                elif product < 0:
                    self._indicator_weights[ni][ind_idx] += -sin_minus if outcome_sign > 0 else sin_plus

    def _calculate_neuron_outputs(self, indicator_signals):
        outputs = [0.0] * 5
        for ni in range(5):
            s = 0.0
            for ind_idx in self._NEURON_INDICATORS[ni]:
                sig = indicator_signals[ind_idx - 1]
                if sig == 0:
                    continue
                w = self._indicator_weights[ni][ind_idx]
                s += w * sig
            outputs[ni] = s
        return outputs

    def _calculate_brain_return(self, neuron_outputs):
        total = 0.0
        for i in range(len(neuron_outputs)):
            total += neuron_outputs[i] * self._base_weights[i]
        return total

    def _update_ma_signal(self, fast_val, slow_val):
        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            self._prev_prev_fast_ma = self._prev_fast_ma
            self._prev_fast_ma = fast_val
            self._prev_slow_ma = slow_val
            return 0
        if self._prev_fast_ma is None or self._prev_prev_fast_ma is None or self._prev_slow_ma is None:
            self._prev_prev_fast_ma = self._prev_fast_ma
            self._prev_fast_ma = fast_val
            self._prev_slow_ma = slow_val
            return 0
        prev_f = self._prev_fast_ma
        prev_f2 = self._prev_prev_fast_ma
        prev_s = self._prev_slow_ma
        signal = 0
        if prev_f2 < prev_s and prev_f > prev_s:
            signal = 1
        elif prev_f2 > prev_s and prev_f < prev_s:
            signal = -1
        self._prev_prev_fast_ma = self._prev_fast_ma
        self._prev_fast_ma = fast_val
        self._prev_slow_ma = slow_val
        return signal

    def _update_rsi_signal(self, rsi_val):
        if not self._rsi.IsFormed:
            self._prev_prev_rsi = self._prev_rsi
            self._prev_rsi = rsi_val
            return 0
        if self._prev_rsi is None or self._prev_prev_rsi is None:
            self._prev_prev_rsi = self._prev_rsi
            self._prev_rsi = rsi_val
            return 0
        prev = self._prev_rsi
        prev2 = self._prev_prev_rsi
        signal = 0
        if prev2 < 30 and prev > 30:
            signal = 1
        elif prev2 > 70 and prev < 70:
            signal = -1
        self._prev_prev_rsi = self._prev_rsi
        self._prev_rsi = rsi_val
        return signal

    def _update_cci_signal(self, cci_val):
        if not self._cci.IsFormed:
            self._prev_prev_cci = self._prev_cci
            self._prev_cci = cci_val
            return 0
        if self._prev_cci is None or self._prev_prev_cci is None:
            self._prev_prev_cci = self._prev_cci
            self._prev_cci = cci_val
            return 0
        prev = self._prev_cci
        prev2 = self._prev_prev_cci
        signal = 0
        if prev2 < -100 and prev > -100:
            signal = 1
        elif prev2 > 100 and prev < 100:
            signal = -1
        self._prev_prev_cci = self._prev_cci
        self._prev_cci = cci_val
        return signal

    def _update_slope_signal(self, slope_val):
        if not self._slope_ma.IsFormed:
            self._prev_prev_slope_ma = self._prev_slope_ma
            self._prev_slope_ma = slope_val
            return 0
        if self._prev_slope_ma is None or self._prev_prev_slope_ma is None:
            self._prev_prev_slope_ma = self._prev_slope_ma
            self._prev_slope_ma = slope_val
            return 0
        prev = self._prev_slope_ma
        prev2 = self._prev_prev_slope_ma
        signal = 0
        if prev > prev2:
            signal = 1
        elif prev < prev2:
            signal = -1
        self._prev_prev_slope_ma = self._prev_slope_ma
        self._prev_slope_ma = slope_val
        return signal

    def _update_ao_signal(self, ao_val):
        if not self._ao.IsFormed:
            self._prev_ao = ao_val
            return 0
        if self._prev_ao is None:
            self._prev_ao = ao_val
            return 0
        prev = self._prev_ao
        signal = 0
        if ao_val > prev:
            signal = 1
        elif ao_val < prev:
            signal = -1
        self._prev_ao = ao_val
        return signal

    def _reset_after_exit(self):
        self._entry_price = 0.0
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._is_long_position = False
        self._entry_candle_time = None
        self._last_trade_direction = 0
        self._has_last_signals = False
        self._last_indicator_signals = [0] * 5
        self._last_neuron_outputs = [0.0] * 5

    def OnReseted(self):
        super(perceptron_adaptive_strategy, self).OnReseted()
        self._reset_state()

    def CreateClone(self):
        return perceptron_adaptive_strategy()
