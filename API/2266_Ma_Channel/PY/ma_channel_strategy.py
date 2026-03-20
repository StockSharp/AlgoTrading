import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class ma_channel_strategy(Strategy):
    def __init__(self):
        super(ma_channel_strategy, self).__init__()
        self._length = self.Param("Length", 8) \
            .SetDisplay("Length", "Moving average period", "Parameters")
        self._offset = self.Param("Offset", 100.0) \
            .SetDisplay("Offset", "Price offset from the average", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._ma_high = None
        self._ma_low = None
        self._trend = 0

    @property
    def length(self):
        return self._length.Value

    @property
    def offset(self):
        return self._offset.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_channel_strategy, self).OnReseted()
        self._ma_high = None
        self._ma_low = None
        self._trend = 0

    def OnStarted(self, time):
        super(ma_channel_strategy, self).OnStarted(time)
        self._trend = 0
        self._ma_high = ExponentialMovingAverage()
        self._ma_high.Length = self.length
        self._ma_low = ExponentialMovingAverage()
        self._ma_low.Length = self.length
        self.Indicators.Add(self._ma_high)
        self.Indicators.Add(self._ma_low)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        t = candle.ServerTime
        input_high = DecimalIndicatorValue(self._ma_high, float(candle.HighPrice), t)
        input_high.IsFinal = True
        high_result = self._ma_high.Process(input_high)
        input_low = DecimalIndicatorValue(self._ma_low, float(candle.LowPrice), t)
        input_low.IsFinal = True
        low_result = self._ma_low.Process(input_low)
        if not self._ma_high.IsFormed or not self._ma_low.IsFormed:
            return
        offset_val = float(self.offset)
        upper = float(high_result.GetValue[float]()) + offset_val
        lower = float(low_result.GetValue[float]()) - offset_val
        prev_trend = self._trend
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        if high_price > upper:
            self._trend = 1
        elif low_price < lower:
            self._trend = -1
        if prev_trend <= 0 and self._trend > 0 and self.Position <= 0:
            self.BuyMarket()
        elif prev_trend >= 0 and self._trend < 0 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return ma_channel_strategy()
