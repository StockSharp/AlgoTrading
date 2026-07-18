import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class return_strategy(Strategy):
    def __init__(self):
        super(return_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._period = self.Param("Period", 20) \
            .SetDisplay("Period", "Bollinger Bands period", "Indicators")
        self._width = self.Param("Width", 2.0) \
            .SetDisplay("Width", "Bollinger Bands width", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Period(self):
        return self._period.Value

    @property
    def Width(self):
        return self._width.Value

    def OnStarted2(self, time):
        super(return_strategy, self).OnStarted2(time)

        ma = SimpleMovingAverage()
        ma.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        middle = float(ma_value)
        band_width = float(self.Width) / 100.0
        upper = middle * (1.0 + band_width)
        lower = middle * (1.0 - band_width)
        close = float(candle.ClosePrice)

        # Buy when price drops below lower band (mean reversion)
        if close < lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell when price rises above upper band
        elif close > upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long at middle band
        elif self.Position > 0 and close >= middle:
            self.SellMarket()
        # Exit short at middle band
        elif self.Position < 0 and close <= middle:
            self.BuyMarket()

    def CreateClone(self):
        return return_strategy()
