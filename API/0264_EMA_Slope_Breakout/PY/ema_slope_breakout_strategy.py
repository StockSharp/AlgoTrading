import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ema_slope_breakout_strategy(Strategy):
    """
    Strategy based on EMA slope breakout.
    Enters when slope of EMA exceeds average slope plus a multiple of standard deviation.
    Exits when slope returns to average.
    """

    def __init__(self):
        super(ema_slope_breakout_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Period for EMA", "Indicator Parameters")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy Parameters")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Std dev multiplier for breakout", "Strategy Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_ema_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = []
        self._current_index = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_slope_breakout_strategy, self).OnReseted()
        self._prev_ema_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        lookback = self._lookback_period.Value
        self._slopes = [0.0] * lookback
        self._current_index = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(ema_slope_breakout_strategy, self).OnStarted(time)

        lookback = self._lookback_period.Value
        self._slopes = [0.0] * lookback

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

        sl = self._stop_loss_percent.Value
        self.StartProtection(
            Unit(0.0, UnitTypes.Absolute),
            Unit(float(sl), UnitTypes.Percent)
        )

    def _process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_val)

        if not self._is_initialized:
            self._prev_ema_value = ema_val
            self._is_initialized = True
            return

        self._current_slope = ema_val - self._prev_ema_value

        lookback = self._lookback_period.Value
        self._slopes[self._current_index] = self._current_slope
        self._current_index = (self._current_index + 1) % lookback

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ema_value = ema_val
            return

        self._calculate_statistics()

        if abs(self._avg_slope) > 0:
            dev_mult = self._deviation_multiplier.Value

            if (self._current_slope > 0 and
                    self._current_slope > self._avg_slope + dev_mult * self._std_dev_slope and
                    self.Position <= 0):
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif (self._current_slope < 0 and
                    self._current_slope < self._avg_slope - dev_mult * self._std_dev_slope and
                    self.Position >= 0):
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

            if self.Position > 0 and self._current_slope < self._avg_slope:
                self.SellMarket()
            elif self.Position < 0 and self._current_slope > self._avg_slope:
                self.BuyMarket()

        self._prev_ema_value = ema_val

    def _calculate_statistics(self):
        lookback = self._lookback_period.Value
        self._avg_slope = 0.0
        sum_sq = 0.0

        for i in range(lookback):
            self._avg_slope += self._slopes[i]
        self._avg_slope /= float(lookback)

        for i in range(lookback):
            diff = self._slopes[i] - self._avg_slope
            sum_sq += diff * diff

        self._std_dev_slope = math.sqrt(sum_sq / float(lookback))

    def CreateClone(self):
        return ema_slope_breakout_strategy()
