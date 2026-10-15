import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class stochastic_slope_breakout_strategy(Strategy):
    """
    Strategy based on Stochastic %K slope breakout.
    Opens positions when Stochastic slope deviates from its recent average by a multiple of standard deviation.
    """

    def __init__(self):
        super(stochastic_slope_breakout_strategy, self).__init__()

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Indicator Parameters") \
            .SetOptimize(7, 21, 7)

        self._k_period = self.Param("KPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("K Period", "Smoothing period for %K line", "Indicator Parameters") \
            .SetOptimize(1, 5, 1)

        self._d_period = self.Param("DPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("D Period", "Period for %D line", "Indicator Parameters") \
            .SetOptimize(1, 5, 1)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 2400) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stochastic = None
        self._prev_stoch_k = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_slope_breakout_strategy, self).OnReseted()
        self._stochastic = None
        self._prev_stoch_k = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        lb = int(self._lookback_period.Value)
        self._slopes = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(stochastic_slope_breakout_strategy, self).OnStarted2(time)

        lb = int(self._lookback_period.Value)
        self._slopes = [0.0] * lb
        self._cooldown = 0
        self._filled_count = 0
        self._current_index = 0

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = int(self._k_period.Value)
        self._stochastic.D.Length = int(self._d_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._stochastic, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._stochastic.IsFormed:
            return

        k_value = stoch_value.K
        if k_value is None:
            return

        k_val = float(k_value)

        if not self._is_initialized:
            self._prev_stoch_k = k_val
            self._is_initialized = True
            return

        self._current_slope = k_val - self._prev_stoch_k
        self._prev_stoch_k = k_val

        lb = int(self._lookback_period.Value)
        self._slopes[self._current_index] = self._current_slope
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            return

        self._calculate_statistics()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._std_dev_slope <= 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        dm = float(self._deviation_multiplier.Value)
        upper_threshold = self._avg_slope + dm * self._std_dev_slope
        lower_threshold = self._avg_slope - dm * self._std_dev_slope

        if self.Position == 0:
            if self._current_slope > upper_threshold:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif self._current_slope < lower_threshold:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0:
            if self._current_slope <= self._avg_slope:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            if self._current_slope >= self._avg_slope:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)

    def _calculate_statistics(self):
        lb = int(self._lookback_period.Value)
        self._avg_slope = 0.0
        sum_sq = 0.0

        for i in range(lb):
            self._avg_slope += self._slopes[i]
        self._avg_slope /= float(lb)

        for i in range(lb):
            diff = self._slopes[i] - self._avg_slope
            sum_sq += diff * diff

        self._std_dev_slope = math.sqrt(sum_sq / float(lb))

    def CreateClone(self):
        return stochastic_slope_breakout_strategy()
