import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange

class huge_income_strategy(Strategy):
    def __init__(self):
        super(huge_income_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")
        self._prev_close = 0.0
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

    def OnStarted2(self, time):
        super(huge_income_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
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
        close = float(candle.ClosePrice)
        if av <= 0 or self._prev_close == 0:
            self._prev_close = close
            return
        if self.Position > 0:
            if close < ev or close <= self._entry_price - av * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close > ev or close >= self._entry_price + av * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            return
        if self.Position == 0:
            if close > ev and self._prev_close <= ev:
                self._entry_price = close
                self.BuyMarket()
            elif close < ev and self._prev_close >= ev:
                self._entry_price = close
                self.SellMarket()
        self._prev_close = close

    def OnReseted(self):
        super(huge_income_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return huge_income_strategy()
