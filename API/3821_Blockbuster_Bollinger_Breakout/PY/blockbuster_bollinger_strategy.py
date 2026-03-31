import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class blockbuster_bollinger_strategy(Strategy):
    def __init__(self):
        super(blockbuster_bollinger_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger bands period", "Indicators")
        self._bollinger_width = self.Param("BollingerWidth", 1.0) \
            .SetDisplay("BB Width", "Bollinger bands deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def bollinger_width(self):
        return self._bollinger_width.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(blockbuster_bollinger_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(blockbuster_bollinger_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bollinger_period
        bb.Width = self.bollinger_width
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
        if close > upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close < lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif self.Position > 0 and close < middle:
            self.SellMarket()
        elif self.Position < 0 and close > middle:
            self.BuyMarket()

    def CreateClone(self):
        return blockbuster_bollinger_strategy()
