import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy

class burg_extrapolator_strategy(Strategy):
    """
    Strategy that extrapolates future prices using the Burg autoregressive model
    and opens trades when forecasted swings exceed thresholds.
    """

    def __init__(self):
        super(burg_extrapolator_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 5.0) \
            .SetDisplay("Risk %", "Risk percent per trade", "Money")
        self._max_positions = self.Param("MaxPositions", 1) \
            .SetDisplay("Max Positions", "Maximum simultaneous trades", "Risk")
        self._min_profit_pips = self.Param("MinProfitPips", 2.0) \
            .SetDisplay("Min Profit", "Minimum predicted profit (pips)", "Signals")
        self._max_loss_pips = self.Param("MaxLossPips", 5.0) \
            .SetDisplay("Max Loss", "Maximum tolerated loss (pips)", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 0.0) \
            .SetDisplay("Take Profit", "Take profit distance (pips)", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 5.0) \
            .SetDisplay("Stop Loss", "Stop loss distance (pips)", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance (pips)", "Risk")
        self._past_bars = self.Param("PastBars", 50) \
            .SetDisplay("Past Bars", "Bars used for Burg model", "Model")
        self._model_order_fraction = self.Param("ModelOrderFraction", 0.37) \
            .SetDisplay("Model Order", "Fraction of bars used for AR order", "Model")
        self._use_momentum = self.Param("UseMomentum", True) \
            .SetDisplay("Use Momentum", "Use logarithmic momentum input", "Model")
        self._use_rate_of_change = self.Param("UseRateOfChange", False) \
            .SetDisplay("Use ROC", "Use rate of change input when momentum is off", "Model")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetDisplay("Order Volume", "Fallback order volume", "Money")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._open_history = []
        self._pip_size = 0.0
        self._effective_past_bars = 0
        self._model_order = 1
        self._forecast_steps = 1
        self._history_capacity = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(burg_extrapolator_strategy, self).OnReseted()
        self._open_history = []
        self._pip_size = 0.0
        self._effective_past_bars = 0
        self._model_order = 1
        self._forecast_steps = 1
        self._history_capacity = 0

    def OnStarted(self, time):
        super(burg_extrapolator_strategy, self).OnStarted(time)

        self._pip_size = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        decimals = self.Security.Decimals if self.Security.Decimals is not None else 0
        if decimals in (3, 5):
            self._pip_size *= 10.0

        self._ensure_capacity()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

        tp = None
        sl = None
        tp_pips = self._take_profit_pips.Value
        sl_pips = self._stop_loss_pips.Value
        trailing_pips = self._trailing_stop_pips.Value

        if tp_pips > 0:
            tp = Unit(tp_pips * self._pip_size, UnitTypes.Absolute)
        if sl_pips > 0:
            sl = Unit(sl_pips * self._pip_size, UnitTypes.Absolute)

        self.StartProtection(tp, sl, trailing_pips > 0)

    def _ensure_capacity(self):
        bars = max(self._past_bars.Value, 3)
        momentum_enabled = self._use_momentum.Value
        roc_enabled = not momentum_enabled and self._use_rate_of_change.Value
        required = bars + 1 if (momentum_enabled or roc_enabled) else bars

        if self._effective_past_bars != bars:
            self._effective_past_bars = bars
            self._open_history = []
            self._history_capacity = required

        order = int(math.floor(self._model_order_fraction.Value * bars))
        if order < 1:
            order = 1
        if order >= bars:
            order = bars - 1
        if order < 1:
            order = 1

        nf = bars - order - 1
        if nf < 1:
            nf = 1

        self._model_order = order
        self._forecast_steps = nf

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._ensure_capacity()
        self._open_history.append(float(candle.OpenPrice))

        if len(self._open_history) > self._history_capacity:
            self._open_history = self._open_history[-self._history_capacity:]

        if len(self._open_history) < self._history_capacity:
            return

        bars = self._effective_past_bars
        momentum_enabled = self._use_momentum.Value
        roc_enabled = not momentum_enabled and self._use_rate_of_change.Value

        # Build input series
        input_buffer = [0.0] * bars
        average = 0.0

        if momentum_enabled:
            for i in range(bars):
                prev_val = self._open_history[i]
                next_val = self._open_history[i + 1]
                if prev_val <= 0 or next_val <= 0:
                    input_buffer[i] = 0.0
                else:
                    input_buffer[i] = math.log(next_val / prev_val)
        elif roc_enabled:
            for i in range(bars):
                prev_val = self._open_history[i]
                next_val = self._open_history[i + 1]
                if prev_val == 0:
                    input_buffer[i] = 0.0
                else:
                    input_buffer[i] = next_val / prev_val - 1.0
        else:
            for i in range(bars):
                average += self._open_history[i]
            average /= bars
            for i in range(bars):
                input_buffer[i] = self._open_history[i] - average

        # Burg coefficients
        order = self._model_order
        nf = self._forecast_steps
        coefficients = [0.0] * (order + 1)
        forward_errors = list(input_buffer)
        backward_errors = list(input_buffer)

        den = sum(v * v for v in input_buffer) * 2.0
        reflection = 0.0

        for k in range(1, order + 1):
            num = sum(forward_errors[i] * backward_errors[i - 1] for i in range(k, bars))
            left = forward_errors[k - 1]
            right = backward_errors[bars - 1]
            denom = (1.0 - reflection * reflection) * den - left * left - right * right
            reflection = -2.0 * num / denom if abs(denom) > 1e-15 else 0.0

            coefficients[k] = reflection
            half = k // 2
            for i in range(1, half + 1):
                ki = k - i
                temp = coefficients[i]
                coefficients[i] += reflection * coefficients[ki]
                if i != ki:
                    coefficients[ki] += reflection * temp

            if k < order:
                for i in range(bars - 1, k - 1, -1):
                    temp = forward_errors[i]
                    forward_errors[i] += reflection * backward_errors[i - 1]
                    backward_errors[i] = backward_errors[i - 1] + reflection * temp

        # Forecast
        predictions = [0.0] * (nf + 1)
        for n in range(bars - 1, bars + nf):
            s = 0.0
            for i in range(1, order + 1):
                idx = n - i
                if idx < bars:
                    s -= coefficients[i] * input_buffer[idx]
                else:
                    pf_idx = idx - bars + 1
                    if 0 <= pf_idx < len(predictions):
                        s -= coefficients[i] * predictions[pf_idx]
            target = n - bars + 1
            if 0 <= target < len(predictions):
                predictions[target] = s

        # Convert to price forecast
        current_open = self._open_history[-1]
        price_forecast = [0.0] * (nf + 1)

        if momentum_enabled:
            price_forecast[0] = current_open
            for i in range(1, nf + 1):
                price_forecast[i] = price_forecast[i - 1] * math.exp(predictions[i])
        elif roc_enabled:
            price_forecast[0] = current_open
            for i in range(1, nf + 1):
                price_forecast[i] = price_forecast[i - 1] * (1.0 + predictions[i])
        else:
            for i in range(nf + 1):
                price_forecast[i] = predictions[i] + average

        # Evaluate signals
        min_profit = self._min_profit_pips.Value * self._pip_size
        max_loss = self._max_loss_pips.Value * self._pip_size
        ymax = price_forecast[0]
        ymin = price_forecast[0]
        imax = 0
        imin = 0
        open_signal = 0
        close_signal = 0

        for i in range(1, nf):
            value = price_forecast[i]
            if value > ymax and open_signal == 0:
                ymax = value
                imax = i
                if imin == 0 and ymax - ymin >= max_loss:
                    close_signal = 1
                if imin == 0 and ymax - ymin >= min_profit:
                    open_signal = 1
            if value < ymin and open_signal == 0:
                ymin = value
                imin = i
                if imax == 0 and ymax - ymin >= max_loss:
                    close_signal = -1
                if imax == 0 and ymax - ymin >= min_profit:
                    open_signal = -1

        # Trade
        has_position = self.Position != 0
        if has_position:
            if self.Position > 0 and (close_signal == -1 or open_signal == -1):
                self.SellMarket()
                return
            if self.Position < 0 and (close_signal == 1 or open_signal == 1):
                self.BuyMarket()
                return

        if open_signal == 0:
            return

        if open_signal > 0 and self.Position < self._max_positions.Value * self._order_volume.Value:
            self.BuyMarket()
        elif open_signal < 0:
            short_exposure = abs(min(self.Position, 0))
            if short_exposure < self._max_positions.Value * self._order_volume.Value:
                self.SellMarket()

    def CreateClone(self):
        return burg_extrapolator_strategy()
