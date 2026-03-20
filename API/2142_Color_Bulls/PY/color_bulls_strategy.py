import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_bulls_strategy(Strategy):
    def __init__(self):
        super(color_bulls_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast MA Length", "Period of high price moving average", "Indicator")
        self._smooth_length = self.Param("SmoothLength", 5) \
            .SetDisplay("Smooth Length", "Period of smoothing moving average", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_value = 0.0
        self._prev_color = 1
        self._high_ma = None
        self._bulls_ma = None

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def smooth_length(self):
        return self._smooth_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_bulls_strategy, self).OnReseted()
        self._prev_value = 0.0
        self._prev_color = 1
        self._high_ma = None
        self._bulls_ma = None

    def OnStarted(self, time):
        super(color_bulls_strategy, self).OnStarted(time)
        self._high_ma = ExponentialMovingAverage()
        self._high_ma.Length = self.fast_length
        self._bulls_ma = ExponentialMovingAverage()
        self._bulls_ma.Length = self.smooth_length
        self.Indicators.Add(self._high_ma)
        self.Indicators.Add(self._bulls_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high_input = DecimalIndicatorValue(self._high_ma, candle.HighPrice, candle.OpenTime)
        high_input.IsFinal = True
        ma_value = self._high_ma.Process(high_input)
        if not self._high_ma.IsFormed:
            return
        bulls = float(candle.HighPrice) - float(ma_value)
        bulls_input = DecimalIndicatorValue(self._bulls_ma, bulls, candle.OpenTime)
        bulls_input.IsFinal = True
        smooth = float(self._bulls_ma.Process(bulls_input))
        if not self._bulls_ma.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if smooth > self._prev_value:
            color = 0
        elif smooth < self._prev_value:
            color = 2
        else:
            color = 1
        if self._prev_color == 0 and color == 2:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_color == 2 and color == 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_color = color
        self._prev_value = smooth

    def CreateClone(self):
        return color_bulls_strategy()
