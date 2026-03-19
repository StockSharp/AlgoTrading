import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class macd_sample_1010_strategy(Strategy):
    """
    SMA band mean reversion with ATR-based bands and stops.
    """

    def __init__(self):
        super(macd_sample_1010_strategy, self).__init__()
        self._sma_length = self.Param("SmaLength", 20).SetDisplay("SMA Length", "Band center period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._band_mult = self.Param("BandMult", 2.0).SetDisplay("Band Multiplier", "ATR multiplier for bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_sample_1010_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(macd_sample_1010_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        sma = float(sma_val)
        atr = float(atr_val)
        if atr <= 0:
            return
        close = float(candle.ClosePrice)
        bm = float(self._band_mult.Value)
        upper = sma + atr * bm
        lower = sma - atr * bm
        if self.Position > 0:
            if close >= sma or close <= self._entry_price - atr * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= sma or close >= self._entry_price + atr * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0
        if self.Position == 0:
            if close < lower:
                self._entry_price = close
                self.BuyMarket()
            elif close > upper:
                self._entry_price = close
                self.SellMarket()

    def CreateClone(self):
        return macd_sample_1010_strategy()
