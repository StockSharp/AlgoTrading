import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class envelope_limit_ladder_strategy(Strategy):
    def __init__(self):
        super(envelope_limit_ladder_strategy, self).__init__()
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger bands period", "Indicators")
        self._bb_width = self.Param("BbWidth", 1.5) \
            .SetDisplay("BB Width", "Bollinger bands deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def bb_period(self):
        return self._bb_period.Value

    @property
    def bb_width(self):
        return self._bb_width.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(envelope_limit_ladder_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(envelope_limit_ladder_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bb_period
        bb.Width = self.bb_width
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.process_candle).Start()

    def process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not bb_value.IsFinal or bb_value.IsEmpty:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        close = float(candle.ClosePrice)
        if close < lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close > upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif self.Position > 0 and close > middle:
            self.SellMarket()
        elif self.Position < 0 and close < middle:
            self.BuyMarket()

    def CreateClone(self):
        return envelope_limit_ladder_strategy()
