import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class dekidaka_ashi_candles_volume_strategy(Strategy):
    """
    Dekidaka-Ashi candles volume strategy.
    The range is the body of the previous candle, scaled around its middle by BodySize and by the ratio of that candle's volume
    to its VolumeSmooth EMA, so heavy candles give a wider range.
    A candle with its high above the upper bound and its low above the lower bound is bullish and goes long; one with its high below
    the upper bound and its low below the lower bound is bearish and goes short, reversing an opposite position.
    A candle spanning both bounds closes any position.
    """

    def __init__(self):
        super(dekidaka_ashi_candles_volume_strategy, self).__init__()
        self._body_size = self.Param("BodySize", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Body Size", "Multiplier of the body range", "Range")
        self._volume_smooth = self.Param("VolumeSmooth", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Smooth", "EMA length of the volume smoothing", "Range")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_ema = None
        self._prev_upper = None
        self._prev_lower = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(dekidaka_ashi_candles_volume_strategy, self).OnReseted()
        self._prev_upper = None
        self._prev_lower = None

    def OnStarted2(self, time):
        super(dekidaka_ashi_candles_volume_strategy, self).OnStarted2(time)

        self._prev_upper = None
        self._prev_lower = None
        self._volume_ema = ExponentialMovingAverage()
        self._volume_ema.Length = self._volume_smooth.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        upper = self._prev_upper
        lower = self._prev_lower

        smoothed = process_float(self._volume_ema, candle.TotalVolume, candle.ServerTime, True)
        if smoothed.IsFormed:
            avg_volume = float(smoothed.GetValue[Decimal](None))
            volume = float(candle.TotalVolume)
            ratio = volume / avg_volume if avg_volume > 0 else 1.0
            open_price = float(candle.OpenPrice)
            close = float(candle.ClosePrice)
            middle = (open_price + close) / 2.0
            half_body = abs(close - open_price) / 2.0 * float(self._body_size.Value) * ratio
            self._prev_upper = middle + half_body
            self._prev_lower = middle - half_body

        if upper is None or lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        bullish = high > upper and low > lower
        bearish = high < upper and low < lower
        spans_both = high >= upper and low <= lower

        if bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif spans_both and self.Position > 0:
            self.SellMarket(self.Position)
        elif spans_both and self.Position < 0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return dekidaka_ashi_candles_volume_strategy()
