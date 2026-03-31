import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class xma_range_channel_strategy(Strategy):
    def __init__(self):
        super(xma_range_channel_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for analysis", "General")
        self._length = self.Param("Length", 7) \
            .SetDisplay("Channel Length", "Period for high and low moving averages", "Indicator")
        self._high_ma = None
        self._low_ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def length(self):
        return self._length.Value

    def OnReseted(self):
        super(xma_range_channel_strategy, self).OnReseted()
        self._high_ma = None
        self._low_ma = None

    def OnStarted2(self, time):
        super(xma_range_channel_strategy, self).OnStarted2(time)
        self._high_ma = ExponentialMovingAverage()
        self._high_ma.Length = self.length
        self._low_ma = ExponentialMovingAverage()
        self._low_ma.Length = self.length
        self.Indicators.Add(self._high_ma)
        self.Indicators.Add(self._low_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high_input = DecimalIndicatorValue(self._high_ma, candle.HighPrice, candle.OpenTime)
        high_input.IsFinal = True
        upper_result = self._high_ma.Process(high_input)
        low_input = DecimalIndicatorValue(self._low_ma, candle.LowPrice, candle.OpenTime)
        low_input.IsFinal = True
        lower_result = self._low_ma.Process(low_input)
        if not self._high_ma.IsFormed or not self._low_ma.IsFormed:
            return
        upper = float(upper_result)
        lower = float(lower_result)
        close = float(candle.ClosePrice)
        # Breakout above upper band - go long
        if close > upper and self.Position <= 0:
            self.BuyMarket()
        # Breakout below lower band - go short
        elif close < lower and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return xma_range_channel_strategy()
