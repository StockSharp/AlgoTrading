import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy

class macd_slope_mean_reversion_strategy(Strategy):
    """
    MACD slope mean reversion strategy.
    Trades reversions from extreme MACD histogram slopes and exits when the slope returns to its recent average.
    """

    def __init__(self):
        super(macd_slope_mean_reversion_strategy, self).__init__()

        self._fast_macd_period = self.Param("FastMacdPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicator Parameters")

        self._slow_macd_period = self.Param("SlowMacdPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicator Parameters")

        self._signal_macd_period = self.Param("SignalMacdPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "Signal line period for MACD", "Indicator Parameters")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy Parameters")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._fast_ema = 0.0
        self._slow_ema = 0.0
        self._signal_ema = 0.0
        self._previous_histogram = 0.0
        self._slope_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_slope_mean_reversion_strategy, self).OnReseted()
        self._fast_ema = 0.0
        self._slow_ema = 0.0
        self._signal_ema = 0.0
        self._previous_histogram = 0.0
        lb = int(self._lookback_period.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(macd_slope_mean_reversion_strategy, self).OnStarted2(time)

        lb = int(self._lookback_period.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close_price = float(candle.ClosePrice)

        if not self._is_initialized:
            self._fast_ema = close_price
            self._slow_ema = close_price
            self._signal_ema = 0.0
            self._previous_histogram = 0.0
            self._is_initialized = True
            return

        fast_period = float(self._fast_macd_period.Value)
        slow_period = float(self._slow_macd_period.Value)
        signal_period = float(self._signal_macd_period.Value)

        fast_alpha = 2.0 / (fast_period + 1.0)
        slow_alpha = 2.0 / (slow_period + 1.0)
        signal_alpha = 2.0 / (signal_period + 1.0)

        self._fast_ema += fast_alpha * (close_price - self._fast_ema)
        self._slow_ema += slow_alpha * (close_price - self._slow_ema)

        macd_line = self._fast_ema - self._slow_ema
        self._signal_ema += signal_alpha * (macd_line - self._signal_ema)
        histogram = macd_line - self._signal_ema
        histogram_slope = histogram - self._previous_histogram
        self._previous_histogram = histogram

        lb = int(self._lookback_period.Value)
        self._slope_history[self._current_index] = histogram_slope
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            return

        avg_slope = 0.0
        for i in range(lb):
            avg_slope += self._slope_history[i]
        avg_slope /= float(lb)

        sum_sq = 0.0
        for i in range(lb):
            diff = self._slope_history[i] - avg_slope
            sum_sq += diff * diff
        std_slope = math.sqrt(sum_sq / float(lb))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        dm = float(self._deviation_multiplier.Value)
        lower_threshold = avg_slope - dm * std_slope
        upper_threshold = avg_slope + dm * std_slope

        if self.Position == 0:
            if histogram_slope < lower_threshold and histogram < 0:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif histogram_slope > upper_threshold and histogram > 0:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and histogram_slope >= avg_slope:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and histogram_slope <= avg_slope:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return macd_slope_mean_reversion_strategy()
