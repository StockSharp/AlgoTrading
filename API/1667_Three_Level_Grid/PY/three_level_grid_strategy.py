import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class three_level_grid_strategy(Strategy):
    def __init__(self):
        super(three_level_grid_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 30) \
            .SetDisplay("EMA Length", "EMA period for center line", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(three_level_grid_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        atr = StandardDeviation()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if atr_val <= 0:
            return
        close = candle.ClosePrice
        diff = close - ema_val
        # Buy at different grid levels below EMA
        if diff < -1.5 * atr_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell at different grid levels above EMA
        elif diff > 1.5 * atr_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Mean reversion exit
        elif self.Position > 0 and close > ema_val + 0.5 * atr_val:
            self.SellMarket()
        elif self.Position < 0 and close < ema_val - 0.5 * atr_val:
            self.BuyMarket()

    def CreateClone(self):
        return three_level_grid_strategy()
