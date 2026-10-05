import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class adx_with_volume_breakout_strategy(Strategy):
    """
    ADX trend strength with a volume breakout confirmation.
    Enters in the direction of the dominant directional index when ADX is above AdxThreshold
    and volume exceeds its average by VolumeThresholdFactor. The opposite signal reverses the position.
    """

    def __init__(self):
        super(adx_with_volume_breakout_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators")
        self._volume_threshold_factor = self.Param("VolumeThresholdFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Factor", "Volume must exceed its average by this factor", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._volume_sma = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(adx_with_volume_breakout_strategy, self).OnReseted()
        self._volume_sma = None

    def OnStarted2(self, time):
        super(adx_with_volume_breakout_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_avg_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, self._process_candle).Start()

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

        # The average is taken over previous bars so the current bar's volume is compared with history.
        volume_average = float(get_current_value(self._volume_sma))
        volume_formed = self._volume_sma.IsFormed
        process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True)

        if not volume_formed or not adx_value.IsFormed:
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return

        dx = adx_value.Dx
        if dx.Plus is None or dx.Minus is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx = float(adx_ma)
        plus_di = float(dx.Plus)
        minus_di = float(dx.Minus)
        volume = float(candle.TotalVolume)

        if adx <= float(self._adx_threshold.Value) or \
                volume <= volume_average * float(self._volume_threshold_factor.Value):
            return

        if plus_di > minus_di and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif minus_di > plus_di and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return adx_with_volume_breakout_strategy()
