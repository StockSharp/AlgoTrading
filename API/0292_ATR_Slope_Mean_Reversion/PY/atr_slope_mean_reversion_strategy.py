import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class atr_slope_mean_reversion_strategy(Strategy):
    """
    ATR slope mean reversion strategy.
    Trades reversion of extreme ATR slope values with an EMA direction filter.
    """

    def __init__(self):
        super(atr_slope_mean_reversion_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicator Parameters")

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA direction filter", "Indicator Parameters")

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

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr = None
        self._ema = None
        self._previous_atr_value = 0.0
        self._slope_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_slope_mean_reversion_strategy, self).OnReseted()
        self._atr = None
        self._ema = None
        self._previous_atr_value = 0.0
        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(atr_slope_mean_reversion_strategy, self).OnStarted(time)

        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._ema, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._atr.IsFormed or not self._ema.IsFormed:
            return

        av = float(atr_value)
        ev = float(ema_value)

        if not self._is_initialized:
            self._previous_atr_value = av
            self._is_initialized = True
            return

        slope = av - self._previous_atr_value
        self._previous_atr_value = av

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
        close_price = float(candle.ClosePrice)
        price_above_ema = close_price >= ev
        price_below_ema = close_price <= ev

        if self.Position == 0:
            if slope <= lower_threshold and price_above_ema:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif slope >= upper_threshold and price_below_ema:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0:
            if slope >= avg_slope or price_below_ema:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            if slope <= avg_slope or price_above_ema:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return atr_slope_mean_reversion_strategy()
