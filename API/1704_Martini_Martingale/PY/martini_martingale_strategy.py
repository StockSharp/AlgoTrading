import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class martini_martingale_strategy(Strategy):
    def __init__(self):
        super(martini_martingale_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 7) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA for mean reversion target", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(martini_martingale_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, sma):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        # RSI oversold => buy
        if rsi < 30 and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # RSI overbought => sell
        elif rsi > 70 and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        # Exit long at SMA
        elif self.Position > 0 and close >= sma and rsi > 50:
            self.SellMarket()
        # Exit short at SMA
        elif self.Position < 0 and close <= sma and rsi < 50:
            self.BuyMarket()

    def CreateClone(self):
        return martini_martingale_strategy()
