import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class volume_slope_mean_reversion_strategy(Strategy):
    """
    Volume slope mean reversion.
    The slope of the smoothed volume is compared with its average: buys when the slope is far below the average
    and starts turning up, sells when it is far above and starts turning down.
    Exits when the slope returns to its average.
    """

    def __init__(self):
        super(volume_slope_mean_reversion_strategy, self).__init__()

        self._volume_ma_period = self.Param("VolumeMaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume MA Period", "Period of the volume moving average", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for extreme slope", "Strategy")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._volume_ma = None
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_volume = None
        self._prev_slope = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(volume_slope_mean_reversion_strategy, self).OnReseted()
        self._volume_ma = None
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_volume = None
        self._prev_slope = None

    def OnStarted2(self, time):
        super(volume_slope_mean_reversion_strategy, self).OnStarted2(time)

        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self._volume_ma_period.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._lookback_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(None, Unit(stop, UnitTypes.Percent) if stop > 0 else None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        volume = float(process_float(self._volume_ma, candle.TotalVolume, candle.ServerTime, True))

        if not self._volume_ma.IsFormed:
            return

        if self._prev_volume is None:
            self._prev_volume = volume
            return

        slope = volume - self._prev_volume
        self._prev_volume = volume

        avg_slope = float(process_float(self._slope_average, slope, candle.ServerTime, True))
        std_slope = float(process_float(self._slope_std_dev, slope, candle.ServerTime, True))

        prev = self._prev_slope
        self._prev_slope = slope

        if not self._slope_average.IsFormed or not self._slope_std_dev.IsFormed or prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = float(self._deviation_multiplier.Value)

        # Extreme reading that has started to turn back toward the average.
        if slope < avg_slope - k * std_slope and slope > prev and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif slope > avg_slope + k * std_slope and slope < prev and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and slope >= avg_slope:
            self.SellMarket(self.Position)
        elif self.Position < 0 and slope <= avg_slope:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return volume_slope_mean_reversion_strategy()
