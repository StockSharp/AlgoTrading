import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange

class flat001a_strategy(Strategy):
    def __init__(self):
        super(flat001a_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")
        self._prev_rsi = 0.0
        self._entry_price = 0.0

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
        super(flat001a_strategy, self).OnStarted(time)
        self._prev_rsi = 0.0
        self._entry_price = 0.0
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
        if self._prev_rsi == 0 or av <= 0:
            self._prev_rsi = rv
            return
        close = float(candle.ClosePrice)
        if self.Position > 0:
            if close >= self._entry_price + av * 2.5 or close <= self._entry_price - av * 1.5 or rv > 70:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - av * 2.5 or close >= self._entry_price + av * 1.5 or rv < 30:
                self.BuyMarket()
                self._entry_price = 0.0
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return
        if self.Position == 0:
            if rv > 55 and self._prev_rsi <= 55 and close > ev:
                self._entry_price = close
                self.BuyMarket()
            elif rv < 45 and self._prev_rsi >= 45 and close < ev:
                self._entry_price = close
                self.SellMarket()
        self._prev_rsi = rv

    def OnReseted(self):
        super(flat001a_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return flat001a_strategy()
