import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange

class com_fracti_strategy(Strategy):
    def __init__(self):
        super(com_fracti_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._entry_price = 0.0
        self._h1 = 0.0
        self._h2 = 0.0
        self._h3 = 0.0
        self._h4 = 0.0
        self._h5 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._l4 = 0.0
        self._l5 = 0.0
        self._bar_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(com_fracti_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._bar_count = 0
        self._h1 = self._h2 = self._h3 = self._h4 = self._h5 = 0.0
        self._l1 = self._l2 = self._l3 = self._l4 = self._l5 = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._ema, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)
        ev = float(ema_val)
        av = float(atr_val)

        self._h5 = self._h4
        self._h4 = self._h3
        self._h3 = self._h2
        self._h2 = self._h1
        self._h1 = float(candle.HighPrice)
        self._l5 = self._l4
        self._l4 = self._l3
        self._l3 = self._l2
        self._l2 = self._l1
        self._l1 = float(candle.LowPrice)
        self._bar_count += 1

        if self._bar_count < 5 or av <= 0:
            return

        close = float(candle.ClosePrice)

        fractal_up = (self._h3 > self._h1 and self._h3 > self._h2 and
                      self._h3 > self._h4 and self._h3 > self._h5)
        fractal_down = (self._l3 < self._l1 and self._l3 < self._l2 and
                        self._l3 < self._l4 and self._l3 < self._l5)

        if self.Position > 0:
            if close >= self._entry_price + av * 3.0 or close <= self._entry_price - av * 2.0 or (fractal_up and rv > 65):
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - av * 3.0 or close >= self._entry_price + av * 2.0 or (fractal_down and rv < 35):
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if fractal_down and rv < 55:
                self._entry_price = close
                self.BuyMarket()
            elif fractal_up and rv > 45:
                self._entry_price = close
                self.SellMarket()

    def OnReseted(self):
        super(com_fracti_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._bar_count = 0
        self._h1 = self._h2 = self._h3 = self._h4 = self._h5 = 0.0
        self._l1 = self._l2 = self._l3 = self._l4 = self._l5 = 0.0

    def CreateClone(self):
        return com_fracti_strategy()
