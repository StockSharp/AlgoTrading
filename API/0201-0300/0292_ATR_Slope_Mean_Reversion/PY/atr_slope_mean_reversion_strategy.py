import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class atr_slope_mean_reversion_strategy(Strategy):
    """
    ATR slope mean reversion.
    Buys when the ATR slope is far below its average and starts turning up, sells when it is far above
    and starts turning down. Exits when the slope returns to its average or the ATR stop is hit.
    """

    def __init__(self):
        super(atr_slope_mean_reversion_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period of ATR", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for extreme slope", "Strategy")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 2) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss Multiplier", "Stop-loss distance in ATR multiples", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._slope_average = None
        self._slope_std_dev = None
        self._prev_atr = None
        self._prev_slope = None
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(atr_slope_mean_reversion_strategy, self).OnReseted()
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_atr = None
        self._prev_slope = None
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(atr_slope_mean_reversion_strategy, self).OnStarted2(time)

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._lookback_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            atr_area = self.CreateChartArea()
            if atr_area is not None:
                self.DrawIndicator(atr_area, atr)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_value = float(atr_value)

        if self._prev_atr is None:
            self._prev_atr = atr_value
            return

        slope = atr_value - self._prev_atr
        self._prev_atr = atr_value

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
        stop_distance = self._stop_loss_multiplier.Value * atr_value

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
        return atr_slope_mean_reversion_strategy()
