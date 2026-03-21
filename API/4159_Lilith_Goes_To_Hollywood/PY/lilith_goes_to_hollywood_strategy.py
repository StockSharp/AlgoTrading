import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange

class lilith_goes_to_hollywood_strategy(Strategy):
    def __init__(self):
        super(lilith_goes_to_hollywood_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._fast_sma_length = self.Param("FastSmaLength", 10) \
            .SetDisplay("Fast SMA", "Fast SMA period.", "Indicators")
        self._slow_sma_length = self.Param("SlowSmaLength", 25) \
            .SetDisplay("Slow SMA", "Slow SMA period.", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastSmaLength(self):
        return self._fast_sma_length.Value

    @property
    def SlowSmaLength(self):
        return self._slow_sma_length.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(lilith_goes_to_hollywood_strategy, self).OnStarted(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastSmaLength
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowSmaLength
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self._rsi, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        rv = float(rsi_val)
        av = float(atr_val)

        if self._prev_fast == 0 or self._prev_slow == 0 or av <= 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if fv < sv and self._prev_fast >= self._prev_slow:
                self.SellMarket()
                self._entry_price = 0.0
            elif close <= self._entry_price - av * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if fv > sv and self._prev_fast <= self._prev_slow:
                self.BuyMarket()
                self._entry_price = 0.0
            elif close >= self._entry_price + av * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if fv > sv and self._prev_fast <= self._prev_slow and rv > 45:
                self._entry_price = close
                self.BuyMarket()
            elif fv < sv and self._prev_fast >= self._prev_slow and rv < 55:
                self._entry_price = close
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def OnReseted(self):
        super(lilith_goes_to_hollywood_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return lilith_goes_to_hollywood_strategy()
