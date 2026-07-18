import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class xbug_free_strategy(Strategy):
    def __init__(self):
        super(xbug_free_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(xbug_free_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        atr = StandardDeviation()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma, atr):
        if candle.State != CandleStates.Finished:
            return
        if atr <= 0:
            return
        close = candle.ClosePrice
        # Counter-trend: buy when price drops below SMA by 1 ATR
        if close < sma - atr and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Counter-trend: sell when price rises above SMA by 1 ATR
        elif close > sma + atr and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long at SMA
        elif self.Position > 0 and close >= sma:
            self.SellMarket()
        # Exit short at SMA
        elif self.Position < 0 and close <= sma:
            self.BuyMarket()

    def CreateClone(self):
        return xbug_free_strategy()
