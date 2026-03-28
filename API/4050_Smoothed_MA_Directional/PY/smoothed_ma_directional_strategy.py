import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class smoothed_ma_directional_strategy(Strategy):
    def __init__(self):
        super(smoothed_ma_directional_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 12).SetDisplay("MA Period", "Number of bars for MA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Time frame", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnStarted(self, time):
        super(smoothed_ma_directional_strategy, self).OnStarted(time)

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if close > ma_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close < ma_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return smoothed_ma_directional_strategy()
