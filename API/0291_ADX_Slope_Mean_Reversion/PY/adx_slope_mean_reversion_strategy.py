import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class adx_slope_mean_reversion_strategy(Strategy):
    """
    ADX slope mean reversion strategy.
    Trades reversion of extreme ADX slope moves once the recent slope distribution is formed.
    """

    def __init__(self):
        super(adx_slope_mean_reversion_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicator Parameters")

        self._slope_lookback = self.Param("SlopeLookback", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Lookback", "Period for slope statistics", "Strategy Parameters")

        self._threshold_multiplier = self.Param("ThresholdMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entries", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._min_adx = self.Param("MinAdx", 18.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min ADX", "Minimum ADX level required for entries", "Signal Filters")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx = None
        self._previous_adx_value = 0.0
        self._slope_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_slope_mean_reversion_strategy, self).OnReseted()
        self._adx = None
        self._previous_adx_value = 0.0
        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(adx_slope_mean_reversion_strategy, self).OnStarted(time)

        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._adx = AverageDirectionalIndex()
        self._adx.Length = int(self._adx_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._adx, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._adx.IsFormed:
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return

        adx_val = float(adx_ma)
        dx = adx_value.Dx
        if dx is None or dx.Plus is None or dx.Minus is None:
            return

        di_plus = float(dx.Plus)
        di_minus = float(dx.Minus)

        if not self._is_initialized:
            self._previous_adx_value = adx_val
            self._is_initialized = True
            return

        slope = adx_val - self._previous_adx_value
        self._previous_adx_value = adx_val

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

        if std_slope <= 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        tm = float(self._threshold_multiplier.Value)
        lower_threshold = avg_slope - tm * std_slope
        upper_threshold = avg_slope + tm * std_slope
        min_adx = float(self._min_adx.Value)
        is_bullish = di_plus >= di_minus
        is_bearish = di_minus > di_plus

        if self.Position == 0:
            if adx_val >= min_adx and slope <= lower_threshold and is_bullish:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif adx_val >= min_adx and slope >= upper_threshold and is_bearish:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0:
            if slope >= avg_slope or not is_bullish:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            if slope <= avg_slope or not is_bearish:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return adx_slope_mean_reversion_strategy()
