import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class ai_grid_strategy(Strategy):
    def __init__(self):
        super(ai_grid_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("MA Length", "SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(ai_grid_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.ma_length
        atr = StandardDeviation()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if atr_val <= 0:
            return
        close = candle.ClosePrice
        deviation = close - sma_val
        # Grid buy: price dropped by more than 1 ATR below SMA
        if deviation < -atr_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Grid sell: price rose by more than 1 ATR above SMA
        elif deviation > atr_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Take profit at mean
        elif self.Position > 0 and close > sma_val:
            self.SellMarket()
        elif self.Position < 0 and close < sma_val:
            self.BuyMarket()

    def CreateClone(self):
        return ai_grid_strategy()
