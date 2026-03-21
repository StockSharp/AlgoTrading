import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class corr_time_strategy(Strategy):
    """
    Bollinger Bands mean reversion strategy.
    Buys when close drops below lower BB, sells when close rises above upper BB.
    """

    def __init__(self):
        super(corr_time_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_width = self.Param("BbWidth", 2.0) \
            .SetDisplay("BB Width", "Bollinger Bands width", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(corr_time_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_width.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def on_process(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        upper = bb_val.UpBand
        lower = bb_val.LowBand
        if upper is None or lower is None:
            return

        close = float(candle.ClosePrice)
        upper = float(upper)
        lower = float(lower)

        if close < lower and self.Position <= 0:
            self.BuyMarket()
        elif close > upper and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return corr_time_strategy()
