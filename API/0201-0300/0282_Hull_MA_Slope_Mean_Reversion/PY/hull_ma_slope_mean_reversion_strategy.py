import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class hull_ma_slope_mean_reversion_strategy(Strategy):
    """
    Hull Moving Average slope mean reversion.
    Buys when the slope is far below its average and starts turning up, sells when it is far above
    and starts turning down. Exits when the slope returns to its average or the ATR stop is hit.
    """

    def __init__(self):
        super(hull_ma_slope_mean_reversion_strategy, self).__init__()

        self._hull_period = self.Param("HullPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull Period", "Period of Hull Moving Average", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for extreme slope", "Strategy")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period for the stop", "Risk Management")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetNotNegative() \
            .SetDisplay("ATR Multiplier", "Stop-loss distance in ATR multiples", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._slope_average = None
        self._slope_std_dev = None
        self._prev_hull = None
        self._prev_slope = None
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hull_ma_slope_mean_reversion_strategy, self).OnReseted()
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_hull = None
        self._prev_slope = None
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(hull_ma_slope_mean_reversion_strategy, self).OnStarted2(time)

        hull = HullMovingAverage()
        hull.Length = self._hull_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._lookback_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(hull, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hull_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        hull_value = float(hull_value)
        atr_value = float(atr_value)

        if self._prev_hull is None:
            self._prev_hull = hull_value
            return

        slope = hull_value - self._prev_hull
        self._prev_hull = hull_value

        avg_slope = float(process_float(self._slope_average, slope, candle.ServerTime, True))
        std_slope = float(process_float(self._slope_std_dev, slope, candle.ServerTime, True))

        prev = self._prev_slope
        self._prev_slope = slope

        if not self._slope_average.IsFormed or not self._slope_std_dev.IsFormed or prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_stop(candle):
            return

        close = float(candle.ClosePrice)
        k = float(self._deviation_multiplier.Value)
        stop_distance = float(self._atr_multiplier.Value) * atr_value

        # Extreme reading that has started to turn back toward the average.
        if slope < avg_slope - k * std_slope and slope > prev and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance if stop_distance > 0 else 0.0
        elif slope > avg_slope + k * std_slope and slope < prev and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance if stop_distance > 0 else 0.0
        elif (self.Position > 0 and slope >= avg_slope) or (self.Position < 0 and slope <= avg_slope):
            self._exit_position()

    def _check_stop(self, candle):
        if self._stop_price == 0.0:
            return False

        if (self.Position > 0 and float(candle.LowPrice) <= self._stop_price) or \
                (self.Position < 0 and float(candle.HighPrice) >= self._stop_price):
            self._exit_position()
            return True

        return False

    def _exit_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)

        self._stop_price = 0.0

    def CreateClone(self):
        return hull_ma_slope_mean_reversion_strategy()
