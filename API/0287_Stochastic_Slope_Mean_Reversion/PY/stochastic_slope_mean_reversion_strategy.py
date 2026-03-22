import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy

class stochastic_slope_mean_reversion_strategy(Strategy):
    """
    Stochastic slope mean reversion strategy.
    Trades reversions from extreme smoothed stochastic slopes and exits when the slope returns to its recent average.
    """

    def __init__(self):
        super(stochastic_slope_mean_reversion_strategy, self).__init__()

        self._stoch_k_period = self.Param("StochKPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %K Period", "Stochastic lookback period", "Stochastic")

        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %D Period", "Smoothing period for stochastic %K", "Stochastic")

        self._slope_lookback = self.Param("SlopeLookback", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Lookback", "Period for slope statistics", "Slope")

        self._threshold_multiplier = self.Param("ThresholdMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Threshold Multiplier", "Std dev multiplier for entry", "Slope")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._long_stoch_level = self.Param("LongStochLevel", 30.0) \
            .SetDisplay("Long Stoch Level", "Maximum stochastic level for long entries", "Signal Filters")

        self._short_stoch_level = self.Param("ShortStochLevel", 70.0) \
            .SetDisplay("Short Stoch Level", "Minimum stochastic level for short entries", "Signal Filters")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._highs = None
        self._lows = None
        self._price_index = 0
        self._price_filled = 0
        self._k_values = None
        self._k_index = 0
        self._k_filled = 0
        self._previous_stoch_k = 0.0
        self._slope_history = None
        self._slope_index = 0
        self._slope_filled = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_slope_mean_reversion_strategy, self).OnReseted()
        kp = int(self._stoch_k_period.Value)
        dp = int(self._stoch_d_period.Value)
        lb = int(self._slope_lookback.Value)
        self._highs = [0.0] * kp
        self._lows = [0.0] * kp
        self._price_index = 0
        self._price_filled = 0
        self._k_values = [0.0] * dp
        self._k_index = 0
        self._k_filled = 0
        self._previous_stoch_k = 0.0
        self._slope_history = [0.0] * lb
        self._slope_index = 0
        self._slope_filled = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(stochastic_slope_mean_reversion_strategy, self).OnStarted(time)

        kp = int(self._stoch_k_period.Value)
        dp = int(self._stoch_d_period.Value)
        lb = int(self._slope_lookback.Value)
        self._highs = [0.0] * kp
        self._lows = [0.0] * kp
        self._k_values = [0.0] * dp
        self._slope_history = [0.0] * lb
        self._price_index = 0
        self._price_filled = 0
        self._k_index = 0
        self._k_filled = 0
        self._slope_index = 0
        self._slope_filled = 0
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

        kp = int(self._stoch_k_period.Value)
        dp = int(self._stoch_d_period.Value)
        lb = int(self._slope_lookback.Value)

        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        close_price = float(candle.ClosePrice)

        self._highs[self._price_index] = high_price
        self._lows[self._price_index] = low_price
        self._price_index = (self._price_index + 1) % kp

        if self._price_filled < kp:
            self._price_filled += 1

        if self._price_filled < kp:
            return

        highest = -1e18
        lowest = 1e18
        for i in range(kp):
            if self._highs[i] > highest:
                highest = self._highs[i]
            if self._lows[i] < lowest:
                lowest = self._lows[i]

        rng = highest - lowest
        if rng <= 0:
            return

        raw_k = (close_price - lowest) / rng * 100.0

        self._k_values[self._k_index] = raw_k
        self._k_index = (self._k_index + 1) % dp

        if self._k_filled < dp:
            self._k_filled += 1

        if self._k_filled < dp:
            return

        stoch_k = 0.0
        for i in range(dp):
            stoch_k += self._k_values[i]
        stoch_k /= float(dp)

        if not self._is_initialized:
            self._previous_stoch_k = stoch_k
            self._is_initialized = True
            return

        slope = stoch_k - self._previous_stoch_k
        self._previous_stoch_k = stoch_k

        self._slope_history[self._slope_index] = slope
        self._slope_index = (self._slope_index + 1) % lb

        if self._slope_filled < lb:
            self._slope_filled += 1

        if self._slope_filled < lb:
            return

        avg_slope = 0.0
        for i in range(lb):
            avg_slope += self._slope_history[i]
        avg_slope /= float(lb)

        sum_sq = 0.0
        for i in range(lb):
            diff = self._slope_history[i] - avg_slope
            sum_sq += diff * diff
        std_dev = math.sqrt(sum_sq / float(lb))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        tm = float(self._threshold_multiplier.Value)
        lower_threshold = avg_slope - tm * std_dev
        upper_threshold = avg_slope + tm * std_dev
        long_level = float(self._long_stoch_level.Value)
        short_level = float(self._short_stoch_level.Value)

        if self.Position == 0:
            if slope < lower_threshold and stoch_k <= long_level:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif slope > upper_threshold and stoch_k >= short_level:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and slope >= avg_slope:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and slope <= avg_slope:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return stochastic_slope_mean_reversion_strategy()
