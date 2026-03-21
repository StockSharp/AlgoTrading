import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange

class trailing_stop_fr_cn_sar_strategy(Strategy):
    def __init__(self):
        super(trailing_stop_fr_cn_sar_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 10) \
            .SetDisplay("Fast EMA Length", "Fast EMA period.", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 30) \
            .SetDisplay("Slow EMA Length", "Slow EMA period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._best_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastEmaLength(self):
        return self._fast_ema_length.Value

    @property
    def SlowEmaLength(self):
        return self._slow_ema_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(trailing_stop_fr_cn_sar_strategy, self).OnStarted(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._best_price = 0.0

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastEmaLength
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowEmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        av = float(atr_val)

        if self._prev_fast == 0 or self._prev_slow == 0 or av <= 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close > self._best_price:
                self._best_price = close
            if close <= self._best_price - av * 2.0 or (fv < sv and self._prev_fast >= self._prev_slow):
                self.SellMarket()
                self._entry_price = 0.0
                self._best_price = 0.0
        elif self.Position < 0:
            if close < self._best_price:
                self._best_price = close
            if close >= self._best_price + av * 2.0 or (fv > sv and self._prev_fast <= self._prev_slow):
                self.BuyMarket()
                self._entry_price = 0.0
                self._best_price = 0.0

        if self.Position == 0:
            if fv > sv and self._prev_fast <= self._prev_slow:
                self._entry_price = close
                self._best_price = close
                self.BuyMarket()
            elif fv < sv and self._prev_fast >= self._prev_slow:
                self._entry_price = close
                self._best_price = close
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def OnReseted(self):
        super(trailing_stop_fr_cn_sar_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._best_price = 0.0

    def CreateClone(self):
        return trailing_stop_fr_cn_sar_strategy()
