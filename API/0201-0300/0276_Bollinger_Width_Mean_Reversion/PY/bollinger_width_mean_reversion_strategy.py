import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class bollinger_width_mean_reversion_strategy(Strategy):
    """
    Bollinger Bands width mean reversion.
    Enters when the band width is beyond its average by a standard deviation multiplier and starts
    turning back toward the average: long on an extreme contraction, short on an extreme expansion.
    Exits when the width returns to its average or the ATR stop is hit.
    """

    def __init__(self):
        super(bollinger_width_mean_reversion_strategy, self).__init__()

        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviations for Bollinger Bands", "Indicators")
        self._width_lookback_period = self.Param("WidthLookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Lookback", "Period for width statistics", "Strategy")
        self._width_deviation_multiplier = self.Param("WidthDeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Deviation Multiplier", "Standard deviation multiplier for extreme width", "Strategy")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period for the stop", "Risk Management")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetNotNegative() \
            .SetDisplay("ATR Multiplier", "Stop-loss distance in ATR multiples", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._width_average = None
        self._width_std_dev = None
        self._prev_width = None
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(bollinger_width_mean_reversion_strategy, self).OnReseted()
        self._width_average = None
        self._width_std_dev = None
        self._prev_width = None
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(bollinger_width_mean_reversion_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_length.Value
        bollinger.Width = self._bollinger_deviation.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self._width_lookback_period.Value
        self._width_std_dev = StandardDeviation()
        self._width_std_dev.Length = self._width_lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not atr_value.IsFormed:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        if upper is None or lower is None:
            return

        atr = float(atr_value)
        width = float(upper) - float(lower)
        avg_width = float(process_float(self._width_average, width, candle.ServerTime, True))
        std_width = float(process_float(self._width_std_dev, width, candle.ServerTime, True))

        prev = self._prev_width
        self._prev_width = width

        if not self._width_average.IsFormed or not self._width_std_dev.IsFormed or prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_stop(candle):
            return

        close = float(candle.ClosePrice)
        k = float(self._width_deviation_multiplier.Value)
        stop_distance = float(self._atr_multiplier.Value) * atr

        # Extreme reading that has started to turn back toward the average.
        if width < avg_width - k * std_width and width > prev and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance if stop_distance > 0 else 0.0
        elif width > avg_width + k * std_width and width < prev and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance if stop_distance > 0 else 0.0
        elif (self.Position > 0 and width >= avg_width) or (self.Position < 0 and width <= avg_width):
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
        return bollinger_width_mean_reversion_strategy()
