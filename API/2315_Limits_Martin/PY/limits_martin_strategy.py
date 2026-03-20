import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class limits_martin_strategy(Strategy):
    def __init__(self):
        super(limits_martin_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI", "Indicators")
        self._oversold = self.Param("Oversold", 30.0) \
            .SetDisplay("Oversold", "RSI oversold level", "Indicators")
        self._overbought = self.Param("Overbought", 70.0) \
            .SetDisplay("Overbought", "RSI overbought level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def oversold(self):
        return self._oversold.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(limits_martin_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        rsi_value = float(rsi_value)
        if rsi_value < float(self.oversold) and self.Position <= 0:
            self.BuyMarket()
        elif rsi_value > float(self.overbought) and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return limits_martin_strategy()
