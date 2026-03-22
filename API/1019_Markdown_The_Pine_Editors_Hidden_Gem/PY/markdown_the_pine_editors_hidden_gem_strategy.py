import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class markdown_the_pine_editors_hidden_gem_strategy(Strategy):
    """
    Bollinger Bands breakout: buy above upper band, sell below lower band.
    """

    def __init__(self):
        super(markdown_the_pine_editors_hidden_gem_strategy, self).__init__()
        self._length = self.Param("Length", 50).SetDisplay("Length", "BB period", "Indicators")
        self._multiplier = self.Param("Multiplier", 2.0).SetDisplay("Multiplier", "BB width", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(markdown_the_pine_editors_hidden_gem_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(markdown_the_pine_editors_hidden_gem_strategy, self).OnStarted(time)
        self._bb = BollingerBands()
        self._bb.Length = self._length.Value
        self._bb.Width = self._multiplier.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._bb.IsFormed:
            return
        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return
        upper_f = float(upper)
        lower_f = float(lower)
        close = float(candle.ClosePrice)
        if self.Position <= 0 and close > upper_f:
            self.BuyMarket()
        elif self.Position >= 0 and close < lower_f:
            self.SellMarket()

    def CreateClone(self):
        return markdown_the_pine_editors_hidden_gem_strategy()
