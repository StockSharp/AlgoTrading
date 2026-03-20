import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange

class moving_average_with_frames_strategy(Strategy):
    def __init__(self):
        super(moving_average_with_frames_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._sma_length = self.Param("SmaLength", 12) \
            .SetDisplay("SMA Length", "SMA period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._entry_price = 0.0
        self._prev_close = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def SmaLength(self):
        return self._sma_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(moving_average_with_frames_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._prev_close = 0.0

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.SmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        sv = float(sma_val)
        av = float(atr_val)

        if av <= 0:
            return

        close = float(candle.ClosePrice)

        if self._prev_close == 0:
            self._prev_close = close
            return

        if self.Position > 0:
            if close < sv and self._prev_close >= sv:
                self.SellMarket()
                self._entry_price = 0.0
            elif close <= self._entry_price - av * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close > sv and self._prev_close <= sv:
                self.BuyMarket()
                self._entry_price = 0.0
            elif close >= self._entry_price + av * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            return

        if self.Position == 0:
            if close > sv and self._prev_close <= sv:
                self._entry_price = close
                self.BuyMarket()
            elif close < sv and self._prev_close >= sv:
                self._entry_price = close
                self.SellMarket()

        self._prev_close = close

    def OnReseted(self):
        super(moving_average_with_frames_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_close = 0.0

    def CreateClone(self):
        return moving_average_with_frames_strategy()
