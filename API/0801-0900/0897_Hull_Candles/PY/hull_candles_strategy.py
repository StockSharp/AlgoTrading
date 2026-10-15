import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class hull_candles_strategy(Strategy):
    """
    Hull Candles strategy.
    A Hull moving average of BodyLength is built on the OHLC4 price and smoothed by an SMA of SmaLength. When the HMA rises and the
    close is above that SMA the strategy goes long; when the HMA falls and the close is below it the strategy goes short, reversing
    an opposite position.
    """

    def __init__(self):
        super(hull_candles_strategy, self).__init__()
        self._body_length = self.Param("BodyLength", 10).SetGreaterThanZero().SetDisplay("Body Length", "Hull moving average length", "Indicators")
        self._sma_length = self.Param("SmaLength", 1).SetGreaterThanZero().SetDisplay("SMA Length", "Length of the SMA that smooths the HMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._hma = None
        self._sma = None
        self._prev_hma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_candles_strategy, self).OnReseted()
        self._prev_hma = None

    def OnStarted2(self, time):
        super(hull_candles_strategy, self).OnStarted2(time)

        self._prev_hma = None
        self._hma = HullMovingAverage()
        self._hma.Length = self._body_length.Value
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._hma)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ohlc4 = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(4)

        hma_value = process_float(self._hma, ohlc4, candle.OpenTime, True)
        if not self._hma.IsFormed:
            return

        hma = float(hma_value)
        sma_value = process_float(self._sma, Decimal(hma), candle.OpenTime, True)

        prev_hma = self._prev_hma
        self._prev_hma = hma

        if not self._sma.IsFormed or prev_hma is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sma = float(sma_value)
        close = float(candle.ClosePrice)

        if hma > prev_hma and close > sma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif hma < prev_hma and close < sma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hull_candles_strategy()
