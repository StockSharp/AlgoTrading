import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class stochastic_slope_mean_reversion_strategy(Strategy):
    """
    Stochastic slope mean reversion.
    The slope of the smoothed %K is compared with its average: buys when the slope is far below the average
    and starts turning up, sells when it is far above and starts turning down.
    Exits when the slope returns to its average.
    """

    def __init__(self):
        super(stochastic_slope_mean_reversion_strategy, self).__init__()

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Stochastic lookback period", "Stochastic")
        self._stoch_k_period = self.Param("StochKPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %K Period", "Smoothing period of %K", "Stochastic")
        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %D Period", "Period of %D", "Stochastic")
        self._slope_lookback = self.Param("SlopeLookback", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Lookback", "Period for slope statistics", "Strategy")
        self._threshold_multiplier = self.Param("ThresholdMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Threshold Multiplier", "Standard deviation multiplier for extreme slope", "Strategy")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._k_smoothing = None
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_k = None
        self._prev_slope = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(stochastic_slope_mean_reversion_strategy, self).OnReseted()
        self._k_smoothing = None
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_k = None
        self._prev_slope = None

    def OnStarted2(self, time):
        super(stochastic_slope_mean_reversion_strategy, self).OnStarted2(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_d_period.Value

        self._k_smoothing = SimpleMovingAverage()
        self._k_smoothing.Length = self._stoch_k_period.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._slope_lookback.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._slope_lookback.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stochastic, self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(None, Unit(stop, UnitTypes.Percent) if stop > 0 else None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            stoch_area = self.CreateChartArea()
            if stoch_area is not None:
                self.DrawIndicator(stoch_area, stochastic)

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        raw_k = stoch_value.K
        if raw_k is None:
            return

        k = float(process_float(self._k_smoothing, float(raw_k), candle.ServerTime, True))
        if not self._k_smoothing.IsFormed:
            return

        if self._prev_k is None:
            self._prev_k = k
            return

        slope = k - self._prev_k
        self._prev_k = k

        avg_slope = float(process_float(self._slope_average, slope, candle.ServerTime, True))
        std_slope = float(process_float(self._slope_std_dev, slope, candle.ServerTime, True))

        prev = self._prev_slope
        self._prev_slope = slope

        if not self._slope_average.IsFormed or not self._slope_std_dev.IsFormed or prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        m = float(self._threshold_multiplier.Value)

        # Extreme reading that has started to turn back toward the average.
        if slope < avg_slope - m * std_slope and slope > prev and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif slope > avg_slope + m * std_slope and slope < prev and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and slope >= avg_slope:
            self.SellMarket(self.Position)
        elif self.Position < 0 and slope <= avg_slope:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return stochastic_slope_mean_reversion_strategy()
