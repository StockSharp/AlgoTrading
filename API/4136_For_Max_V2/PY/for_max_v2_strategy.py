import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange

class for_max_v2_strategy(Strategy):
    def __init__(self):
        super(for_max_v2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 30) \
            .SetDisplay("EMA Length", "Trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")
        self._lookback = self.Param("Lookback", 10) \
            .SetDisplay("Lookback", "N-bar channel lookback.", "Indicators")

        self._entry_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bar_count = 0
        self._highs = []
        self._lows = []

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
    def Lookback(self):
        return self._lookback.Value

    def OnStarted(self, time):
        super(for_max_v2_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._bar_count = 0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._highs = [0.0] * 10
        self._lows = [0.0] * 10

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

        length = min(self.Lookback, 10)
        idx = self._bar_count % length
        self._highs[idx] = float(candle.HighPrice)
        self._lows[idx] = float(candle.LowPrice)
        self._bar_count += 1

        if self._bar_count < length or av <= 0:
            return

        high = max(self._highs[i] for i in range(length))
        low = min(self._lows[i] for i in range(length))

        close = float(candle.ClosePrice)

        if self._prev_high == 0 or self._prev_low == 0:
            self._prev_high = high
            self._prev_low = low
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
            if close > self._prev_high and close > ev:
                self._entry_price = close
                self.BuyMarket()
            elif close < self._prev_low and close < ev:
                self._entry_price = close
                self.SellMarket()

        self._prev_high = high
        self._prev_low = low

    def OnReseted(self):
        super(for_max_v2_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._bar_count = 0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._highs = []
        self._lows = []

    def CreateClone(self):
        return for_max_v2_strategy()
