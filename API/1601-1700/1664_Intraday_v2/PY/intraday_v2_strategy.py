import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class intraday_v2_strategy(Strategy):
    def __init__(self):
        super(intraday_v2_strategy, self).__init__()
        self._band_length = self.Param("BandLength", 20) \
            .SetDisplay("Band Length", "Band period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def band_length(self):
        return self._band_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(intraday_v2_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(intraday_v2_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self.band_length
        stdev = StandardDeviation()
        stdev.Length = self.band_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, stdev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, stdev_val):
        if candle.State != CandleStates.Finished:
            return
        if stdev_val <= 0:
            return
        close = candle.ClosePrice
        upper = sma_val + 2 * stdev_val
        lower = sma_val - 2 * stdev_val
        # Mean reversion: buy at lower band
        if close < lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Mean reversion: sell at upper band
        elif close > upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long at middle (SMA)
        if self.Position > 0 and close > sma_val:
            self.SellMarket()
        # Exit short at middle (SMA)
        elif self.Position < 0 and close < sma_val:
            self.BuyMarket()

    def CreateClone(self):
        return intraday_v2_strategy()
