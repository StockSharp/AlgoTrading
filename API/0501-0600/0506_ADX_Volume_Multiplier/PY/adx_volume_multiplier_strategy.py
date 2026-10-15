import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class adx_volume_multiplier_strategy(Strategy):
    """
    ADX Volume Multiplier strategy.
    When ADX is above AdxThreshold and the candle volume exceeds VolumeMultiplier times its SMA, the strategy goes long if DI+ is above
    DI- and short if DI- is above DI+; the opposite signal reverses the position.
    """

    def __init__(self):
        super(adx_volume_multiplier_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 21).SetGreaterThanZero().SetDisplay("ADX Period", "ADX period", "ADX")
        self._adx_threshold = self.Param("AdxThreshold", 26.0).SetDisplay("ADX Threshold", "ADX level a trend must exceed", "ADX")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.8).SetGreaterThanZero().SetDisplay("Volume Multiplier", "Multiple of the average volume the candle volume must exceed", "Volume")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Period of the volume SMA", "Volume")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_volume_multiplier_strategy, self).OnReseted()
        self._volume_sma = None

    def OnStarted2(self, time):
        super(adx_volume_multiplier_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        volume_average = process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True).GetValue[Decimal](None)

        if not adx_value.IsFormed or not self._volume_sma.IsFormed:
            return

        adx = adx_value.MovingAverage
        di_plus = adx_value.Dx.Plus
        di_minus = adx_value.Dx.Minus
        if adx is None or di_plus is None or di_minus is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        volume_surge = candle.TotalVolume > volume_average * Decimal(self._volume_multiplier.Value)
        strong = adx > Decimal(self._adx_threshold.Value)

        if strong and volume_surge and di_plus > di_minus and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif strong and volume_surge and di_minus > di_plus and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return adx_volume_multiplier_strategy()
