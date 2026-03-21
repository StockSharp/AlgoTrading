import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange

class send_close_order_strategy(Strategy):
    def __init__(self):
        super(send_close_order_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._entry_price = 0.0
        self._fractal_high = 0.0
        self._fractal_low = 0.0
        self._prev2_high = 0.0
        self._prev1_high = 0.0
        self._prev2_low = 0.0
        self._prev1_low = 0.0
        self._bar_count = 0

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
        super(send_close_order_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._fractal_high = 0.0
        self._fractal_low = 0.0
        self._prev2_high = 0.0
        self._prev1_high = 0.0
        self._prev2_low = 0.0
        self._prev1_low = 0.0
        self._bar_count = 0

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

        self._bar_count += 1

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        # Detect fractal high: prev1High > prev2High and prev1High > current high
        if self._bar_count > 3 and self._prev1_high > self._prev2_high and self._prev1_high > high:
            self._fractal_high = self._prev1_high

        # Detect fractal low: prev1Low < prev2Low and prev1Low < current low
        if self._bar_count > 3 and self._prev1_low < self._prev2_low and self._prev1_low < low:
            self._fractal_low = self._prev1_low

        self._prev2_high = self._prev1_high
        self._prev1_high = high
        self._prev2_low = self._prev1_low
        self._prev1_low = low

        if self._fractal_high == 0 or self._fractal_low == 0 or av <= 0:
            return

        if self.Position > 0:
            if close >= self._entry_price + av * 3.0 or close <= self._entry_price - av * 1.5:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - av * 3.0 or close >= self._entry_price + av * 1.5:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if close > self._fractal_high and close > ev:
                self._entry_price = close
                self.BuyMarket()
            elif close < self._fractal_low and close < ev:
                self._entry_price = close
                self.SellMarket()

    def OnReseted(self):
        super(send_close_order_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._fractal_high = 0.0
        self._fractal_low = 0.0
        self._prev2_high = 0.0
        self._prev1_high = 0.0
        self._prev2_low = 0.0
        self._prev1_low = 0.0
        self._bar_count = 0

    def CreateClone(self):
        return send_close_order_strategy()
