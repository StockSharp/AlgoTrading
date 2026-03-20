import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange

class com_fracti_fractal_rsi_strategy(Strategy):
    def __init__(self):
        super(com_fracti_fractal_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._entry_price = 0.0
        self._prev_high5 = 0.0
        self._prev_low5 = 0.0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._high4 = 0.0
        self._high5 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._low4 = 0.0
        self._low5 = 0.0
        self._bar_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(com_fracti_fractal_rsi_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._bar_count = 0
        self._prev_high5 = 0.0
        self._prev_low5 = 0.0
        self._high1 = self._high2 = self._high3 = self._high4 = self._high5 = 0.0
        self._low1 = self._low2 = self._low3 = self._low4 = self._low5 = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)
        av = float(atr_val)

        # Shift fractal window
        self._high5 = self._high4
        self._high4 = self._high3
        self._high3 = self._high2
        self._high2 = self._high1
        self._high1 = float(candle.HighPrice)
        self._low5 = self._low4
        self._low4 = self._low3
        self._low3 = self._low2
        self._low2 = self._low1
        self._low1 = float(candle.LowPrice)
        self._bar_count += 1

        if self._bar_count < 5 or av <= 0:
            return

        close = float(candle.ClosePrice)

        # Detect fractal high (center bar _high3 is highest)
        fractal_up = (self._high3 > self._high1 and self._high3 > self._high2 and
                      self._high3 > self._high4 and self._high3 > self._high5)
        # Detect fractal low (center bar _low3 is lowest)
        fractal_down = (self._low3 < self._low1 and self._low3 < self._low2 and
                        self._low3 < self._low4 and self._low3 < self._low5)

        if self.Position > 0:
            if close >= self._entry_price + av * 3.0 or close <= self._entry_price - av * 2.0 or fractal_down:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - av * 3.0 or close >= self._entry_price + av * 2.0 or fractal_up:
                self.BuyMarket()
                self._entry_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high5 = self._high5
            self._prev_low5 = self._low5
            return

        if self.Position == 0:
            if fractal_down and rv < 45:
                self._entry_price = close
                self.BuyMarket()
            elif fractal_up and rv > 55:
                self._entry_price = close
                self.SellMarket()

        self._prev_high5 = self._high5
        self._prev_low5 = self._low5

    def OnReseted(self):
        super(com_fracti_fractal_rsi_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._bar_count = 0
        self._prev_high5 = 0.0
        self._prev_low5 = 0.0
        self._high1 = self._high2 = self._high3 = self._high4 = self._high5 = 0.0
        self._low1 = self._low2 = self._low3 = self._low4 = self._low5 = 0.0

    def CreateClone(self):
        return com_fracti_fractal_rsi_strategy()
