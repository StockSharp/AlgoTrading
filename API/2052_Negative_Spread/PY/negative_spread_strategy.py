import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class negative_spread_strategy(Strategy):
    def __init__(self):
        super(negative_spread_strategy, self).__init__()
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_width = self.Param("BbWidth", 1.5) \
            .SetDisplay("BB Width", "Bollinger Bands deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def BbPeriod(self):
        return self._bb_period.Value

    @BbPeriod.setter
    def BbPeriod(self, value):
        self._bb_period.Value = value

    @property
    def BbWidth(self):
        return self._bb_width.Value

    @BbWidth.setter
    def BbWidth(self, value):
        self._bb_width.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(negative_spread_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(negative_spread_strategy, self).OnStarted2(time)

        bb = BollingerBands()
        bb.Length = self.BbPeriod
        bb.Width = self.BbWidth

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(bb, self.OnProcess).Start()

        self.StartProtection(
            takeProfit=Unit(1, UnitTypes.Percent),
            stopLoss=Unit(0.5, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not bb_value.IsFormed:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return

        close = candle.ClosePrice

        if close > float(upper) and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif close < float(lower) and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

    def CreateClone(self):
        return negative_spread_strategy()
