import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange

class envelope_ma_short_strategy(Strategy):
    def __init__(self):
        super(envelope_ma_short_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")
        self._band_percent = self.Param("BandPercent", 1.0) \
            .SetDisplay("Band %", "Band width percent.", "Indicators")

        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def BandPercent(self):
        return self._band_percent.Value

    def OnStarted2(self, time):
        super(envelope_ma_short_strategy, self).OnStarted2(time)

        self._entry_price = 0.0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        ev = float(ema_val)
        av = float(atr_val)

        if av <= 0 or ev <= 0:
            return

        close = float(candle.ClosePrice)
        factor = float(self.BandPercent) / 100.0
        upper = ev * (1.0 + factor)
        lower = ev * (1.0 - factor)

        if self.Position > 0:
            if close >= ev or close <= self._entry_price - av * 1.5:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= ev or close >= self._entry_price + av * 1.5:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if close < lower:
                self._entry_price = close
                self.BuyMarket()
            elif close > upper:
                self._entry_price = close
                self.SellMarket()

    def OnReseted(self):
        super(envelope_ma_short_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return envelope_ma_short_strategy()
