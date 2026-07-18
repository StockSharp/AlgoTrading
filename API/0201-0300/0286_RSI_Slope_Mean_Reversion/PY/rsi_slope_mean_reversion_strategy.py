import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_slope_mean_reversion_strategy(Strategy):
    """
    RSI slope mean reversion strategy.
    Trades reversions from extreme RSI slopes and exits when the slope returns to its recent average.
    """

    def __init__(self):
        super(rsi_slope_mean_reversion_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Relative Strength Index period", "RSI Settings")

        self._slope_lookback = self.Param("SlopeLookback", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Lookback", "Period for slope statistics", "Slope Settings")

        self._threshold_multiplier = self.Param("ThresholdMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entry threshold", "Slope Settings")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._long_rsi_level = self.Param("LongRsiLevel", 40.0) \
            .SetDisplay("Long RSI Level", "Maximum RSI level for long entries", "Signal Filters")

        self._short_rsi_level = self.Param("ShortRsiLevel", 60.0) \
            .SetDisplay("Short RSI Level", "Minimum RSI level for short entries", "Signal Filters")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._rsi = None
        self._previous_rsi_value = 0.0
        self._slope_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_slope_mean_reversion_strategy, self).OnReseted()
        self._rsi = None
        self._previous_rsi_value = 0.0
        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(rsi_slope_mean_reversion_strategy, self).OnStarted2(time)

        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed:
            return

        rv = float(rsi_value)

        if not self._is_initialized:
            self._previous_rsi_value = rv
            self._is_initialized = True
            return

        slope = rv - self._previous_rsi_value
        self._previous_rsi_value = rv

        lb = int(self._slope_lookback.Value)
        self._slope_history[self._current_index] = slope
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

        tm = float(self._threshold_multiplier.Value)
        lower_threshold = avg_slope - tm * std_slope
        upper_threshold = avg_slope + tm * std_slope
        long_rsi = float(self._long_rsi_level.Value)
        short_rsi = float(self._short_rsi_level.Value)

        if self.Position == 0:
            if slope < lower_threshold and rv <= long_rsi:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif slope > upper_threshold and rv >= short_rsi:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and slope >= avg_slope:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and slope <= avg_slope:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return rsi_slope_mean_reversion_strategy()
