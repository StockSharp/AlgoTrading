import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_distance_mean_reversion_strategy(Strategy):
    """
    Parabolic SAR distance mean reversion strategy.
    Trades large deviations of price from a locally calculated Parabolic SAR level
    and exits when the distance returns to its recent average.
    """

    def __init__(self):
        super(parabolic_sar_distance_mean_reversion_strategy, self).__init__()

        self._acceleration_factor = self.Param("AccelerationFactor", 0.02) \
            .SetGreaterThanZero() \
            .SetDisplay("Acceleration Factor", "Acceleration factor for Parabolic SAR", "Parabolic SAR")

        self._acceleration_limit = self.Param("AccelerationLimit", 0.2) \
            .SetGreaterThanZero() \
            .SetDisplay("Acceleration Limit", "Acceleration limit for Parabolic SAR", "Parabolic SAR")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Lookback period for distance statistics", "Strategy Parameters")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._distance_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False
        self._is_bullish_trend = False
        self._sar_value = 0.0
        self._extreme_point = 0.0
        self._acceleration = 0.0
        self._previous_high = 0.0
        self._previous_low = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_distance_mean_reversion_strategy, self).OnReseted()
        lb = int(self._lookback_period.Value)
        self._distance_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False
        self._is_bullish_trend = False
        self._sar_value = 0.0
        self._extreme_point = 0.0
        self._acceleration = 0.0
        self._previous_high = 0.0
        self._previous_low = 0.0

    def OnStarted2(self, time):
        super(parabolic_sar_distance_mean_reversion_strategy, self).OnStarted2(time)

        lb = int(self._lookback_period.Value)
        self._distance_history = [0.0] * lb
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

    def _initialize_state(self, candle):
        close_price = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)

        self._is_bullish_trend = close_price >= open_price
        self._sar_value = low_price if self._is_bullish_trend else high_price
        self._extreme_point = high_price if self._is_bullish_trend else low_price
        self._acceleration = float(self._acceleration_factor.Value)
        self._previous_high = high_price
        self._previous_low = low_price
        self._is_initialized = True

    def _update_sar(self, candle):
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        af = float(self._acceleration_factor.Value)
        al = float(self._acceleration_limit.Value)

        self._sar_value += self._acceleration * (self._extreme_point - self._sar_value)

        if self._is_bullish_trend:
            self._sar_value = min(self._sar_value, self._previous_low)

            if low_price <= self._sar_value:
                self._is_bullish_trend = False
                self._sar_value = self._extreme_point
                self._extreme_point = low_price
                self._acceleration = af
            elif high_price > self._extreme_point:
                self._extreme_point = high_price
                self._acceleration = min(self._acceleration + af, al)
        else:
            self._sar_value = max(self._sar_value, self._previous_high)

            if high_price >= self._sar_value:
                self._is_bullish_trend = True
                self._sar_value = self._extreme_point
                self._extreme_point = high_price
                self._acceleration = af
            elif low_price < self._extreme_point:
                self._extreme_point = low_price
                self._acceleration = min(self._acceleration + af, al)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self._is_initialized:
            self._initialize_state(candle)
            return

        self._update_sar(candle)

        close_price = float(candle.ClosePrice)
        distance = abs(close_price - self._sar_value)

        lb = int(self._lookback_period.Value)
        self._distance_history[self._current_index] = distance
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            self._previous_high = float(candle.HighPrice)
            self._previous_low = float(candle.LowPrice)
            return

        avg_distance = 0.0
        for i in range(lb):
            avg_distance += self._distance_history[i]
        avg_distance /= float(lb)

        sum_sq = 0.0
        for i in range(lb):
            diff = self._distance_history[i] - avg_distance
            sum_sq += diff * diff
        std_distance = math.sqrt(sum_sq / float(lb))

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_high = float(candle.HighPrice)
            self._previous_low = float(candle.LowPrice)
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_high = float(candle.HighPrice)
            self._previous_low = float(candle.LowPrice)
            return

        dm = float(self._deviation_multiplier.Value)
        extended_threshold = avg_distance + std_distance * dm
        price_above_sar = close_price > self._sar_value
        price_below_sar = close_price < self._sar_value

        if self.Position == 0:
            if distance > extended_threshold:
                if price_above_sar:
                    self.SellMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
                elif price_below_sar:
                    self.BuyMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and (distance <= avg_distance or price_above_sar):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and (distance <= avg_distance or price_below_sar):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

        self._previous_high = float(candle.HighPrice)
        self._previous_low = float(candle.LowPrice)

    def CreateClone(self):
        return parabolic_sar_distance_mean_reversion_strategy()
