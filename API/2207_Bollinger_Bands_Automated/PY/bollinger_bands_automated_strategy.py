import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_automated_strategy(Strategy):
    def __init__(self):
        super(bollinger_bands_automated_strategy, self).__init__()
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_deviation = self.Param("BbDeviation", 2.0) \
            .SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def bb_period(self):
        return self._bb_period.Value

    @property
    def bb_deviation(self):
        return self._bb_deviation.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(bollinger_bands_automated_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self.bb_period
        bb.Width = self.bb_deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not bb_value.IsFormed:
            return
        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        middle = (upper + lower) / 2.0
        close = float(candle.ClosePrice)
        if self.Position > 0 and close >= middle:
            self.SellMarket()
        elif self.Position < 0 and close <= middle:
            self.BuyMarket()
        if close <= lower and self.Position <= 0:
            self.BuyMarket()
        elif close >= upper and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return bollinger_bands_automated_strategy()
