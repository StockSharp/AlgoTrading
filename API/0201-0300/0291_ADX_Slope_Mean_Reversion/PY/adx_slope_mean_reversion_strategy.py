import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class adx_slope_mean_reversion_strategy(Strategy):
    """
    ADX slope mean reversion.
    Buys when the ADX slope is far below its average and starts turning up, sells when it is far above
    and starts turning down. Exits when the slope returns to its average.
    """

    def __init__(self):
        super(adx_slope_mean_reversion_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period of ADX", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for extreme slope", "Strategy")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._slope_average = None
        self._slope_std_dev = None
        self._prev_adx = None
        self._prev_slope = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(adx_slope_mean_reversion_strategy, self).OnReseted()
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_adx = None
        self._prev_slope = None

    def OnStarted2(self, time):
        super(adx_slope_mean_reversion_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._lookback_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(None, Unit(stop, UnitTypes.Percent) if stop > 0 else None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            adx_area = self.CreateChartArea()
            if adx_area is not None:
                self.DrawIndicator(adx_area, adx)

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed:
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return

        adx = float(adx_ma)

        if self._prev_adx is None:
            self._prev_adx = adx
            return

        slope = adx - self._prev_adx
        self._prev_adx = adx

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
        return adx_slope_mean_reversion_strategy()
