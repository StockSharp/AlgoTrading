import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import OnBalanceVolume, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class obv_slope_breakout_strategy(Strategy):
    """
    Strategy based on OBV slope breakout with EMA direction filter.
    Opens positions when OBV slope deviates from its recent average and price confirms direction relative to EMA.
    """

    def __init__(self):
        super(obv_slope_breakout_strategy, self).__init__()

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating average and standard deviation of OBV slope", "Strategy Parameters") \
            .SetOptimize(10, 50, 5)

        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Std Dev Multiplier", "Multiplier for standard deviation to determine breakout threshold", "Strategy Parameters") \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop-loss as a percentage of entry price", "Risk Management")

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA trend confirmation", "Indicator Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use in the strategy", "General")

        self._obv = None
        self._ema = None
        self._prev_obv = 0.0
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
        super(obv_slope_breakout_strategy, self).OnReseted()
        self._obv = None
        self._ema = None
        self._prev_obv = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        lb = int(self._lookback_period.Value)
        self._slopes = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(obv_slope_breakout_strategy, self).OnStarted(time)

        lb = int(self._lookback_period.Value)
        self._slopes = [0.0] * lb
        self._cooldown = 0
        self._filled_count = 0
        self._current_index = 0

        self._obv = OnBalanceVolume()
        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._obv, self._ema, self._process_obv).Start()

        self.StartProtection(Unit(), Unit(self._stop_loss.Value, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawIndicator(area, self._obv)
            self.DrawOwnTrades(area)

    def _process_obv(self, candle, obv_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._obv.IsFormed or not self._ema.IsFormed:
            return

        obv_val = float(obv_value)
        ema_val = float(ema_value)

        if not self._is_initialized:
            self._prev_obv = obv_val
            self._is_initialized = True
            return

        self._current_slope = obv_val - self._prev_obv
        self._prev_obv = obv_val

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

        mult = float(self._multiplier.Value)
        upper_threshold = self._avg_slope + mult * self._std_dev_slope
        lower_threshold = self._avg_slope - mult * self._std_dev_slope
        close_price = float(candle.ClosePrice)
        price_above_ema = close_price > ema_val
        price_below_ema = close_price < ema_val

        if self.Position == 0:
            if self._current_slope > upper_threshold and price_above_ema:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif self._current_slope < lower_threshold and price_below_ema:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0:
            if self._current_slope <= self._avg_slope or price_below_ema:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            if self._current_slope >= self._avg_slope or price_above_ema:
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
        return obv_slope_breakout_strategy()
