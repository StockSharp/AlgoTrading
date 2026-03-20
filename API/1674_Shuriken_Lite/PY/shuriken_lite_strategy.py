import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class shuriken_lite_strategy(Strategy):
    def __init__(self):
        super(shuriken_lite_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 14) \
            .SetDisplay("EMA", "EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("RSI", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(shuriken_lite_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val, rsi):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        # Buy when RSI oversold
        if rsi < 30 and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # Sell when RSI overbought
        elif rsi > 70 and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()

    def CreateClone(self):
        return shuriken_lite_strategy()
