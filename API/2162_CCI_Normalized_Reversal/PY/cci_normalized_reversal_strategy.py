import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cci_normalized_reversal_strategy(Strategy):
    def __init__(self):
        super(cci_normalized_reversal_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 10) \
            .SetDisplay("CCI Period", "Lookback period for CCI", "General")
        self._high_level = self.Param("HighLevel", 100) \
            .SetDisplay("High Level", "Upper CCI threshold", "General")
        self._middle_level = self.Param("MiddleLevel", 0) \
            .SetDisplay("Middle Level", "Middle CCI threshold", "General")
        self._low_level = self.Param("LowLevel", -100) \
            .SetDisplay("Low Level", "Lower CCI threshold", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_color = 2
        self._prev_prev_color = 2

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def middle_level(self):
        return self._middle_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_normalized_reversal_strategy, self).OnReseted()
        self._prev_color = 2
        self._prev_prev_color = 2

    def OnStarted(self, time):
        super(cci_normalized_reversal_strategy, self).OnStarted(time)
        self._prev_color = 2
        self._prev_prev_color = 2

        cci = CommodityChannelIndex()
        cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.process_candle).Start()

    def process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cci_val = float(cci_value)
        color = self._get_color_index(cci_val)

        # Close short when CCI rises above middle level
        if self._prev_color < 2 and self.Position < 0:
            self.BuyMarket()

        # Close long when CCI falls below middle level
        if self._prev_color > 2 and self.Position > 0:
            self.SellMarket()

        # Open long after leaving high zone
        if self._prev_prev_color == 0 and self._prev_color > 0 and self.Position <= 0:
            self.BuyMarket()

        # Open short after leaving low zone
        if self._prev_prev_color == 4 and self._prev_color < 4 and self.Position >= 0:
            self.SellMarket()

        self._prev_prev_color = self._prev_color
        self._prev_color = color

    def _get_color_index(self, cci):
        mid = float(self.middle_level)
        high = float(self.high_level)
        low = float(self.low_level)
        if cci > mid:
            return 0 if cci > high else 1
        if cci < mid:
            return 4 if cci < low else 3
        return 2

    def CreateClone(self):
        return cci_normalized_reversal_strategy()
