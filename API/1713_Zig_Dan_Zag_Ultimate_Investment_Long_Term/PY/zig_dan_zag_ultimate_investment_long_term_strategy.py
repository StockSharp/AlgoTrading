import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zig_dan_zag_ultimate_investment_long_term_strategy(Strategy):
    def __init__(self):
        super(zig_dan_zag_ultimate_investment_long_term_strategy, self).__init__()
        self._zigzag_depth = self.Param("ZigzagDepth", 12) \
            .SetDisplay("ZigZag Depth", "Pivot search depth", "ZigZag")
        self._sma_length = self.Param("SmaLength", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("SMA Length", "Long-term trend filter", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_zigzag = 0.0
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._direction = 0
        self._sma = 0.0

    @property
    def zigzag_depth(self):
        return self._zigzag_depth.Value

    @property
    def sma_length(self):
        return self._sma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zig_dan_zag_ultimate_investment_long_term_strategy, self).OnReseted()
        self._last_zigzag = 0.0
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._direction = 0
        self._sma = 0.0

    def OnStarted(self, time):
        super(zig_dan_zag_ultimate_investment_long_term_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.zigzag_depth
        lowest = Lowest()
        lowest.Length = self.zigzag_depth
        sma = SimpleMovingAverage()
        sma.Length = self.sma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, highest, lowest, sma_value):
        if candle.State != CandleStates.Finished:
            return
        self._sma = sma_value
        # update last ZigZag pivot
        if candle.HighPrice >= highest and self._direction != 1:
            self._last_zigzag = candle.HighPrice
            self._last_zigzag_high = candle.HighPrice
            self._direction = 1
        elif candle.LowPrice <= lowest and self._direction != -1:
            self._last_zigzag = candle.LowPrice
            self._last_zigzag_low = candle.LowPrice
            self._direction = -1
        # long-only logic using SMA as trend filter
        if self._last_zigzag == self._last_zigzag_low and candle.ClosePrice > self._sma and self.Position <= 0:
            self.BuyMarket()
        elif self._last_zigzag == self._last_zigzag_high and candle.ClosePrice < self._sma and self.Position > 0:
            self.SellMarket()

    def CreateClone(self):
        return zig_dan_zag_ultimate_investment_long_term_strategy()
