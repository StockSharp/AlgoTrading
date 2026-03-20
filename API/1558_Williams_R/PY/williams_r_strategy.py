import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class williams_r_strategy(Strategy):
    def __init__(self):
        super(williams_r_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "General")
        self._oversold = self.Param("Oversold", 20) \
            .SetDisplay("Oversold", "Oversold level", "General")
        self._overbought = self.Param("Overbought", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Overbought", "Overbought/exit level", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_high = 0.0

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def oversold(self):
        return self._oversold.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_r_strategy, self).OnReseted()
        self._prev_high = 0.0

    def OnStarted(self, time):
        super(williams_r_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        long_signal = rsi_val < self.oversold
        exit_signal = (self._prev_high > 0 and candle.ClosePrice > self._prev_high) or rsi_val > self.overbought
        if long_signal and self.Position <= 0:
            self.BuyMarket()
        elif exit_signal and self.Position > 0:
            self.SellMarket()
        self._prev_high = candle.HighPrice

    def CreateClone(self):
        return williams_r_strategy()
