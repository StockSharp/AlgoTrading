import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange

class speed_ma_strategy(Strategy):
    def __init__(self):
        super(speed_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 13) \
            .SetDisplay("EMA Length", "Moving average period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
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

    def OnStarted(self, time):
        super(speed_ma_strategy, self).OnStarted(time)

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
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

        if self._prev_ema == 0 or av <= 0:
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ev
            return

        if self._prev_prev_ema == 0:
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ev
            return

        close = float(candle.ClosePrice)
        slope = ev - self._prev_ema
        prev_slope = self._prev_ema - self._prev_prev_ema

        if self.Position > 0:
            if close >= self._entry_price + av * 2.5 or close <= self._entry_price - av * 1.5 or slope < 0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - av * 2.5 or close >= self._entry_price + av * 1.5 or slope > 0:
                self.BuyMarket()
                self._entry_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ev
            return

        if self.Position == 0:
            if slope > 0 and prev_slope <= 0:
                self._entry_price = close
                self.BuyMarket()
            elif slope < 0 and prev_slope >= 0:
                self._entry_price = close
                self.SellMarket()

        self._prev_prev_ema = self._prev_ema
        self._prev_ema = ev

    def OnReseted(self):
        super(speed_ma_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return speed_ma_strategy()
