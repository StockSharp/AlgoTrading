import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class fxscalper_strategy(Strategy):
    def __init__(self):
        super(fxscalper_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands length", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("BB Width", "Bollinger Bands width", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._entry_price = 0.0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fxscalper_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(fxscalper_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def on_process(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        upper = float(value.UpBand)
        lower = float(value.LowBand)
        middle = float(value.MovingAverage)
        if upper == 0 or lower == 0:
            return
        close = float(candle.ClosePrice)
        if close > upper and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
        elif close < lower and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
        if self.Position > 0 and close <= middle:
            self.SellMarket()
        elif self.Position < 0 and close >= middle:
            self.BuyMarket()

    def CreateClone(self):
        return fxscalper_strategy()
