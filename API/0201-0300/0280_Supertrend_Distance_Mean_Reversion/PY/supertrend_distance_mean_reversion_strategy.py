import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

class supertrend_distance_mean_reversion_strategy(Strategy):
    """
    Supertrend distance mean reversion strategy.
    Trades large deviations of price from Supertrend and exits when the distance returns to its recent average.
    """

    def __init__(self):
        super(supertrend_distance_mean_reversion_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period for Supertrend calculation", "Supertrend")

        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Supertrend")

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

        self._supertrend = None
        self._distance_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_distance_mean_reversion_strategy, self).OnReseted()
        self._supertrend = None
        lb = int(self._lookback_period.Value)
        self._distance_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(supertrend_distance_mean_reversion_strategy, self).OnStarted2(time)

        lb = int(self._lookback_period.Value)
        self._distance_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._supertrend = SuperTrend()
        self._supertrend.Length = int(self._atr_period.Value)
        self._supertrend.Multiplier = self._multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._supertrend, self._process_supertrend).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_supertrend(self, candle, supertrend_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._supertrend.IsFormed:
            return

        close_price = float(candle.ClosePrice)
        st_val = float(supertrend_value)
        distance = abs(close_price - st_val)

        lb = int(self._lookback_period.Value)
        self._distance_history[self._current_index] = distance
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
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
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        dm = float(self._deviation_multiplier.Value)
        extended_threshold = avg_distance + std_distance * dm
        price_above_supertrend = close_price > st_val
        price_below_supertrend = close_price < st_val

        if self.Position == 0:
            if distance > extended_threshold:
                if price_above_supertrend:
                    self.SellMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
                elif price_below_supertrend:
                    self.BuyMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and (distance <= avg_distance or price_above_supertrend):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and (distance <= avg_distance or price_below_supertrend):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return supertrend_distance_mean_reversion_strategy()
