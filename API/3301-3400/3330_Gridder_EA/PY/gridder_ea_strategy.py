import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class gridder_ea_strategy(Strategy):
    """
    Gridder EA: Bollinger Bands mean reversion.
    Buys when close < lower BB, sells when close > upper BB.
    """

    def __init__(self):
        super(gridder_ea_strategy, self).__init__()
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(gridder_ea_strategy, self).OnStarted2(time)

        bb = BollingerBands()
        bb.Length = self._bb_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        upper = bb_val.UpBand
        lower = bb_val.LowBand
        if upper is None or lower is None:
            return

        upper = float(upper)
        lower = float(lower)
        close = float(candle.ClosePrice)

        if close < lower and self.Position <= 0:
            self.BuyMarket()
        elif close > upper and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return gridder_ea_strategy()
